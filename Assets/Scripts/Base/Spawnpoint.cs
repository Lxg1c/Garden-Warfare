using Photon.Pun.Demo.PunBasics;
using UnityEngine;

using UnityEngine;

public class Spawnpoint : MonoBehaviour
{
    [Header("Визуальные эффекты")]
    public ParticleSystem activationEffect; // Эффект при активации
    public Light spawnpointLight; // Свет точки (опционально)
    public Color activeColor = Color.green; // Цвет активной точки

    [Header("Звуки")]
    public AudioClip activationSound; // Звук активации

    private bool isActive = false; // Активна ли точка
    private SpriteRenderer spriteRenderer; // Для смены спрайта
    private AudioSource audioSource;

    void Start()
    {
        // Получаем компоненты
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        // Настраиваем начальный цвет света
        if (spawnpointLight != null)
            spawnpointLight.color = Color.gray;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Проверяем, что это игрок и точка еще не активна
        if (other.CompareTag("Player") && !isActive)
        {
            ActivateSpawnpoint();
        }
    }

    void ActivateSpawnpoint()
    {
        isActive = true;

        // Сохраняем позицию возрождения в GameManager
        GameManager.Instance.SetSpawnpoint(transform.position);

        // Визуальные эффекты
        if (activationEffect != null)
            activationEffect.Play();

        if (spriteRenderer != null)
            spriteRenderer.color = activeColor;

        if (spawnpointLight != null)
            spawnpointLight.color = activeColor;

        // Звуковые эффекты
        if (activationSound != null && audioSource != null)
            audioSource.PlayOneShot(activationSound);

        Debug.Log("Точка возрождения активирована!");
    }
}