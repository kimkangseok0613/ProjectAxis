using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("일반 이동 속도")]
    [SerializeField] private float walkSpeed = 7f;

    [Tooltip("달리기 속도")]
    [SerializeField] private float runSpeed = 11f;

    [Tooltip("웅크렸을 때 이동 속도")]
    [SerializeField] private float crouchSpeed = 3f;

    [Header("점프 설정")]
    [Tooltip("점프 높이")]
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("웅크리기 설정")]
    [Tooltip("서 있을 때 캡슐 높이")]
    [SerializeField] private float standingHeight = 2f;

    [Tooltip("웅크렸을 때 캡슐 높이")]
    [SerializeField] private float crouchingHeight = 1.2f;

    [Header("바닥 판정 설정")]
    [Tooltip("이 값 이상으로 위쪽을 향한 면만 바닥으로 판정")]
    [Range(0f, 1f)]
    [SerializeField] private float groundNormalThreshold = 0.6f;

    [Tooltip("바닥에서 떨어진 직후 점프를 허용하는 시간")]
    [SerializeField] private float groundGraceTime = 0.1f;

    [Header("벽 충돌 설정")]
    [Tooltip("벽으로 판정할 때 사용하는 수직 방향 기준값")]
    [Range(0f, 1f)]
    [SerializeField] private float wallNormalThreshold = 0.3f;

    [Header("카메라 연결")]
    [Tooltip("Main Camera에 연결된 CameraController")]
    [SerializeField] private CameraController cameraController;

    private Rigidbody playerRigidbody;
    private CapsuleCollider capsuleCollider;

    private Vector3 movementDirection;
    private Vector3 wallNormal;

    private float currentSpeed;
    private float lastGroundedTime = -100f;

    private bool isCrouching;
    private bool jumpRequested;
    private bool touchingWall;

    private bool IsGrounded =>
        Time.time - lastGroundedTime <= groundGraceTime;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        capsuleCollider.center = Vector3.zero;
        capsuleCollider.height = standingHeight;

        playerRigidbody.useGravity = true;
        playerRigidbody.isKinematic = false;
        playerRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        playerRigidbody.collisionDetectionMode =
            CollisionDetectionMode.Continuous;
    }

    private void Update()
    {
        HandleMovementInput();
        HandleCrouchInput();
        UpdateCameraRunningState();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        HandleHorizontalMovement();
        HandleJump();

        // 다음 물리 프레임에서 벽 충돌 정보를 다시 수집한다.
        touchingWall = false;
        wallNormal = Vector3.zero;
    }

    private void HandleMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector2 input = Vector2.ClampMagnitude(
            new Vector2(horizontal, vertical), 1f);

        float cameraYaw = cameraController != null
            ? cameraController.CameraYaw
            : transform.eulerAngles.y;

        Quaternion cameraRotation =
            Quaternion.Euler(0f, cameraYaw, 0f);

        Vector3 forward = cameraRotation * Vector3.forward;
        Vector3 right = cameraRotation * Vector3.right;

        movementDirection = forward * input.y + right * input.x;
        movementDirection = Vector3.ClampMagnitude(
            movementDirection, 1f);

        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = runSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }
    }

    private void HandleHorizontalMovement()
    {
        Vector3 desiredVelocity = movementDirection * currentSpeed;

        if (touchingWall)
        {
            // 벽 안쪽으로 향하는 속도만 제거한다.
            // 벽에서 멀어지거나 옆으로 가는 입력은 유지한다.
            float intoWall = Vector3.Dot(desiredVelocity, wallNormal);

            if (intoWall < 0f)
            {
                desiredVelocity -= wallNormal * intoWall;
            }
        }

        // X, Z축 이동만 제어하고 Y축 속도는 유지한다.
        // 따라서 공중에서는 중력에 따라 계속 떨어진다.
        Vector3 velocity = playerRigidbody.linearVelocity;

        velocity.x = desiredVelocity.x;
        velocity.z = desiredVelocity.z;

        playerRigidbody.linearVelocity = velocity;
    }

    private void HandleJump()
    {
        if (jumpRequested && IsGrounded)
        {
            Vector3 velocity = playerRigidbody.linearVelocity;

            velocity.y = Mathf.Sqrt(
                jumpHeight * -2f * Physics.gravity.y);

            playerRigidbody.linearVelocity = velocity;
        }

        jumpRequested = false;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Ground"))
        {
            return;
        }

        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 normal = contact.normal;

            // 위쪽을 향한 면에 닿았을 때만 바닥으로 판정한다.
            if (Vector3.Dot(normal, Vector3.up) >= groundNormalThreshold)
            {
                lastGroundedTime = Time.time;
                continue;
            }

            // 수직에 가까운 면은 벽으로 처리한다.
            if (Mathf.Abs(normal.y) <= wallNormalThreshold)
            {
                touchingWall = true;
                wallNormal = normal;

                // 벽을 향하는 기존 속도만 제거한다.
                // 수직 속도는 변경하지 않아 중력을 유지한다.
                Vector3 velocity = playerRigidbody.linearVelocity;
                float intoWall = Vector3.Dot(velocity, wallNormal);

                if (intoWall < 0f)
                {
                    velocity -= wallNormal * intoWall;
                    playerRigidbody.linearVelocity = velocity;
                }
            }
        }
    }

    private void HandleCrouchInput()
    {
        isCrouching = Input.GetKey(KeyCode.LeftControl);

        float targetHeight = isCrouching
            ? crouchingHeight
            : standingHeight;

        capsuleCollider.height = Mathf.Lerp(
            capsuleCollider.height,
            targetHeight,
            Time.deltaTime * 10f);

        capsuleCollider.center = Vector3.zero;
    }

    private void UpdateCameraRunningState()
    {
        if (cameraController == null)
        {
            return;
        }

        bool isRunning =
            !isCrouching && Input.GetKey(KeyCode.LeftShift);

        cameraController.SetRunning(isRunning);
    }
}
