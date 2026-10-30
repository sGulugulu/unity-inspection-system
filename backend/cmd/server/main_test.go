package main

import (
	"bytes"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"os"
	"testing"

	_ "modernc.org/sqlite"
)

func TestTaskAPI(t *testing.T) {
	dbPath := t.TempDir() + "\\tasks.db"
	s, err := newStore(dbPath)
	if err != nil {
		t.Fatal(err)
	}
	defer s.db.Close()
	mux := http.NewServeMux()
	mux.HandleFunc("/api/tasks", s.handleTasks)
	mux.HandleFunc("/api/tasks/", s.handleTaskStatus)
	body := bytes.NewBufferString(`{"title":"泵房巡检","description":"检查压力表","priority":"high","position":{"x":1,"y":2,"z":3}}`)
	req := httptest.NewRequest(http.MethodPost, "/api/tasks", body)
	rec := httptest.NewRecorder()
	mux.ServeHTTP(rec, req)
	if rec.Code != http.StatusCreated {
		t.Fatalf("create status=%d body=%s", rec.Code, rec.Body.String())
	}
	var created struct {
		ID string `json:"id"`
	}
	if err := json.Unmarshal(rec.Body.Bytes(), &created); err != nil || created.ID == "" {
		t.Fatal("created task id missing")
	}
	statusBody := bytes.NewBufferString(`{"status":"resolved"}`)
	req = httptest.NewRequest(http.MethodPatch, "/api/tasks/"+created.ID, statusBody)
	rec = httptest.NewRecorder()
	mux.ServeHTTP(rec, req)
	if rec.Code != http.StatusOK {
		t.Fatalf("update status=%d body=%s", rec.Code, rec.Body.String())
	}
	req = httptest.NewRequest(http.MethodGet, "/api/tasks", nil)
	rec = httptest.NewRecorder()
	mux.ServeHTTP(rec, req)
	if rec.Code != http.StatusOK || !bytes.Contains(rec.Body.Bytes(), []byte(`"resolved"`)) {
		t.Fatalf("list response=%s", rec.Body.String())
	}
	if err := s.db.Close(); err != nil {
		t.Fatal(err)
	}
	reopened, err := newStore(dbPath)
	if err != nil {
		t.Fatal(err)
	}
	defer reopened.db.Close()
	row := reopened.db.QueryRow("SELECT COUNT(*) FROM tasks")
	var count int
	if err := row.Scan(&count); err != nil {
		t.Fatal(err)
	}
	if count != 1 {
		t.Fatalf("persisted task count=%d", count)
	}
	_ = os.Remove(dbPath)
}
