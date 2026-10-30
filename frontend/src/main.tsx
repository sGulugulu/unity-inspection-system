import { StrictMode, useEffect, useState, type FormEvent } from 'react';
import { createRoot } from 'react-dom/client';
import { InspectionTask, createTask, listTasks, updateStatus } from './api';
import { filterTasks, type TaskFilters } from './taskFilters';
import './style.css';

const priorityLabels = { low: '低', medium: '中', high: '高' };
const statusLabels = { open: '待处理', in_progress: '处理中', resolved: '已解决' };
const emptyFilters: TaskFilters = { keyword: '', priority: '', status: '' };

function App() {
  const [tasks, setTasks] = useState<InspectionTask[]>([]);
  const [filters, setFilters] = useState<TaskFilters>(emptyFilters);
  const [error, setError] = useState('');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState<InspectionTask['priority']>('medium');
  useEffect(() => { listTasks().then(setTasks).catch((e: Error) => setError(e.message)); }, []);
  async function changeStatus(id: string, status: InspectionTask['status']) { try { await updateStatus(id, status); setTasks(await listTasks()); } catch (e) { setError((e as Error).message); } }
  async function submit(event: FormEvent) { event.preventDefault(); if (!title.trim()) { setError('标题不能为空'); return; } try { await createTask({title, description, priority, position: {x: 0, y: 0, z: 0}}); setTitle(''); setDescription(''); setFilters(emptyFilters); setTasks(await listTasks()); setError(''); } catch (e) { setError((e as Error).message); } }
  const visibleTasks = filterTasks(tasks, filters);
  return <main>
    <h1>巡检任务管理</h1><p className="hint">服务地址：127.0.0.1:18080</p>
    {error && <p className="error" role="alert">{error}</p>}
    <form onSubmit={submit}>
      <input aria-label="问题标题" value={title} onChange={e => setTitle(e.target.value)} placeholder="问题标题（必填）" maxLength={120} />
      <input aria-label="问题描述" value={description} onChange={e => setDescription(e.target.value)} placeholder="问题描述（可选）" maxLength={2000} />
      <select aria-label="新问题优先级" value={priority} onChange={e => setPriority(e.target.value as InspectionTask['priority'])}>
        {Object.entries(priorityLabels).map(([value, label]) => <option key={value} value={value}>{label}优先级</option>)}
      </select><button type="submit">创建问题</button>
    </form>
    <div className="filters" aria-label="问题筛选">
      <input aria-label="搜索问题" placeholder="搜索标题、描述或编号" maxLength={2000} value={filters.keyword} onChange={e => setFilters({ ...filters, keyword: e.target.value })} />
      <select aria-label="按优先级筛选" value={filters.priority} onChange={e => setFilters({ ...filters, priority: e.target.value })}>
        <option value="">全部优先级</option>
        {Object.entries(priorityLabels).map(([value, label]) => <option key={value} value={value}>{label}优先级</option>)}
      </select>
      <select aria-label="按状态筛选" value={filters.status} onChange={e => setFilters({ ...filters, status: e.target.value })}>
        <option value="">全部状态</option>
        {Object.entries(statusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
      </select>
      <button type="button" onClick={() => setFilters(emptyFilters)}>清空筛选</button>
    </div>
    <p className="hint" aria-live="polite">共 {tasks.length} 条，当前显示 {visibleTasks.length} 条</p>
    <section aria-label="问题列表">
      {visibleTasks.length === 0 && <p>{tasks.length === 0 ? '暂无问题' : '没有符合条件的问题，请调整或清空筛选。'}</p>}
      {visibleTasks.map(task => <article key={task.id} className={`priority-${task.priority} ${task.status === 'resolved' ? 'task-resolved' : ''}`}>
        <h2>{task.title}</h2><p>{task.description || '无描述'}</p>
        <span className="status">{priorityLabels[task.priority]}优先级 · {statusLabels[task.status]}</span>
        <select aria-label={`修改“${task.title}”的状态`} value={task.status} onChange={e => changeStatus(task.id, e.target.value as InspectionTask['status'])}>
          {Object.entries(statusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select>
        <details><summary>查看详情</summary><dl>
          <dt>问题编号</dt><dd>{task.id}</dd>
          <dt>世界坐标</dt><dd>X：{task.position.x}，Y：{task.position.y}，Z：{task.position.z}</dd>
          <dt>创建时间</dt><dd>{new Date(task.createdAt).toLocaleString('zh-CN')}</dd>
          <dt>更新时间</dt><dd>{new Date(task.updatedAt).toLocaleString('zh-CN')}</dd>
        </dl></details>
      </article>)}
    </section>
  </main>;
}

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>);
