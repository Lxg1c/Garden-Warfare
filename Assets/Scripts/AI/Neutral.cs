using Core.Components;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using System.Collections;


[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class NeutralAI : MonoBehaviourPun
{
    [Header("Settings")]
    public float detectionRadius = 6f;
    public float attackRange = 4f;
    public float aggressionDuration = 5f;
    public float maxDistanceFromHome = 10f;
    public Transform homePoint;
    public string playerTag = "Player";
    public static event System.Action<Transform, Transform> OnGroupAggro; 
    public float groupAggroRadius = 4f;
    
    [Header("Group Settings")]
    public SpawnPointGroup spawnGroup;
    private SpawnPointGroup _assignedGroup;

    [Header("Combat")]
    public float attackDamage = 10f;
    public float attackCooldown = 1f;

    private NavMeshAgent _agent;
    private Health _health;
    private Transform _currentTarget;
    private Vector3 _homePosition;
    private float _lastAttackTime;
    private float _aggroEndTime = 3f;
    private bool _hasAggro;
    private Coroutine _returnCoroutine;
    private float _combatTimer;
    private bool _combatTimerActive;
    private float _lastDetectionTime;

    // Простые состояния
    private enum State { Idle, Chasing, Attacking, Returning }
    private State _currentState = State.Idle;

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        _homePosition = homePoint != null ? homePoint.position : transform.position;
        
        _health.OnDamaged += OnDamaged;
        
        _agent.stoppingDistance = 3f;
        _agent.autoBraking = true;
        
        if (spawnGroup != null)
        {
            spawnGroup.RegisterNeutral(this);
        }
        else
        {
            SpawnPointGroup parentGroup = GetComponentInParent<SpawnPointGroup>();
            if (parentGroup != null)
            {
                parentGroup.RegisterNeutral(this);
            }
        }
    }
    
    public void SetSpawnGroup(SpawnPointGroup group)
    {
        _assignedGroup = group;
    }

    private void Update()
    {
        CheckAttackRange();
        
        if (Time.time - _lastDetectionTime > 0.5f)
        {
            CheckForNearbyPlayers();
            _lastDetectionTime = Time.time;
        }
    
        if (_combatTimerActive)
        {
            _combatTimer -= Time.deltaTime;
            if (_combatTimer <= 0f)
            {
                Debug.Log("Таймер боя истек, возвращаемся на базу");
                StartReturningHome();
                _combatTimerActive = false;
            }
        }
    
        switch (_currentState)
        {
            case State.Idle:
                UpdateIdle();
                break;
            case State.Chasing:
                UpdateChasing();
                break;
            case State.Attacking:
                UpdateAttacking();
                break;
            case State.Returning:
                UpdateReturning();
                break;
        }
    }
    
    public void OnGroupAggroTriggered(Transform aggroSource, Transform target)
    {
        if (aggroSource == transform)
            return;
        
        Health targetHealth = target.GetComponent<Health>();
        if (targetHealth != null && targetHealth.GetHealth() <= 0)
            return;

        if (_hasAggro && _currentTarget == target)
        {
            _aggroEndTime = Time.time + aggressionDuration;
            _combatTimer = 3f;
            return;
        }

        Debug.Log($"Получаем групповой агро от {aggroSource.name} на цель {target.name}");
        SetAggro(target);
    }
    
    private void CheckAttackRange()
    {
        if (_hasAggro && _currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
        
            if (_currentState == State.Chasing && distanceToTarget <= attackRange)
            {
                _currentState = State.Attacking;
                _agent.isStopped = true;
            }
            else if (_currentState == State.Attacking && distanceToTarget > attackRange)
            {
                _currentState = State.Chasing;
                _agent.isStopped = false;
            }
        }
    }

    private void UpdateIdle()
    {
        float distanceFromHome = Vector3.Distance(transform.position, _homePosition);
        if (distanceFromHome > maxDistanceFromHome)
        {
            StartReturningHome();
            return;
        }

        if (_hasAggro && _currentTarget != null)
        {
            _currentState = State.Chasing;
            _agent.isStopped = false;
        }
    }

    private void UpdateChasing()
    {
        if (_currentTarget == null)
        {
            StartReturningHome();
            return;
        }

        if (Time.time > _aggroEndTime)
        {
            StartReturningHome();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
        if (distanceToTarget <= attackRange)
        {
            _currentState = State.Attacking;
            _agent.isStopped = true;
        }
        else
        {
            _agent.SetDestination(_currentTarget.position);
        }
    }

    private void UpdateAttacking()
    {
        if (_currentTarget == null)
        {
            StartReturningHome();
            return;
        }

        if (Time.time > _aggroEndTime)
        {
            StartReturningHome();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
        
        if (distanceToTarget > attackRange)
        {
            _currentState = State.Chasing;
            _agent.isStopped = false;
            return;
        }

        RotateTowardsTarget();

        if (Time.time >= _lastAttackTime + attackCooldown)
        {
            AttackTarget();
        }
    }

    private void UpdateReturning()
    {
        float distanceToHome = Vector3.Distance(transform.position, _homePosition);
        
        if (distanceToHome <= 1f)
        {
            _agent.isStopped = true;
            _currentState = State.Idle;
            _hasAggro = false;
            _currentTarget = null;
            
            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
                _returnCoroutine = null;
            }
            return;
        }

        if (!_agent.hasPath || Vector3.Distance(_agent.destination, _homePosition) > 0.5f)
        {
            _agent.SetDestination(_homePosition);
        }
    }

    private void StartReturningHome()
    {
        if (_currentState == State.Returning) return;
    
        _currentState = State.Returning;
        _hasAggro = false;
        _agent.isStopped = false;
        _agent.SetDestination(_homePosition);
    
        UnsubscribeFromTargetDeath();
        _combatTimerActive = false;
    
        if (_returnCoroutine != null)
            StopCoroutine(_returnCoroutine);
    
        _returnCoroutine = StartCoroutine(ReturnHomeRoutine());
    }

    private IEnumerator ReturnHomeRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (_currentState == State.Returning)
        {
            _agent.SetDestination(_homePosition);
        }
    }

    private void SetAggro(Transform target)
    {
        Health targetHealth = target.GetComponent<Health>();
        if (targetHealth != null && targetHealth.GetHealth() <= 0)
        {
            Debug.Log($"Цель {target.name} мертва, игнорируем агро");
            return;
        }

        if (_hasAggro && _currentTarget == target)
        {
            _aggroEndTime = Time.time + aggressionDuration;
            _combatTimer = 3f;
            return;
        }

        Debug.Log($"SetAggro вызван с целью: {target.name}");
    
        UnsubscribeFromTargetDeath();
    
        _currentTarget = target;
        _hasAggro = true;
        _aggroEndTime = Time.time + aggressionDuration;

        SubscribeToTargetDeath();

        _combatTimer = 3f;
        _combatTimerActive = true;
        Debug.Log("Запущен таймер преследования: 3 секунды");

        if (_returnCoroutine != null)
        {
            StopCoroutine(_returnCoroutine);
            _returnCoroutine = null;
        }

        _currentState = State.Chasing;
        _agent.isStopped = false;
    
        if (_assignedGroup != null)
        {
            _assignedGroup.NotifyGroupAggro(transform, target);
        }
        else if (spawnGroup != null)
        {
            spawnGroup.NotifyGroupAggro(transform, target);
        }
    }

    private void RotateTowardsTarget()
    {
        if (_currentTarget == null) return;

        Vector3 direction = (_currentTarget.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    private void AttackTarget()
    {
        Debug.Log("Вызываем attack target");
        if (_currentTarget == null) return;

        Health targetHealth = _currentTarget.GetComponent<Health>();
        if (targetHealth != null)
        {
            Debug.Log("Есть компонент здоровья");
            if (PhotonNetwork.IsConnected && photonView != null)
            {
                Debug.Log("Выполняем атаку по фотону");
                int attackerViewId = photonView.ViewID;
                PhotonView targetPhotonView = _currentTarget.GetComponent<PhotonView>();
                if (targetPhotonView != null)
                {
                    Debug.Log($"Вызываем RPC атаки на цели {_currentTarget.name}");
                    targetPhotonView.RPC("TakeDamageRPC", RpcTarget.All, attackDamage, attackerViewId);
                }
                else
                {
                    Debug.LogWarning($"У цели {_currentTarget.name} нет PhotonView!");
                }
            }
            else
            {
                Debug.Log("Атакуем игрока (оффлайн)");
                targetHealth.TakeDamage(attackDamage, transform);
            }
    
            Debug.Log($"Атаковал игрока! Урон: {attackDamage}");
        }
        else
        {
            Debug.LogWarning("У цели нет компонента Health!");
        }

        _lastAttackTime = Time.time;
        _aggroEndTime = Time.time + aggressionDuration;
    }

    private void OnDamaged(Transform attacker)
    {
        if (attacker != null)
        {
            SetAggro(attacker);
            StartCoroutine(DamageFlash());
        
            OnGroupAggro?.Invoke(transform, attacker);
        }
    }

    private IEnumerator DamageFlash()
    {
        var meshRenderer = GetComponent<Renderer>(); // Переименовал переменную
        if (meshRenderer != null)
        {
            Color originalColor = meshRenderer.material.color;
            meshRenderer.material.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            meshRenderer.material.color = originalColor;
        }
    }

    public void OnEnterAttackRange(Transform target)
    {
        if (_hasAggro && _currentTarget == target && _currentState == State.Chasing)
        {
            _currentState = State.Attacking;
            _agent.isStopped = true;
        }
    }

    public void OnExitAttackRange(Transform target)
    {
        if (_hasAggro && _currentTarget == target && _currentState == State.Attacking)
        {
            _currentState = State.Chasing;
            _agent.isStopped = false;
        }
    }
    
    private void SubscribeToTargetDeath()
    {
        if (_currentTarget != null)
        {
            Health targetHealth = _currentTarget.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.OnDeath += OnTargetDied;
                Debug.Log($"Подписались на смерть цели: {_currentTarget.name}");
            }
        }
    }

    private void UnsubscribeFromTargetDeath()
    {
        if (_currentTarget != null)
        {
            Health targetHealth = _currentTarget.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.OnDeath -= OnTargetDied;
                Debug.Log($"Отписались от смерти цели: {_currentTarget.name}");
            }
        }
    }

    private void OnTargetDied(Transform deadTransform)
    {
        Debug.Log($"Цель {deadTransform.name} умерла, возвращаемся на базу");
    
        if (_currentTarget == deadTransform)
        {
            StartReturningHome();
        }
    }
    
    private void CheckForNearbyPlayers()
    {
        if (_hasAggro || _currentState == State.Returning) return;
    
        Collider[] hitColliders = new Collider[10];
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, hitColliders);
    
        for (int i = 0; i < numColliders; i++)
        {
            Collider hitCollider = hitColliders[i];
        
            if (hitCollider.CompareTag(playerTag))
            {
                Transform player = hitCollider.transform;
            
                Health playerHealth = player.GetComponent<Health>();
                if (playerHealth != null && playerHealth.GetHealth() <= 0) 
                {
                    Debug.Log($"Игрок {player.name} мертв, пропускаем");
                    continue;
                }
                SetAggro(player);
                break;
            }
        }
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnDamaged -= OnDamaged;
    
        if (_returnCoroutine != null)
            StopCoroutine(_returnCoroutine);
        
        UnsubscribeFromTargetDeath();
    
        if (_assignedGroup != null)
        {
            _assignedGroup.UnregisterNeutral(this);
        }
        else if (spawnGroup != null)
        {
            spawnGroup.UnregisterNeutral(this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        switch (_currentState)
        {
            case State.Idle:
                Gizmos.color = Color.green;
                break;
            case State.Chasing:
                Gizmos.color = Color.yellow;
                break;
            case State.Attacking:
                Gizmos.color = Color.red;
                break;
            case State.Returning:
                Gizmos.color = Color.blue;
                break;
        }
        Gizmos.DrawWireSphere(transform.position, 1f);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_homePosition, 0.5f);
        Gizmos.DrawWireSphere(_homePosition, maxDistanceFromHome);
    }
}