using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform[] patrolPoints;
    public Transform player;

    [Header("Detección")]
    public float detectionRange = 10f;
    public float loseRange = 14f;
    [Range(0f, 360f)]
    public float viewAngle = 90f;   // 90 total = 45 a cada lado
    public float eyeHeight = 1.5f;
    public LayerMask obstacleMask;  // paredes
    public LayerMask playerMask;    // capa del jugador

    [Header("Patrulla")]
    public float waitAtPoint = 2f;

    private NavMeshAgent agent;
    private int currentPointIndex = 0;
    private float waitTimer = 0f;

    private enum State { Patrol, Chase }
    private State currentState = State.Patrol;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (patrolPoints.Length > 0)
        {
            GoToNextPatrolPoint();
        }
    }

    void Update()
    {
        bool canSeePlayer = CanSeePlayer();

        if (canSeePlayer)
        {
            currentState = State.Chase;
        }
        else if (currentState == State.Chase)
        {
            float dist = Vector3.Distance(transform.position, player.position);

            if (dist > loseRange)
            {
                currentState = State.Patrol;
                GoToNearestPatrolPoint();
            }
        }

        switch (currentState)
        {
            case State.Patrol:
                HandlePatrol();
                break;

            case State.Chase:
                HandleChase();
                break;
        }
    }

    void HandlePatrol()
    {
        if (patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f)
            {
                waitTimer += Time.deltaTime;

                if (waitTimer >= waitAtPoint)
                {
                    GoToNextPatrolPoint();
                    waitTimer = 0f;
                }
            }
        }
    }

    void HandleChase()
    {
        agent.SetDestination(player.position);
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 enemyEyes = transform.position + Vector3.up * eyeHeight;

        Collider playerCol = player.GetComponent<Collider>();
        Vector3 playerTarget = playerCol != null ? playerCol.bounds.center : player.position;

        Vector3 dirToPlayer = playerTarget - enemyEyes;
        float distanceToPlayer = dirToPlayer.magnitude;

        if (distanceToPlayer > detectionRange)
            return false;

        float angle = Vector3.Angle(transform.forward, dirToPlayer.normalized);
        if (angle > viewAngle * 0.5f)
            return false;

        LayerMask visionMask = playerMask | obstacleMask;

        Debug.DrawRay(enemyEyes, dirToPlayer.normalized * distanceToPlayer, Color.green);

        if (Physics.Raycast(
                enemyEyes,
                dirToPlayer.normalized,
                out RaycastHit hit,
                distanceToPlayer,
                visionMask,
                QueryTriggerInteraction.Ignore))
        {
            Debug.Log("Hit: " + hit.collider.name + " | Layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));
            return ((1 << hit.collider.gameObject.layer) & playerMask) != 0;
        }

        return false;
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[currentPointIndex].position);
        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
    }

    void GoToNearestPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        float bestDistance = Mathf.Infinity;
        int bestIndex = 0;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float d = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (d < bestDistance)
            {
                bestDistance = d;
                bestIndex = i;
            }
        }

        currentPointIndex = bestIndex;
        agent.SetDestination(patrolPoints[currentPointIndex].position);
        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
    }

    void OnDrawGizmosSelected()
    {
        // rango de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // rango para perder al jugador
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseRange);

        // líneas del cono de visión
        Vector3 leftBoundary = DirFromAngle(-viewAngle / 2f);
        Vector3 rightBoundary = DirFromAngle(viewAngle / 2f);

        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Gizmos.DrawLine(origin, origin + leftBoundary * detectionRange);
        Gizmos.DrawLine(origin, origin + rightBoundary * detectionRange);
    }

    Vector3 DirFromAngle(float angleDegrees)
    {
        float rad = (transform.eulerAngles.y + angleDegrees) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
    }
}