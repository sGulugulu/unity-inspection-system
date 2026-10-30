# Unity 巡检系统

在固定虚拟场景中标注巡检问题，通过 Go 后端保存，并在 React 管理页查看和更新。

## 1. 使用版本

- Unity：2022.3.62f3c1。
- 后端：Go 1.27（`go.mod` 要求），SQLite 驱动 `modernc.org/sqlite` 1.61.0。
- 前端：React 19.3.1、TypeScript 5.9.3、Vite 7.3.7（以锁文件为准）。
- Node.js：建议 24 LTS；前端自动化测试需要 22.18+ 或 24+。以下命令在 Windows PowerShell 中执行。

## 2. 三个部分如何启动

先将仓库克隆或下载到本地。以下以源码位于 `D:\u3d_editor` 为例，下载到其他位置时请替换路径。

**后端**：打开一个终端执行：

```powershell
cd D:\u3d_editor\backend
go run ./cmd/server
```

**React 管理前端**：另开一个终端执行：

```powershell
cd D:\u3d_editor\frontend
npm ci
npm run dev
```

浏览器打开终端显示的地址，通常为 `http://127.0.0.1:5173`。前端通过 Vite 将 `/api` 请求代理到后端。

**Unity 客户端**：使用上述 Unity 版本打开 `D:\u3d_editor\New Unity Project`，打开空场景并点击播放。程序自动生成场景、相机和光源。右键拖动旋转视角，WASD 移动，左键点击表面填写问题；坐标可手动修改，提交成功后保存标记，悬停查看标题和描述。填写表单时暂停相机移动。

## 3. 已完成、未完成和已知问题

- **已完成**：虚拟场景点击标注、问题表单和坐标校验、提交结果提示；后端创建、查询、更新状态和 SQLite 持久化；Unity 启动时恢复历史标记；React 展示问题、修改状态、组合筛选、搜索、展开详情和按优先级／状态区分卡片颜色；后端及前端筛选自动化测试。
- **未完成**：删除问题、设备编号／区域字段、照片上传、真实 AR、XR Simulation 和 Docker；这些扩展本次未实现。
- **本次提交范围**：包含三端源码和说明文档，暂未包含 `AI_LOG.md` 和两段演示视频，完整交付前仍需补齐。
- **已知问题与限制**：Unity 历史标记仅在启动时加载，管理页改动后需重新进入播放模式才能同步；React 创建的问题坐标固定为 `(0, 0, 0)`，需要准确选点时请在 Unity 中创建；尚未进行真实 AR 真机测试，新增前端功能尚未完成浏览器端到端验收；系统暂无登录鉴权，当前用于本机演示。

验证命令：在 `backend` 目录执行 `go test ./...`；在 `frontend` 目录执行 `npm test` 和 `npm run build`。

## 4. 后端地址和接口设计

默认地址：`http://127.0.0.1:18080`。默认数据文件为后端启动目录下的 `inspection.db`，数据保存在 `tasks` 表中。可在启动前通过环境变量 `INSPECTION_ADDR` 和 `INSPECTION_DB` 修改监听地址和数据库路径；修改地址时需同步调整前端 `vite.config.ts` 和 Unity 的 `apiBaseUrl`。

| 方法 | 路径 | 作用 |
| --- | --- | --- |
| GET | `/api/health` | 检查服务是否正常 |
| GET | `/api/tasks` | 获取全部问题，按创建时间倒序返回 |
| POST | `/api/tasks` | 创建问题，提交 `title`、`description`、`priority`、`position` |
| PATCH | `/api/tasks/{id}` | 更新状态，请求体如 `{"status":"resolved"}` |

三端共用字段：`id`、`title`、`description`、`priority`、`status`、`position: {x,y,z}`、`createdAt`、`updatedAt`。编号和时间由后端生成，新问题状态为 `open`。优先级可选 `low`／`medium`／`high`，状态可选 `open`／`in_progress`／`resolved`。后端校验标题、描述长度、枚举和坐标范围；创建成功返回 201，查询或更新成功返回 200，无效请求返回 400，更新不存在的问题返回 404。

## 5. 三个 AR 基础问题

1. **真实 AR 如何检测平面并点击放置？** 用 `ARPlaneManager` 识别地面、桌面，用 `ARRaycastManager` 找到点击对应的位置，再放置标注；需要时用 `ARAnchorManager` 创建锚点，让标注保持稳定。
2. **为什么只保存 `x/y/z` 不够？** 每次启动 AR，坐标原点和方向可能变化，同一组坐标可能指向别处。要找回原来的真实位置，还需要平台支持的持久化锚点、环境重定位，或固定图像标记。
3. **初始化失败、不支持 AR 或跟踪异常怎么办？** 告诉用户原因和处理办法，例如“当前设备不支持 AR”“请开启相机权限”“请到明亮处缓慢移动手机”。定位不稳定时暂停放置，并提供重试或返回虚拟场景的入口。
