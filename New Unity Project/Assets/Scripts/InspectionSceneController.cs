using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

namespace Inspection.Client
{
    public sealed class InspectionSceneController : MonoBehaviour
    {
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private string apiBaseUrl = "http://127.0.0.1:18080";
        private readonly List<IssueMarker> savedMarkers = new List<IssueMarker>();
        private IssueMarker draft;
        private string titleText = "", descriptionText = "", priority = "medium";
        private string xText = "", yText = "", zText = "", message = "";
        private bool submitting;
        private Rect FormRect => new Rect(16, 16, 340, Mathf.Min(490, Screen.height - 32));
        public bool IsEditing => draft != null;

        private sealed class IssueMarker
        {
            public Vector3 Position;
            public string Title, Description;
        }

        private void Start()
        {
            if (sceneCamera == null) sceneCamera = GetComponent<Camera>();
            if (sceneCamera == null) sceneCamera = Camera.main;
            CreatePrimitive(PrimitiveType.Plane, Vector3.zero, new Vector3(4, 1, 4), "地面");
            CreatePrimitive(PrimitiveType.Cube, new Vector3(-2, 0.75f, 2), new Vector3(1, 1.5f, 1), "设备A");
            CreatePrimitive(PrimitiveType.Cylinder, new Vector3(2, 0.75f, 2), new Vector3(1, 1.5f, 1), "管线B");
            // 墙体围合场地但保持顶部敞开，便于观察和点击内部设备。
            CreatePrimitive(PrimitiveType.Cube, new Vector3(0, 2, 20), new Vector3(40, 4, 0.3f), "北墙");
            CreatePrimitive(PrimitiveType.Cube, new Vector3(0, 2, -20), new Vector3(40, 4, 0.3f), "南墙");
            CreatePrimitive(PrimitiveType.Cube, new Vector3(-20, 2, 0), new Vector3(0.3f, 4, 40), "西墙");
            CreatePrimitive(PrimitiveType.Cube, new Vector3(20, 2, 0), new Vector3(0.3f, 4, 40), "东墙");
            StartCoroutine(LoadSavedMarkers());
        }

