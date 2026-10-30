import test from 'node:test';
import assert from 'node:assert/strict';
import { filterTasks } from '../src/taskFilters.ts';

const tasks = [
  { id: 'task-A1', title: '泵房温度异常', description: '检查 Valve', priority: 'high', status: 'open' },
  { id: 'task-B2', title: '管线漏水', description: '', priority: 'medium', status: 'in_progress' },
  { id: 'task-C3', title: '泵房照明', description: '已更换', priority: 'low', status: 'resolved' },
];
const empty = { keyword: '', priority: '', status: '' };
test('无条件保留全部记录及顺序，空列表正常返回', () => {
  assert.deepEqual(filterTasks(tasks, empty), tasks);
  assert.notEqual(filterTasks(tasks, empty), tasks);
  assert.deepEqual(filterTasks([], empty), []);
});
test('搜索支持中文标题、描述、编号、忽略大小写和首尾空格', () => {
  assert.deepEqual(filterTasks(tasks, { ...empty, keyword: '泵房' }).map(t => t.id), ['task-A1', 'task-C3']);
  for (const keyword of [' VALVE ', 'TASK-a1', '温度']) {
    assert.deepEqual(filterTasks(tasks, { ...empty, keyword }).map(t => t.id), ['task-A1']);
  }
  assert.equal(filterTasks(tasks, { ...empty, keyword: '   ' }).length, 3);
});
test('优先级与状态均可单独筛选', () => {
  for (const task of tasks) {
    assert.deepEqual(filterTasks(tasks, { ...empty, priority: task.priority }), [task]);
    assert.deepEqual(filterTasks(tasks, { ...empty, status: task.status }), [task]);
  }
});
test('组合条件取交集，无匹配时返回空列表', () => {
  assert.deepEqual(filterTasks(tasks, { keyword: '泵房', priority: 'high', status: 'open' }), [tasks[0]]);
  assert.deepEqual(filterTasks(tasks, { keyword: '泵房', priority: 'high', status: 'resolved' }), []);
  assert.deepEqual(filterTasks(tasks, { ...empty, keyword: '不存在' }), []);
});
