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
    private bool _isAtHome = true;

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
    }

    private void Update()
    {
        CheckAttackRange();
        
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
        // Проверяем расстояние от дома
        float distanceFromHome = Vector3.Distance(transform.position, _homePosition);
        if (distanceFromHome > maxDistanceFromHome)
        {
            StartReturningHome();
            return;
        }

        // Если есть агро, начинаем преследование
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

        // Проверяем таймаут агро
        if (Time.time > _aggroEndTime)
        {
            StartReturningHome();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
        if (distanceToTarget <= attackRange)
        {
            // В радиусе атаки
            _currentState = State.Attacking;
            _agent.isStopped = true;
        }
        else
        {
            // Продолжаем преследование
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

        // Проверяем таймаут агро
        if (Time.time > _aggroEndTime)
        {
            StartReturningHome();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
        
        if (distanceToTarget > attackRange)
        {
            // Цель убежала, продолжаем преследование
            _currentState = State.Chasing;
            _agent.isStopped = false;
            return;
        }

        // Поворачиваемся к цели
        RotateTowardsTarget();

        // Атакуем
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
            // Достигли дома
            _agent.isStopped = true;
            _currentState = State.Idle;
            _hasAggro = false;
            _currentTarget = null;
            _isAtHome = true;
            
            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
                _returnCoroutine = null;
            }
            return;
        }

        // Продолжаем движение к дому
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
        
        if (_returnCoroutine != null)
            StopCoroutine(_returnCoroutine);
        
        _returnCoroutine = StartCoroutine(ReturnHomeRoutine());
    }

    private IEnumerator ReturnHomeRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        
        // Двойная проверка, что мы все еще возвращаемся
        if (_currentState == State.Returning)
        {
            _agent.SetDestination(_homePosition);
        }
    }

    private void SetAggro(Transform target)
    {
        _currentTarget = target;
        _hasAggro = true;
        _aggroEndTime = Time.time + aggressionDuration;
        _isAtHome = false;

        // Прерываем возврат если он был
        if (_returnCoroutine != null)
        {
            StopCoroutine(_returnCoroutine);
            _returnCoroutine = null;
        }

        if (_currentState == State.Returning)
        {
            _currentState = State.Chasing;
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
                // Получаем ViewID атакующего (этого нейтрала)
                int attackerViewId = photonView.ViewID;
            
                // Вызываем RPC на цели через её PhotonView
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
                // Оффлайн режим
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
        }
    }

    private IEnumerator DamageFlash()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            renderer.material.color = originalColor;
        }
    }

    // Методы для внешнего вызова (например, из триггеров)
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

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnDamaged -= OnDamaged;
        
        if (_returnCoroutine != null)
            StopCoroutine(_returnCoroutine);
    }

    private void OnDrawGizmosSelected()
    {
        // Визуализация состояний
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