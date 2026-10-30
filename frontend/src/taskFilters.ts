import type { InspectionTask } from './api';

export type TaskFilters = { keyword: string; priority: string; status: string };

export function filterTasks(tasks: InspectionTask[], filters: TaskFilters): InspectionTask[] {
  // 多个条件取交集，便于快速找出某类待处理问题。
  const keyword = filters.keyword.trim().toLocaleLowerCase();
  return tasks.filter(task =>
    (!filters.priority || task.priority === filters.priority) &&
    (!filters.status || task.status === filters.status) &&
    (!keyword || [task.title, task.description, task.id].some(value => value.toLocaleLowerCase().includes(keyword)))
  );
}