        private IEnumerator LoadSavedMarkers()
        {
            using (var request = UnityWebRequest.Get(apiBaseUrl.TrimEnd('/') + "/api/tasks"))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (string.IsNullOrEmpty(message)) message = "历史问题加载失败，请确认后端已启动；仍可选择位置填写新问题。";
                    yield break;
                }
                var response = JsonUtility.FromJson<ProblemList>("{\"items\":" + request.downloadHandler.text + "}");
                if (response == null || response.items == null) yield break;
                foreach (var item in response.items)
                    savedMarkers.Add(new IssueMarker { Position = new Vector3(item.position.x, item.position.y, item.position.z), Title = item.title, Description = item.description });
            }
        }

        private void LateUpdate()
        {
            if (!Input.GetMouseButtonDown(0) || sceneCamera == null || submitting) return;
            var mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            // 当前界面使用 IMGUI，面板必须拦截场景点击，避免把问题移动到面板背后。
            if (draft != null && FormRect.Contains(mouse)) return;
            foreach (var marker in savedMarkers)
                if (TryGetIconRect(marker.Position, out var icon) && icon.Contains(mouse)) return;
            if (draft != null && TryGetIconRect(draft.Position, out var draftIcon) && draftIcon.Contains(mouse)) return;
            var ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
            // 记录精确表面命中点；图标只是投影，不创建碰撞体，也不添加世界坐标偏移。
            draft = new IssueMarker { Position = hit.point };
            xText = hit.point.x.ToString("R", CultureInfo.InvariantCulture);
            yText = hit.point.y.ToString("R", CultureInfo.InvariantCulture);
            zText = hit.point.z.ToString("R", CultureInfo.InvariantCulture);
            message = "已选择表面位置，请填写问题。";
        }

        private bool TryGetIconRect(Vector3 position, out Rect rect)
        {
            rect = default;
            if (sceneCamera == null) return false;
            var screen = sceneCamera.WorldToScreenPoint(position);
            if (screen.z <= 0 || !sceneCamera.pixelRect.Contains(new Vector2(screen.x, screen.y))) return false;
            rect = new Rect(screen.x - 11, Screen.height - screen.y - 11, 22, 22);
            return true;
        }

        private void OnGUI()
        {
            foreach (var marker in savedMarkers) DrawMarker(marker, false);
            if (draft != null) DrawMarker(draft, true);
            if (draft != null)
            {
                GUILayout.BeginArea(FormRect, "新建巡检问题", GUI.skin.window);
                GUI.enabled = !submitting;
                GUILayout.Label("标题（必填）"); titleText = GUILayout.TextField(titleText, 120);
                GUILayout.Label("描述（可选）"); descriptionText = GUILayout.TextArea(descriptionText, 2000, GUILayout.Height(64));
                GUILayout.Label("优先级");
                var selected = GUILayout.SelectionGrid(priority == "low" ? 0 : priority == "high" ? 2 : 1, new[] { "low", "medium", "high" }, 3);
                priority = selected == 0 ? "low" : selected == 2 ? "high" : "medium";
                GUILayout.Label("标记坐标（世界坐标，可修改）");
                xText = CoordinateField("X", xText); yText = CoordinateField("Y", yText); zText = CoordinateField("Z", zText);
                if (InspectionCoordinates.TryParse(xText, yText, zText, out var position)) draft.Position = position;
                GUILayout.Label("修改有效坐标后，图标会同步移动。");
                if (GUILayout.Button(submitting ? "正在提交……" : "提交问题")) StartCoroutine(SubmitProblem());
                if (GUILayout.Button("取消填写")) { draft = null; titleText = ""; descriptionText = ""; message = "已取消，未保存标记已移除。"; }
                GUI.enabled = true;
                GUILayout.EndArea();
            }
            // 反馈独立于表单生命周期，提交成功关闭表单后仍然可见。
            if (!string.IsNullOrEmpty(message))
                GUI.Box(new Rect(16, Screen.height - 76, Mathf.Max(200, Screen.width - 32), 60), message);
        }

        private static string CoordinateField(string label, string value)
        {
            GUILayout.BeginHorizontal(); GUILayout.Label(label, GUILayout.Width(22));
            var result = GUILayout.TextField(value, 32); GUILayout.EndHorizontal(); return result;
        }

        private void DrawMarker(IssueMarker marker, bool pending)
        {
            if (!TryGetIconRect(marker.Position, out var rect)) return;
            var old = GUI.color; GUI.color = pending ? Color.yellow : Color.cyan;
            GUI.Box(rect, "!"); GUI.color = old;
            if (!rect.Contains(Event.current.mousePosition)) return;
            var title = pending ? (string.IsNullOrWhiteSpace(titleText) ? "未提交问题" : titleText) : marker.Title;
            var description = pending ? descriptionText : marker.Description;
            var width = Mathf.Min(300, Screen.width - 16);
            var left = Mathf.Clamp(rect.xMax + 8, 8, Mathf.Max(8, Screen.width - width - 8));
            var top = Mathf.Clamp(rect.y, 8, Mathf.Max(8, Screen.height - 150));
            GUI.Box(new Rect(left, top, width, 140), title + "\n" + (string.IsNullOrEmpty(description) ? "无描述" : description), new GUIStyle(GUI.skin.box) { wordWrap = true, alignment = TextAnchor.UpperLeft });
        }

        private IEnumerator SubmitProblem()
        {
            if (submitting || draft == null) yield break;
            if (string.IsNullOrWhiteSpace(titleText)) { message = "提交失败：标题不能为空。"; yield break; }
            if (!InspectionCoordinates.TryParse(xText, yText, zText, out var point)) { message = "提交失败：坐标必须为 -100000 到 100000 之间的有限数字。"; yield break; }
            var marker = draft;
            marker.Position = point;
            var submittedTitle = titleText.Trim(); var submittedDescription = descriptionText;
            var payload = JsonUtility.ToJson(new ProblemPayload { title = submittedTitle, description = submittedDescription, priority = priority, position = new Position { x = point.x, y = point.y, z = point.z } });
            submitting = true; message = "正在提交，请稍候……";
            using (var request = new UnityWebRequest(apiBaseUrl.TrimEnd('/') + "/api/tasks", "POST"))
            {
                request.timeout = 15;
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(payload));
                request.downloadHandler = new DownloadHandlerBuffer(); request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();
                submitting = false;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    marker.Title = submittedTitle; marker.Description = submittedDescription; savedMarkers.Add(marker);
                    draft = null; titleText = ""; descriptionText = "";
                    message = "提交成功：问题已保存。悬停在图标上可查看名称和描述。";
                }
                else message = "提交失败：" + request.error + "\n" + request.downloadHandler.text + "\n填写内容已保留，可重试。";
            }
        }

        private static void CreatePrimitive(PrimitiveType type, Vector3 position, Vector3 scale, string objectName)
        {
            var item = GameObject.CreatePrimitive(type); item.name = objectName;
            item.transform.SetPositionAndRotation(position, Quaternion.identity); item.transform.localScale = scale;
        }

        [Serializable] private sealed class ProblemPayload { public string title; public string description; public string priority; public Position position; }
        [Serializable] private sealed class ProblemList { public InspectionTask[] items; }
    }
}
