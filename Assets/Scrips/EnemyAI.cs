using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform[] patrolPoints;
    public Transform player;

    [Header("Ranges")]
    public float detectionRange = 10f;
    public float loseRange = 14f;

    [Header("Timing")]
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
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Detect player
        if (distanceToPlayer <= detectionRange)
        {
            currentState = State.Chase;
        }
        else if (distanceToPlayer > loseRange && currentState == State.Chase)
        {
            currentState = State.Patrol;
            GoToNearestPatrolPoint();
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

        // Wait when reached point
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
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
}