using UnityEngine;
using Core;
using Core.Components;

public class PlayerKillSystem : MonoBehaviour
{
    [Header("Настройки убийства")]
    public int damage = 100;
    public float attackRange = 2f;
    public LayerMask enemyLayer;
    public KeyCode attackKey = KeyCode.Mouse0;
    
    [Header("Визуальные эффекты")]
    public ParticleSystem hitEffect;
    public AudioClip killSound;
    
    private Camera playerCamera;
    private AudioSource audioSource;

    void Start()
    {
        playerCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (Input.GetKeyDown(attackKey))
        {
            TryKillPlayer();
        }
    }

    void TryKillPlayer()
    {
        RaycastHit hit;
        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, attackRange, enemyLayer))
        {
            Health targetHealth = hit.collider.GetComponent<Health>();
            
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
                
                // Визуальные и звуковые эффекты
                if (hitEffect != null)
                {
                    Instantiate(hitEffect, hit.point, Quaternion.identity);
                }
                
                if (killSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(killSound);
                }
                
                Debug.Log($"Игрок атаковал: {hit.collider.name}");
            }
        }
    }

    // Визуализация луча атаки в редакторе
    void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.red;
            Vector3 rayOrigin = playerCamera.transform.position;
            Vector3 rayEnd = rayOrigin + playerCamera.transform.forward * attackRange;
            Gizmos.DrawLine(rayOrigin, rayEnd);
        }
    }
}