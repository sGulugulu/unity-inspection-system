package tasks

import (
	"errors"
	"math"
	"strings"
	"time"
)

var validStatuses = map[string]bool{
	"open": true, "in_progress": true, "resolved": true,
}
var validPriorities = map[string]bool{"low": true, "medium": true, "high": true}

type Position struct {
	X float64 `json:"x"`
	Y float64 `json:"y"`
	Z float64 `json:"z"`
}

type Task struct {
	ID          string    `json:"id"`
	Title       string    `json:"title"`
	Description string    `json:"description"`
	Priority    string    `json:"priority"`
	Status      string    `json:"status"`
	Position    Position  `json:"position"`
	CreatedAt   time.Time `json:"createdAt"`
	UpdatedAt   time.Time `json:"updatedAt"`
}

func ValidateInput(title, description, priority, status string, p Position) error {
	if n := len([]rune(strings.TrimSpace(title))); n < 1 || n > 120 {
		return errors.New("title length must be between 1 and 120")
	}
	if len([]rune(description)) > 2000 {
		return errors.New("description is too long")
	}
	if !validPriorities[priority] {
		return errors.New("invalid priority")
	}
	if !validStatuses[status] {
		return errors.New("invalid status")
	}
	if p.X < -100000 || p.X > 100000 || p.Y < -100000 || p.Y > 100000 || p.Z < -100000 || p.Z > 100000 {
		return errors.New("position is out of range")
	}
	for _, value := range []float64{p.X, p.Y, p.Z} {
		if math.IsNaN(value) || math.IsInf(value, 0) {
			return errors.New("position must be finite")
		}
	}
	return nil
}
