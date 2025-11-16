using Core.Components;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
public class HealthBarController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image damageOverlayImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private Image backgroundImage; // Для прямого доступа к фоновому изображению

    [Header("Settings")]
    [SerializeField] private float damageOverlayDelay = 0.5f; // Задержка перед началом уменьшения damageOverlay
    [SerializeField] private float damageOverlaySpeed = 2f;    // Скорость уменьшения damageOverlay
    [SerializeField] private Color friendlyBorderColor = Color.yellow; // Цвет рамки для основного игрока (IsMine)
    [SerializeField] private Color enemyBorderColor = Color.black;    // Цвет рамки для всех остальных (!IsMine)
    [SerializeField] private string playerTag = "Player"; // НОВОЕ: Тег для идентификации игровых объектов

    [Header("Transparency Settings")]
    [SerializeField][Range(0f, 1f)] private float healthBarAlpha = 0.7f; // Прозрачность всей полоски здоровья

    [Header("Positioning")]
    [SerializeField] private float verticalOffset = 1.5f; // Смещение полоски здоровья над юнитом
    [SerializeField] private float zOffset = 0f;          // Смещение полоски здоровья по её локальной Z-оси

    private Health targetHealth; // Ссылка на Health компонент объекта, к которому привязан Health Bar
    private Camera mainCamera;
    private Coroutine damageOverlayCoroutine;

    public void Initialize(Health healthComponent)
    {
        targetHealth = healthComponent;
        mainCamera = Camera.main; // Предполагаем, что основная камера имеет тег MainCamera

        // --- УСИЛЕННАЯ ПРОВЕРКА: Цвет рамки только для МОЕГО ИГРОКА С ТЕГОМ ---
        bool isLocalPlayerCharacter = false;
        if (targetHealth.photonView != null)
        {
            // Проверка: принадлежит ли объект локальному игроку И имеет ли он тег "Player"
            if (targetHealth.photonView.IsMine && targetHealth.CompareTag(playerTag))
            {
                isLocalPlayerCharacter = true;
            }
        }

        if (isLocalPlayerCharacter)
        {
            borderImage.color = friendlyBorderColor; // Желтый для основного игрока
            Debug.Log($"HealthBar for {targetHealth.name} (ViewID: {targetHealth.photonView.ViewID}, Owner: {targetHealth.photonView.Owner.NickName}) is LOCAL PLAYER CHARACTER (Tag: {targetHealth.tag}). Setting border to YELLOW.");
        }
        else
        {
            borderImage.color = enemyBorderColor; // Черный для всех остальных
            string debugMsg = $"HealthBar for {targetHealth.name}";
            if (targetHealth.photonView != null)
            {
                debugMsg += $" (ViewID: {targetHealth.photonView.ViewID}, Owner: {targetHealth.photonView.Owner.NickName})";
            }
            else
            {
                debugMsg += " (No PhotonView)";
            }
            debugMsg += $" is NOT LOCAL PLAYER CHARACTER (Tag: {targetHealth.tag}). Setting border to BLACK.";
            Debug.Log(debugMsg);
        }
        // --- Конец усиленной проверки ---

        // --- Применение прозрачности ко всем Image элементам ---
        SetAlpha(healthBarAlpha);
        // --- Конец применения прозрачности ---

        // Инициализируем полоску здоровья с текущими значениями
        UpdateHealthBar(targetHealth.GetHealth(), targetHealth.MaxHealth);
    }

    private void OnDestroy()
    {
        if (targetHealth != null)
        {
            targetHealth.OnDamaged -= OnTargetDamaged;
            targetHealth.OnDeath -= OnTargetDeath;
        }
    }

    private void Update()
    {
        if (targetHealth == null || mainCamera == null) return;

        // Позиция над объектом (базовая)
        Vector3 targetPosition = targetHealth.transform.position + Vector3.up * verticalOffset;
        transform.position = targetPosition;

        // --- Вращение только по X ---
        Vector3 cameraEuler = mainCamera.transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(cameraEuler.x, 0f, 0f);
        // --- Конец вращения только по X ---

        // --- Применение смещения по Z ---
        transform.position += transform.forward * zOffset;
        // --- Конец смещения по Z ---
    }

    private void OnTargetDamaged(Transform attacker)
    {
        UpdateHealthBar(targetHealth.GetHealth(), targetHealth.MaxHealth);
    }

    private void OnTargetDeath(Transform deadTransform)
    {
        if (targetHealth != null && targetHealth.gameObject == deadTransform.gameObject)
        {
            Destroy(gameObject);
        }
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        float healthRatio = currentHealth / maxHealth;

        healthFillImage.fillAmount = healthRatio;

        if (damageOverlayCoroutine != null)
        {
            StopCoroutine(damageOverlayCoroutine);
        }
        damageOverlayCoroutine = StartCoroutine(AnimateDamageOverlay(healthRatio));
    }

    private IEnumerator AnimateDamageOverlay(float targetFillAmount)
    {
        damageOverlayImage.fillAmount = Mathf.Max(damageOverlayImage.fillAmount, healthFillImage.fillAmount);

        yield return new WaitForSeconds(damageOverlayDelay);

        while (damageOverlayImage.fillAmount > targetFillAmount)
        {
            damageOverlayImage.fillAmount -= Time.deltaTime * damageOverlaySpeed;
            if (damageOverlayImage.fillAmount < targetFillAmount)
            {
                damageOverlayImage.fillAmount = targetFillAmount;
            }
            yield return null;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (healthFillImage != null)
        {
            Color color = healthFillImage.color;
            color.a = alpha;
            healthFillImage.color = color;
        }

        if (damageOverlayImage != null)
        {
            Color color = damageOverlayImage.color;
            color.a = alpha;
            damageOverlayImage.color = color;
        }

        if (borderImage != null)
        {
            Color color = borderImage.color;
            color.a = alpha;
            borderImage.color = color;
        }

        if (backgroundImage != null)
        {
            Color color = backgroundImage.color;
            color.a = alpha;
            backgroundImage.color = color;
        }
    }
}