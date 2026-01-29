using UnityEngine;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Simple camera controller for pan and zoom
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Movement")]
        public float panSpeed = 20f;
        public float edgePanSpeed = 10f;
        public float edgePanBorder = 50f;

        [Header("Zoom")]
        public float zoomSpeed = 5f;
        public float minZoom = 5f;
        public float maxZoom = 50f;

        [Header("Drag")]
        public bool enableMiddleMouseDrag = true;

        private Camera cam;
        private Vector3 dragOrigin;
        private bool isDragging = false;

        private void Start()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = Camera.main;
            }

            // Position camera for top-down view
            if (cam != null)
            {
                transform.rotation = Quaternion.Euler(70f, 0f, 0f);
                transform.position = new Vector3(0f, 50f, -25f);
                cam.fieldOfView = 75f;
                cam.backgroundColor = Color.black; // Black background for contrast
                cam.clearFlags = CameraClearFlags.SolidColor; // Use solid color instead of skybox

                Debug.Log($"[CameraController] Camera positioned at {transform.position}, rotation {transform.rotation.eulerAngles}, FOV {cam.fieldOfView}");
            }
        }

        private void Update()
        {
            HandlePanning();
            HandleZoom();
        }

        private void HandlePanning()
        {
            Vector3 move = Vector3.zero;

            // WASD keys
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                move += Vector3.forward;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                move += Vector3.back;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                move += Vector3.left;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                move += Vector3.right;

            // Edge panning
            Vector3 mousePos = Input.mousePosition;
            if (mousePos.x < edgePanBorder)
                move += Vector3.left * edgePanSpeed * Time.deltaTime;
            if (mousePos.x > Screen.width - edgePanBorder)
                move += Vector3.right * edgePanSpeed * Time.deltaTime;
            if (mousePos.y < edgePanBorder)
                move += Vector3.back * edgePanSpeed * Time.deltaTime;
            if (mousePos.y > Screen.height - edgePanBorder)
                move += Vector3.forward * edgePanSpeed * Time.deltaTime;

            // Middle mouse drag
            if (enableMiddleMouseDrag)
            {
                if (Input.GetMouseButtonDown(2))
                {
                    dragOrigin = cam.ScreenToViewportPoint(Input.mousePosition);
                    isDragging = true;
                }

                if (Input.GetMouseButton(2) && isDragging)
                {
                    Vector3 difference = dragOrigin - cam.ScreenToViewportPoint(Input.mousePosition);
                    move += new Vector3(difference.x * panSpeed, 0f, difference.y * panSpeed);
                    dragOrigin = cam.ScreenToViewportPoint(Input.mousePosition);
                }

                if (Input.GetMouseButtonUp(2))
                {
                    isDragging = false;
                }
            }

            // Apply movement
            if (move != Vector3.zero)
            {
                transform.Translate(move * panSpeed * Time.deltaTime, Space.World);
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Vector3 pos = transform.position;
                pos.y -= scroll * zoomSpeed;
                pos.y = Mathf.Clamp(pos.y, minZoom, maxZoom);
                transform.position = pos;
            }
        }

        public void FocusOnPosition(Vector3 worldPosition)
        {
            Vector3 newPos = transform.position;
            newPos.x = worldPosition.x;
            newPos.z = worldPosition.z - 10f; // Offset for camera angle
            transform.position = newPos;
        }
    }
}
