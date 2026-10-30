using System;
using Inspection.Client;
using UnityEngine;

public static class InspectionRegressionChecks
{
    public static void Run()
    {
        Require(InspectionCoordinates.TryParse("1.5", "-2", "0.125", out var point) && point == new Vector3(1.5f, -2, 0.125f), "坐标解析保持精确位置");
        foreach (var invalid in new[] { "NaN", "Infinity", "100001", "", "abc", "1,5" })
            Require(!InspectionCoordinates.TryParse(invalid, "0", "0", out _), "拒绝无效坐标：" + invalid);
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.transform.position = new Vector3(0, 0, 5);
        Physics.SyncTransforms();
        Require(Physics.Raycast(new Ray(Vector3.zero, Vector3.forward), out var hit, 10), "射线命中模型");
        Require(Vector3.Distance(hit.point, new Vector3(0, 0, 4.5f)) < 0.0001f, "标记锚点使用真实表面位置");
        var cameraObject = new GameObject("测试相机");
        var camera = cameraObject.AddComponent<Camera>();
        camera.pixelRect = new Rect(0, 0, 1280, 720);
        var screen = camera.WorldToScreenPoint(hit.point);
        Require(Physics.Raycast(camera.ScreenPointToRay(screen), out var repeated, 10) && Vector3.Distance(repeated.point, hit.point) < 0.0001f, "屏幕投影与射线命中保持一致");
        UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject);
        Debug.Log("巡检回归检查通过");
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) throw new Exception("回归检查失败：" + description);
    }
}
