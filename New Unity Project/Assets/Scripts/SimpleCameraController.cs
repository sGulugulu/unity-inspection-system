using UnityEngine;

namespace Inspection.Client
{
    public sealed class SimpleCameraController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float lookSpeed = 2f;
        private float pitch;

        private void Start()
        {
            // 从现有相机角度开始旋转，避免第一次右键拖动时视角突然跳变。
            pitch = Mathf.DeltaAngle(0, transform.localEulerAngles.x);
        }

        private void Update()
        {
            var inspection = GetComponent<InspectionSceneController>();
            // 填写标题和坐标时，WASD 不应同时驱动相机移动。
            if (inspection != null && inspection.IsEditing) return;
            var direction = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            transform.position += transform.TransformDirection(direction) * (moveSpeed * Time.deltaTime);
            if (!Input.GetMouseButton(1)) return;
            transform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * lookSpeed, Space.World);
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * lookSpeed, -80f, 80f);
            transform.localEulerAngles = new Vector3(pitch, transform.localEulerAngles.y, 0);
        }
    }
}
