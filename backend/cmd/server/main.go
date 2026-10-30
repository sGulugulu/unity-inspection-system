package main

import (
	"database/sql"
	"encoding/json"
	"errors"
	"io"
	"log"
	"net/http"
	"os"
	"strings"
	"time"

	"github.com/google/uuid"
	"inspection/backend/internal/tasks"
	_ "modernc.org/sqlite"
)

type store struct{ db *sql.DB }

func newStore(path string) (*store, error) {
	db, err := sql.Open("sqlite", path)
	if err != nil {
		return nil, err
	}
	db.SetMaxOpenConns(1)
	_, err = db.Exec(`CREATE TABLE IF NOT EXISTS tasks (id TEXT PRIMARY KEY, title TEXT NOT NULL, description TEXT NOT NULL, priority TEXT NOT NULL, status TEXT NOT NULL, x REAL NOT NULL, y REAL NOT NULL, z REAL NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL)`)
	if err != nil {
		_ = db.Close()
		return nil, err
	}
	return &store{db: db}, nil
}

func main() {
	path := os.Getenv("INSPECTION_DB")
	if path == "" {
		path = "inspection.db"
	}
	address := os.Getenv("INSPECTION_ADDR")
	if address == "" {
		address = "127.0.0.1:18080"
	}
	s, err := newStore(path)
	if err != nil {
		log.Fatal(err)
	}
	defer s.db.Close()
	mux := http.NewServeMux()
	mux.HandleFunc("/api/health", func(w http.ResponseWriter, _ *http.Request) {
		writeJSON(w, http.StatusOK, map[string]string{"status": "ok"})
	})
	mux.HandleFunc("/api/tasks", s.handleTasks)
	mux.HandleFunc("/api/tasks/", s.handleTaskStatus)
	log.Println("inspection server listening on", address)
	log.Fatal(http.ListenAndServe(address, mux))
}

func (s *store) handleTasks(w http.ResponseWriter, r *http.Request) {
	if r.Method == http.MethodGet {
		rows, err := s.db.Query(`SELECT id,title,description,priority,status,x,y,z,created_at,updated_at FROM tasks ORDER BY created_at DESC`)
		if err != nil {
			http.Error(w, "query failed", http.StatusInternalServerError)
			return
		}
		defer rows.Close()
		items := make([]tasks.Task, 0)
		for rows.Next() {
			var item tasks.Task
			var created, updated string
			if err := rows.Scan(&item.ID, &item.Title, &item.Description, &item.Priority, &item.Status, &item.Position.X, &item.Position.Y, &item.Position.Z, &created, &updated); err != nil {
				http.Error(w, "query failed", http.StatusInternalServerError)
				return
			}
			item.CreatedAt, _ = time.Parse(time.RFC3339Nano, created)
			item.UpdatedAt, _ = time.Parse(time.RFC3339Nano, updated)
			items = append(items, item)
		}
		writeJSON(w, http.StatusOK, items)
		return
	}
	if r.Method != http.MethodPost {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}
	var in struct {
		Title, Description, Priority string
		Position                     tasks.Position `json:"position"`
	}
	if err := decodeJSON(r, &in); err != nil {
		http.Error(w, "invalid JSON", http.StatusBadRequest)
		return
	}
	if err := tasks.ValidateInput(in.Title, in.Description, in.Priority, "open", in.Position); err != nil {
		http.Error(w, err.Error(), http.StatusBadRequest)
		return
	}
	now := time.Now().UTC()
	id := "task-" + uuid.NewString()
	title := strings.TrimSpace(in.Title)
	_, err := s.db.Exec(`INSERT INTO tasks(id,title,description,priority,status,x,y,z,created_at,updated_at) VALUES(?,?,?,?,?,?,?,?,?,?)`, id, title, in.Description, in.Priority, "open", in.Position.X, in.Position.Y, in.Position.Z, now.Format(time.RFC3339Nano), now.Format(time.RFC3339Nano))
	if err != nil {
		http.Error(w, "create failed", http.StatusInternalServerError)
		return
	}
	writeJSON(w, http.StatusCreated, tasks.Task{ID: id, Title: title, Description: in.Description, Priority: in.Priority, Status: "open", Position: in.Position, CreatedAt: now, UpdatedAt: now})
}

func (s *store) handleTaskStatus(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPatch {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}
	id := strings.TrimPrefix(r.URL.Path, "/api/tasks/")
	if id == "" || strings.Contains(id, "/") {
		http.Error(w, "invalid id", http.StatusBadRequest)
		return
	}
	var in struct {
		Status string `json:"status"`
	}
	if err := decodeJSON(r, &in); err != nil {
		http.Error(w, "invalid JSON", http.StatusBadRequest)
		return
	}
	if err := tasks.ValidateInput("有效标题", "", "medium", in.Status, tasks.Position{}); err != nil {
		http.Error(w, err.Error(), http.StatusBadRequest)
		return
	}
	now := time.Now().UTC()
	result, err := s.db.Exec(`UPDATE tasks SET status=?,updated_at=? WHERE id=?`, in.Status, now.Format(time.RFC3339Nano), id)
	if err != nil {
		http.Error(w, "update failed", http.StatusInternalServerError)
		return
	}
	count, _ := result.RowsAffected()
	if count == 0 {
		http.NotFound(w, r)
		return
	}
	var item tasks.Task
	var created, updated string
	err = s.db.QueryRow(`SELECT id,title,description,priority,status,x,y,z,created_at,updated_at FROM tasks WHERE id=?`, id).Scan(&item.ID, &item.Title, &item.Description, &item.Priority, &item.Status, &item.Position.X, &item.Position.Y, &item.Position.Z, &created, &updated)
	if err != nil {
		http.Error(w, "query failed", http.StatusInternalServerError)
		return
	}
	item.CreatedAt, _ = time.Parse(time.RFC3339Nano, created)
	item.UpdatedAt, _ = time.Parse(time.RFC3339Nano, updated)
	writeJSON(w, http.StatusOK, item)
}

func decodeJSON(r *http.Request, dst any) error {
	r.Body = http.MaxBytesReader(discardResponse{}, r.Body, 64<<10)
	decoder := json.NewDecoder(r.Body)
	decoder.DisallowUnknownFields()
	if err := decoder.Decode(dst); err != nil {
		return err
	}
	// 拒绝尾随内容，防止客户端与服务端对同一个请求产生不同理解。
	if err := decoder.Decode(new(any)); err != io.EOF {
		return errors.New("unexpected trailing JSON")
	}
	return nil
}

type discardResponse struct{}

func (discardResponse) Header() http.Header       { return http.Header{} }
func (discardResponse) Write([]byte) (int, error) { return 0, nil }
func (discardResponse) WriteHeader(int)           {}
func writeJSON(w http.ResponseWriter, status int, value any) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(value)
}
