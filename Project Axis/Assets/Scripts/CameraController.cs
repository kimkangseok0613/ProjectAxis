using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("카메라 대상")]
    [Tooltip("카메라가 따라갈 플레이어")]
    [SerializeField] private Transform target;

    [Header("마우스 설정")]
    [Tooltip("마우스 시점 회전 감도")]
    [SerializeField] private float mouseSensitivity = 2f;
    [Tooltip("카메라가 아래로 회전할 수 있는 최소 각도")]
    [SerializeField] private float minCameraAngle = -30f;
    [Tooltip("카메라가 위로 회전할 수 있는 최대 각도")]
    [SerializeField] private float maxCameraAngle = 60f;

    [Header("카메라 위치")]
    [Tooltip("플레이어와 카메라 사이의 거리")]
    [SerializeField] private float cameraDistance = 4f;
    [Tooltip("플레이어 기준 카메라 높이")]
    [SerializeField] private float cameraHeight = 1f;

    [Header("카메라 추적 설정")]
    [Tooltip("일반 이동 시 카메라 추적 시간")]
    [SerializeField] private float cameraSmoothTime = 0.15f;
    [Tooltip("달리기 시 카메라 추적 시간")]
    [SerializeField] private float runCameraSmoothTime = 0.08f;

    private float cameraYaw;
    private float cameraPitch;
    private Vector3 followPosition;
    private Vector3 followVelocity;
    private bool isRunning;

    // 플레이어 이동 스크립트에서 사용하는 카메라의 수평 회전값
    public float CameraYaw => cameraYaw;

    public void SetRunning(bool running)
    {
        isRunning = running;
    }

    private void Start()
    {
        if (!ValidateTarget())
        {
            enabled = false;
            return;
        }

        InitializeView();
        SetCursorLocked(true);
    }

    private void Update()
    {
        HandleCursorInput();
        HandleMouseInput();
    }

    private void LateUpdate()
    {
        UpdateCameraPosition();
    }

    private bool ValidateTarget()
    {
        if (target != null)
        {
            return true;
        }

        Debug.LogError(
            "CameraController: Target에 Player를 연결해야 합니다.",
            this
        );

        return false;
    }

    private void InitializeView()
    {
        Vector3 angles = transform.eulerAngles;

        cameraYaw = angles.y;
        cameraPitch = angles.x;

        if (cameraPitch > 180f)
        {
            cameraPitch -= 360f;
        }

        followPosition = target.position + Vector3.up * cameraHeight;
        ApplyCameraTransform();
    }

    private void HandleCursorInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLocked(false);
        }

        if (Input.GetMouseButtonDown(0))
        {
            SetCursorLocked(true);
        }
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !locked;
    }

    private void HandleMouseInput()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        cameraYaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        cameraPitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;

        cameraPitch = Mathf.Clamp(
            cameraPitch,
            minCameraAngle,
            maxCameraAngle
        );
    }

    private void UpdateCameraPosition()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position + Vector3.up * cameraHeight;
        float smoothTime = isRunning
            ? runCameraSmoothTime
            : cameraSmoothTime;

        followPosition = Vector3.SmoothDamp(
            followPosition,
            targetPosition,
            ref followVelocity,
            smoothTime
        );

        ApplyCameraTransform();
    }

    private void ApplyCameraTransform()
    {
        Quaternion rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        Vector3 offset = rotation * Vector3.back * cameraDistance;

        transform.position = followPosition + offset;
        transform.LookAt(followPosition);
    }
}
