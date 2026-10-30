using UnityEngine;

namespace Inspection.Client
{
    public static class InspectionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartInspectionScene()
        {
            if (Object.FindObjectOfType<InspectionSceneController>() != null) return;
            var cameraObject = new GameObject("巡检相机");
            cameraObject.transform.position = new Vector3(0, 3, -7);
            cameraObject.transform.rotation = Quaternion.Euler(18, 0, 0);
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<SimpleCameraController>();
            var controller = cameraObject.AddComponent<InspectionSceneController>();
            // 控制器会优先使用 Camera.main；这里显式设置主相机标签以保证空场景也能运行。
            camera.tag = "MainCamera";
            var lightObject = new GameObject("巡检主光源");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            Object.DontDestroyOnLoad(cameraObject);
            Object.DontDestroyOnLoad(lightObject);
        }
    }
}
