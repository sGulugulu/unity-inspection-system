package tasks

import "testing"

func TestValidateInput(t *testing.T) {
	if err := ValidateInput("巡检点", "设备检查", "medium", "open", Position{}); err != nil {
		t.Fatal(err)
	}
	if ValidateInput("", "", "medium", "open", Position{}) == nil {
		t.Fatal("empty title should fail")
	}
	if ValidateInput("点位", "", "medium", "unknown", Position{}) == nil {
		t.Fatal("invalid status should fail")
	}
	if ValidateInput("点位", "", "medium", "open", Position{X: 100001}) == nil {
		t.Fatal("out of range position should fail")
	}
}
