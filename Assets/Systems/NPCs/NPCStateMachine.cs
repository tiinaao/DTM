using UnityEngine;

public class NPCStateMachine : MonoBehaviour
{
    public enum NPCState { Idle, Patrol }
    public enum PatrolMode { Loop, PingPong }

    [SerializeField] private NPCState state = NPCState.Idle;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private PatrolMode patrolMode = PatrolMode.PingPong;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float waypointTolerance = 0.1f;

    private NPCInteraction npcInteraction;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private int targetIndex;
    private int direction = 1;

    private void Awake()
    {
        npcInteraction = GetComponent<NPCInteraction>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (state != NPCState.Patrol || waypoints == null || waypoints.Length == 0)
        {
            SetMoving(false);
            return;
        }

        if (npcInteraction != null && npcInteraction.IsInteracting)
        {
            SetMoving(false);
            return;
        }

        Transform target = waypoints[targetIndex];
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= waypointTolerance * waypointTolerance)
        {
            AdvanceWaypoint();
            SetMoving(false);
            return;
        }

        Vector3 move = toTarget.normalized * speed * Time.deltaTime;
        transform.position += move;

        if (spriteRenderer != null)
            spriteRenderer.flipX = toTarget.x > 0f;

        SetMoving(true);
    }

    private void AdvanceWaypoint()
    {
        if (waypoints.Length == 1) return;

        if (patrolMode == PatrolMode.Loop)
        {
            targetIndex = (targetIndex + 1) % waypoints.Length;
            return;
        }

        int next = targetIndex + direction;
        if (next < 0 || next >= waypoints.Length)
        {
            direction = -direction;
            next = targetIndex + direction;
        }

        targetIndex = Mathf.Clamp(next, 0, waypoints.Length - 1);
    }

    private void SetMoving(bool moving)
    {
        if (animator == null) return;
        animator.SetBool("isMoving", moving);
    }
}
