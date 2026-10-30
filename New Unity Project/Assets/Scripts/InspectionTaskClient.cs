using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Inspection.Client
{
    public sealed class InspectionTaskClient : MonoBehaviour
    {
        [SerializeField] private string apiBaseUrl = "http://127.0.0.1:18080";

        public void LoadTasks(Action<InspectionTask[]> onCompleted, Action<string> onFailed = null)
        {
            StartCoroutine(LoadTasksCoroutine(onCompleted, onFailed));
        }

        private IEnumerator LoadTasksCoroutine(Action<InspectionTask[]> onCompleted, Action<string> onFailed)
        {
            using (var request = UnityWebRequest.Get(apiBaseUrl.TrimEnd('/') + "/api/tasks"))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    onFailed?.Invoke(request.error);
                    yield break;
                }

                // JsonUtility 不支持直接反序列化根数组，因此使用轻量包装器适配服务返回值。
                var json = "{\"items\":" + request.downloadHandler.text + "}";
                var response = JsonUtility.FromJson<TaskListResponse>(json);
                onCompleted?.Invoke(response?.items ?? Array.Empty<InspectionTask>());
            }
        }

        [Serializable]
        private sealed class TaskListResponse
        {
            public InspectionTask[] items;
        }
    }
}
