using System;
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
    public float attackRange = 2.5f;
    public float aggressionTimeOut = 3f;
    public float maxDistanceFromHome = 10f;
    public Transform homePoint;
    public string playerTag = "Player";

    [Header("Combat")]
    public float attackDamage = 10f;
    public float attackCooldown = 1f;

    private NavMeshAgent _agent;
    private Health _health;
    private Transform _target;
    private Vector3 _homePosition;
    private float _lastAttackTime;
    private float _aggressionEndTime;
    private bool _isAggressive;
    private float _originalDetectionRadius;
    private bool _isForcedReturn; // Флаг принудительного возврата

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        _homePosition = homePoint != null ? homePoint.position : transform.position;
        _originalDetectionRadius = detectionRadius;

        _health.OnDamaged += OnDamaged;
        
        _agent.stoppingDistance = attackRange;
        _agent.autoBraking = true; 
        
        Debug.Log($"{name} initialized. Home position: {_homePosition}");
    }

    private void Update()
    {
        // ВСЕГДА проверяем расстояние от дома ПЕРВЫМ делом
        CheckDistanceFromHome();

        // Если принудительно возвращаемся - игнорируем всё остальное
        if (_isForcedReturn)
        {
            HandleForcedReturn();
            return;
        }

        // Проверяем таймаут агрессии
        if (_isAggressive && Time.time >= _aggressionEndTime)
        {
            ResetAggression();
            return;
        }

        if (_isAggressive)
        {
            HandleAggressiveState();
        }
        else
        {
            HandlePeacefulState();
        }
    }

    private void CheckDistanceFromHome()
    {
        float distanceFromHome = Vector3.Distance(transform.position, _homePosition);
        
        if (distanceFromHome > maxDistanceFromHome && !_isForcedReturn)
        {
            Debug.Log($"{name} слишком далеко от дома! Distance: {distanceFromHome}, max: {maxDistanceFromHome}");
            ForceReturnHome();
        }
    }

    private void ForceReturnHome()
    {
        _isForcedReturn = true;
        _isAggressive = false;
        _target = null;
        
        Debug.Log($"{name} принудительно возвращается домой");
        ReturnHome();
    }

    private void HandleForcedReturn()
    {
        float distanceToHome = Vector3.Distance(transform.position, _homePosition);
        
        // Когда вернулись достаточно близко к дому, снимаем флаг
        if (distanceToHome <= maxDistanceFromHome * 0.7f) // 70% от максимального расстояния
        {
            _isForcedReturn = false;
            Debug.Log($"{name} вернулся в зону дома");
            return;
        }
        
        // Продолжаем двигаться домой
        ReturnHome();
    }

    private void HandleAggressiveState()
    {
        // Если цель потеряна
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            FindTarget();
            if (_target == null)
            {
                ResetAggression();
                return;
            }
        }

        float distanceToTarget = Vector3.Distance(transform.position, _target.position);
        
        if (distanceToTarget <= attackRange)
        {
            // Останавливаемся и атакуем
            if (_agent.hasPath)
            {
                _agent.ResetPath();
            }

            RotateTowardsTarget();

            if (Time.time >= _lastAttackTime + attackCooldown)
            {
                AttackPlayer();
            }
        }
        else
        {
            // Преследуем
            _agent.SetDestination(_target.position);
        }
    }

    private void HandlePeacefulState()
    {
        // Периодически ищем цели
        if (Time.frameCount % 60 == 0)
        {
            FindTarget();
        }

        // Возвращаемся домой если не агрессивны
        ReturnHome();
    }

    private void FindTarget()
    {
        // Не ищем цели если принудительно возвращаемся
        if (_isForcedReturn) return;

        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag(playerTag) && collider.transform != this.transform)
            {
                SetTarget(collider.transform);
                return;
            }
        }
    }

    private void SetTarget(Transform newTarget)
    {
        _target = newTarget;
        _isAggressive = true;
        _aggressionEndTime = Time.time + aggressionTimeOut;
        
        Debug.Log($"{name} атакует {newTarget.name}");
    }

    private void ResetAggression()
    {
        _isAggressive = false;
        _target = null;
        detectionRadius = _originalDetectionRadius;
        
        Debug.Log($"{name} сбросил агрессию");
        ReturnHome();
    }

    private void ReturnHome()
    {
        float distanceToHome = Vector3.Distance(transform.position, _homePosition);
        
        if (distanceToHome <= 1f) // Более строгое условие достижения дома
        {
            if (_agent.hasPath)
            {
                _agent.ResetPath();
            }
            return;
        }
        
        // Устанавливаем путь только если нужно
        if (!_agent.hasPath || _agent.destination != _homePosition)
        {
            _agent.SetDestination(_homePosition);
        }
    }

    private void RotateTowardsTarget()
    {
        if (_target != null)
        {
            Vector3 direction = (_target.position - transform.position).normalized;
            direction.y = 0;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                    Time.deltaTime * 5f);
            }
        }
    }

    private void AttackPlayer()
    {
        if (_target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, _target.position);
            if (distanceToTarget <= attackRange)
            {
                var health = _target.GetComponent<Health>();
                if (health != null)
                {
                    if (PhotonNetwork.IsConnected && photonView != null)
                    {
                        health.photonView.RPC("TakeDamageRPC", RpcTarget.All, attackDamage, photonView.ViewID);
                    }
                    else
                    {
                        health.TakeDamage(attackDamage, transform);
                    }
                
                    Debug.Log($"Атаковал {_target.name}");
                    _lastAttackTime = Time.time;
                    _aggressionEndTime = Time.time + aggressionTimeOut;
                }
            }
        }
    }

    private void OnDamaged(Transform attacker)
    {
        Debug.Log($"{name} получил урон от {attacker.name}");

        if (attacker != null && !_isForcedReturn)
        {
            SetTarget(attacker);
            detectionRadius = _originalDetectionRadius * 1.5f;
        }
        
        StartCoroutine(DamageFlash());
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

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnDamaged -= OnDamaged;
    }

    private void OnDrawGizmosSelected()
    {
        // Радиус обнаружения
        Gizmos.color = _isAggressive ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Радиус атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Зона активности (максимальное расстояние от дома)
        Gizmos.color = _isForcedReturn ? Color.magenta : Color.blue;
        Gizmos.DrawWireSphere(_homePosition, maxDistanceFromHome);
        
        // Домашняя позиция
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_homePosition, 0.3f);
    }
}