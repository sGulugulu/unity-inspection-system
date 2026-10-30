# Unity-3D 巡检系统开发任务

## 1. 项目目标

交付一个不依赖 AR 设备的 Unity 巡检客户端，以及配套的 Go 服务和 React/TypeScript 管理页。客户端负责展示巡检点、填写结果并提交；服务负责保存任务和巡检记录；管理页负责创建、查看和更新任务。

## 2. 范围与约束

- Unity 使用已安装的 2022.3.62f3c1 LTS，首个版本以 Windows Standalone 为运行目标。
- 不引入 AR Foundation、ARCore、ARKit 或其他 AR 设备依赖。
- 后端使用 Go 标准库和 SQLite 文件，接口默认监听 `127.0.0.1:18080`。
- 前端使用 React + TypeScript，通过 HTTP 调用后端接口。
- 所有外部输入必须做 JSON 解码、字段校验和长度边界检查。
- 密钥、真实设备信息和 AI 访问令牌不得写入仓库。

## 3. 必做功能与最小可验收版本

1. `GET /api/health` 返回服务状态。
2. `GET /api/tasks` 返回任务列表。
3. Unity 运行时生成地面、墙面和至少两个可观察或可点击对象，支持移动视角和鼠标射线点击。
4. Unity 点击场景位置创建唯一的未保存问题标记，重新点击会移除旧的未保存标记。
5. Unity 问题表单包含必填标题、可选描述、`low`/`medium`/`high` 优先级和标记的 x/y/z 坐标，并在成功或失败时提示。
6. `POST /api/tasks` 创建问题，要求标题、描述、优先级和位置有效；服务生成唯一 ID，默认状态为 `open`。
7. `PATCH /api/tasks/{id}` 仅允许 `open`、`in_progress`、`resolved`。
8. `GET /api/tasks` 返回持久化问题列表，服务重启后数据仍存在。
9. React 管理页从 Go 后端读取问题，展示标题、优先级、状态并能修改状态；后端不可用时显示错误而不是白屏。
10. 后端单元测试覆盖校验、创建、状态更新和持久化读取核心逻辑。

Unity 场景启动时将 `InspectionSceneController` 和 `SimpleCameraController` 挂到相机或空物体上；右键拖动旋转视角，WASD 移动，左键射线点击场景并打开问题表单。

## 4. 数据模型

任务字段：`id`、`title`、`description`、`priority`、`status`、`position`、`createdAt`、`updatedAt`。

状态仅允许：`open`、`in_progress`、`resolved`；优先级仅允许：`low`、`medium`、`high`。

## 5. 后续迭代

- Unity 巡检点交互、结果和照片记录。
- Go 的 SQLite 持久化、分页和鉴权。
- React 的任务详情、筛选和错误提示。
- AI 使用记录（模型、用途、输入摘要、输出摘要、时间和操作者），严禁保存令牌。
- Windows 构建和端到端联调。

## 6. 验收命令

```text
go test ./...
go run ./cmd/server
npm install && npm run build
```

验收时记录运行环境、测试结果和未完成项。
