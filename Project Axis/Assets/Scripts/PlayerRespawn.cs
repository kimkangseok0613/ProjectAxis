using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerRespawn : MonoBehaviour
{
    [Header("리스폰 설정")]
    [Tooltip("플레이어가 다시 생성될 위치")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("이 월드 Y 좌표보다 아래로 떨어지면 리스폰")]
    [SerializeField] private float fallLimitY = -12f;

    private Rigidbody playerRigidbody;
    private bool isRespawning;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!isRespawning && transform.position.y < fallLimitY)
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        if (spawnPoint == null)
        {
            Debug.LogError(
                "PlayerRespawn: Spawn Point를 연결해야 합니다.",
                this
            );
            return;
        }

        isRespawning = true;

        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
        playerRigidbody.position = spawnPoint.position;
        playerRigidbody.rotation = spawnPoint.rotation;

        isRespawning = false;
    }
}
