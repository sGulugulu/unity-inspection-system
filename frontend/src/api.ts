export type InspectionTask = {
  id: string;
  title: string;
  description: string;
  priority: 'low' | 'medium' | 'high';
  status: 'open' | 'in_progress' | 'resolved';
  position: { x: number; y: number; z: number };
  createdAt: string;
  updatedAt: string;
};

export async function listTasks(baseUrl = ''): Promise<InspectionTask[]> {
  const response = await fetch(`${baseUrl}/api/tasks`);
  if (!response.ok) throw new Error(`任务列表请求失败：${response.status}`);
  return response.json() as Promise<InspectionTask[]>;
}

export async function createTask(input: Pick<InspectionTask, 'title' | 'description' | 'priority' | 'position'>): Promise<InspectionTask> {
  const response = await fetch('/api/tasks', {method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify(input)});
  if (!response.ok) throw new Error(`创建任务失败：${await response.text()}`);
  return response.json();
}

export async function updateStatus(id: string, status: InspectionTask['status']): Promise<void> {
  const response = await fetch(`/api/tasks/${encodeURIComponent(id)}`, {method: 'PATCH', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({status})});
  if (!response.ok) throw new Error(`更新状态失败：${await response.text()}`);
}
