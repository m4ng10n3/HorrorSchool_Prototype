using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI del teacher.
/// Aggiungere questo componente all'oggetto Teacher in scena insieme a un NavMeshAgent.
/// Il teacher si attiva quando il tempo di una richiesta scade (OnRequestFailed).
///
/// Stati:
///   IDLE   – fermo alla cattedra
///   SEARCH – vaga cercando il player
///   CHASE  – ha visto il player, lo insegue a tutta velocità
///   LOST   – ha perso il player o è rimasto bloccato; pattuiglia intorno
///            all'ultima posizione nota prima di tornare in Search
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class TeacherAI : MonoBehaviour
{
    public enum AIState { Idle, Search, Chase, Lost, Caught }

    // ── Riferimenti ───────────────────────────────────────────────
    [Header("Riferimenti")]
    public Transform player;
    public TeacherRequestSystem teacherSystem;

    // ── Visione ───────────────────────────────────────────────────
    [Header("Visione")]
    public float sightRange  = 12f;
    [Range(1f, 180f)]
    public float sightAngle  = 70f;        // semi-angolo (gradi) del cono visivo
    public LayerMask obstaclesMask;        // layer muri/porte che bloccano la LOS

    // ── Velocità ──────────────────────────────────────────────────
    [Header("Velocità")]
    public float searchSpeed = 2.5f;
    public float chaseSpeed  = 5.5f;

    // ── Pattuglia ─────────────────────────────────────────────────
    [Header("Pattuglia")]
    public float searchWanderRadius = 10f; // raggio wander in Search
    public float lostPatrolRadius   = 5f;  // raggio patrol in Lost
    public float waypointTolerance  = 1.2f;
    public float waypointWaitTime   = 1.5f;
    public float lostPatrolTimeout  = 20f; // dopo N sec in Lost → torna Search

    // ── Rilevamento blocco ────────────────────────────────────────
    [Header("Rilevamento blocco (porta / ostacolo)")]
    public float stuckSpeedThreshold = 0.4f;
    public float stuckTimeLimit      = 2.5f;

    // ── Catch ─────────────────────────────────────────────────────
    [Header("Cattura player")]
    public float catchDistance = 1.5f;

    // ── Evento ────────────────────────────────────────────────────
    public event System.Action OnPlayerCaught;

    // ── Stato pubblico ────────────────────────────────────────────
    public AIState CurrentState { get; private set; } = AIState.Idle;

    // ── Privati ───────────────────────────────────────────────────
    private NavMeshAgent agent;
    private Vector3      startPosition;
    private Vector3      lastKnownPlayerPos;

    private bool  waitingAtWaypoint;
    private float waypointWaitTimer;
    private float lostPatrolTimer;
    private float stuckTimer;

    // ─────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (teacherSystem == null)
            teacherSystem = FindFirstObjectByType<TeacherRequestSystem>();

        if (player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
    }

    private void Start()
    {
        startPosition = transform.position;

        if (teacherSystem != null)
        {
            teacherSystem.OnRequestFailed += HandleRequestFailed;
            teacherSystem.OnAllDelivered  += EnterIdle;
        }

        EnterIdle();
    }

    private void OnDestroy()
    {
        if (teacherSystem != null)
        {
            teacherSystem.OnRequestFailed -= HandleRequestFailed;
            teacherSystem.OnAllDelivered  -= EnterIdle;
        }
    }

    private void Update()
    {
        // Catch check – attivo in tutti gli stati tranne Idle e Caught
        if (CurrentState != AIState.Idle && CurrentState != AIState.Caught)
            CheckCatchPlayer();

        switch (CurrentState)
        {
            case AIState.Idle:   UpdateIdle();   break;
            case AIState.Search: UpdateSearch(); break;
            case AIState.Chase:  UpdateChase();  break;
            case AIState.Lost:   UpdateLost();   break;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // IDLE – fermo alla cattedra
    // ─────────────────────────────────────────────────────────────

    private void EnterIdle()
    {
        CurrentState = AIState.Idle;
        agent.isStopped = true;
        agent.ResetPath();
        Debug.Log("[TeacherAI] → IDLE");
    }

    private void UpdateIdle()
    {
        // Nessuna logica: attende l'evento OnRequestFailed
    }

    // ─────────────────────────────────────────────────────────────
    // SEARCH – vaga cercando il player
    // ─────────────────────────────────────────────────────────────

    private void EnterSearch()
    {
        CurrentState = AIState.Search;
        agent.isStopped = false;
        agent.speed = searchSpeed;
        waitingAtWaypoint = false;
        stuckTimer = 0f;
        PickRandomWaypoint(transform.position, searchWanderRadius);
        Debug.Log("[TeacherAI] → SEARCH");
    }

    private void UpdateSearch()
    {
        if (CanSeePlayer())
        {
            EnterChase();
            return;
        }

        if (waitingAtWaypoint)
        {
            waypointWaitTimer -= Time.deltaTime;
            if (waypointWaitTimer <= 0f)
            {
                waitingAtWaypoint = false;
                PickRandomWaypoint(transform.position, searchWanderRadius);
            }
            return;
        }

        if (HasReachedDestination())
        {
            waitingAtWaypoint = true;
            waypointWaitTimer = waypointWaitTime;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // CHASE – ha visto il player, lo insegue
    // ─────────────────────────────────────────────────────────────

    private void EnterChase()
    {
        CurrentState = AIState.Chase;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        stuckTimer = 0f;
        Debug.Log("[TeacherAI] → CHASE");
    }

    private void UpdateChase()
    {
        if (player == null) return;

        lastKnownPlayerPos = player.position;
        agent.SetDestination(player.position);

        // Perso la visione del player → Lost
        if (!CanSeePlayer())
        {
            EnterLost();
            return;
        }

        // Bloccato (es. porta chiusa) → Lost
        if (!agent.pathPending && agent.velocity.magnitude < stuckSpeedThreshold)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeLimit)
            {
                Debug.Log("[TeacherAI] Bloccato! → LOST");
                EnterLost();
                return;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // LOST – pattuiglia intorno all'ultima posizione nota
    // ─────────────────────────────────────────────────────────────

    private void EnterLost()
    {
        CurrentState = AIState.Lost;
        agent.isStopped = false;
        agent.speed = searchSpeed;
        lostPatrolTimer = 0f;
        waitingAtWaypoint = false;
        stuckTimer = 0f;
        // Raggiungi prima l'ultima posizione nota
        agent.SetDestination(lastKnownPlayerPos);
        Debug.Log("[TeacherAI] → LOST");
    }

    private void UpdateLost()
    {
        // Se rivede il player → Chase
        if (CanSeePlayer())
        {
            EnterChase();
            return;
        }

        // Timeout del patrol → torna a cercare
        lostPatrolTimer += Time.deltaTime;
        if (lostPatrolTimer >= lostPatrolTimeout)
        {
            EnterSearch();
            return;
        }

        // Pattuiglia intorno all'ultima posizione nota
        if (waitingAtWaypoint)
        {
            waypointWaitTimer -= Time.deltaTime;
            if (waypointWaitTimer <= 0f)
            {
                waitingAtWaypoint = false;
                PickRandomWaypoint(lastKnownPlayerPos, lostPatrolRadius);
            }
            return;
        }

        if (HasReachedDestination())
        {
            waitingAtWaypoint = true;
            waypointWaitTimer = waypointWaitTime;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // ── Catch ─────────────────────────────────────────────────────

    private void CheckCatchPlayer()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) <= catchDistance)
            CatchPlayer();
    }

    private void CatchPlayer()
    {
        CurrentState = AIState.Caught;
        agent.isStopped = true;
        agent.ResetPath();
        Debug.Log("[TeacherAI] Giocatore catturato! GAME OVER");
        OnPlayerCaught?.Invoke();
    }

    // Helpers
    // ─────────────────────────────────────────────────────────────

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 origin   = transform.position + Vector3.up * 1.5f;
        Vector3 toPlayer = player.position + Vector3.up * 1f - origin;

        // Distanza
        if (toPlayer.magnitude > sightRange) return false;

        // Angolo (cono frontale)
        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > sightAngle) return false;

        // Raycast: controlla ostacoli tra teacher e player
        if (obstaclesMask != 0 &&
            Physics.Raycast(origin, toPlayer.normalized, toPlayer.magnitude, obstaclesMask))
            return false;

        return true;
    }

    private bool HasReachedDestination()
    {
        if (agent.pathPending) return false;
        if (agent.remainingDistance > waypointTolerance) return false;
        return true;
    }

    /// <summary>Sceglie un punto casuale sul NavMesh attorno a <paramref name="center"/>.</summary>
    private void PickRandomWaypoint(Vector3 center, float radius)
    {
        for (int i = 0; i < 12; i++)
        {
            Vector2 rand   = Random.insideUnitCircle * radius;
            Vector3 candidate = center + new Vector3(rand.x, 0f, rand.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }

        // Fallback: torna alla posizione iniziale
        agent.SetDestination(startPosition);
    }

    // ─────────────────────────────────────────────────────────────
    // Gizmos (visibili solo in editor con l'oggetto selezionato)
    // ─────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;

        // Raggio di vista
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, sightRange);

        // Bordi del cono visivo
        Vector3 fwd   = transform.forward * sightRange;
        Vector3 left  = Quaternion.Euler(0f, -sightAngle, 0f) * fwd;
        Vector3 right = Quaternion.Euler(0f,  sightAngle, 0f) * fwd;
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.5f);
        Gizmos.DrawRay(pos, left);
        Gizmos.DrawRay(pos, right);

        // Raggio patrol Lost
        if (CurrentState == AIState.Lost)
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
            Gizmos.DrawWireSphere(lastKnownPlayerPos, lostPatrolRadius);
            Gizmos.DrawLine(pos, lastKnownPlayerPos);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Event callbacks
    // ─────────────────────────────────────────────────────────────

    private void HandleRequestFailed(ItemData _) => EnterSearch();
}
