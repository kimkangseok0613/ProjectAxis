using System.Collections.Generic;
using UnityEngine;

public class MapRotation : MonoBehaviour
{
    [Header("회전 설정")]
    [Tooltip("통로의 회전 속도")]
    [SerializeField] private float rotationSpeed = 180f;

    [Tooltip("키 입력 한 번당 회전하는 각도")]
    [SerializeField] private float rotationAngle = 90f;

    [Header("카메라 설정")]
    [Tooltip("실제로 플레이어 시점을 표시하는 Main Camera")]
    [SerializeField] private Camera playerCamera;

    private readonly Queue<float> rotationQueue = new Queue<float>();

    private float targetRotationZ;
    private bool isRotating;

    private void Start()
    {
        targetRotationZ = transform.localEulerAngles.z;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
        {
            Debug.LogError(
                "MapRotation: Player Camera를 연결해야 합니다.", this);
        }
    }

    private void Update()
    {
        HandleRotationInput();
        RotateMap();
    }

    private void HandleRotationInput()
    {
        if (playerCamera == null)
        {
            return;
        }

        // 카메라가 실제로 바라보는 월드 방향을 확인한다.
        Vector3 cameraForward = playerCamera.transform.forward;

        // 상하 시선은 제외하고 수평 방향만 비교한다.
        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        cameraForward.Normalize();

        // Z+를 바라보면 1, Z-를 바라보면 -1
        float directionSign = cameraForward.z >= 0f ? 1f : -1f;

        // Q와 E의 회전 방향을 카메라 방향에 맞춰 반전한다.
        if (Input.GetKeyDown(KeyCode.Q))
        {
            rotationQueue.Enqueue(rotationAngle * directionSign);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            rotationQueue.Enqueue(-rotationAngle * directionSign);
        }
    }

    private void RotateMap()
    {
        // 현재 회전이 끝난 뒤 다음 입력을 실행한다.
        if (!isRotating)
        {
            if (rotationQueue.Count == 0)
            {
                return;
            }

            targetRotationZ += rotationQueue.Dequeue();
            isRotating = true;
        }

        float currentZ = transform.localEulerAngles.z;

        float nextZ = Mathf.MoveTowardsAngle(
            currentZ,
            targetRotationZ,
            rotationSpeed * Time.deltaTime
        );

        transform.localRotation = Quaternion.Euler(
            0f, 0f, nextZ);

        if (Mathf.Abs(Mathf.DeltaAngle(nextZ, targetRotationZ)) < 0.01f)
        {
            transform.localRotation = Quaternion.Euler(
                0f, 0f, targetRotationZ);

            isRotating = false;
        }
    }
}
