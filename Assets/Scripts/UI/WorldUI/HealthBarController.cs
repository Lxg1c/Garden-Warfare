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
    [SerializeField] private Image backgroundImage;

    [Header("Settings")]
    [SerializeField] private float damageOverlayDelay = 0.5f;
    [SerializeField] private float damageOverlaySpeed = 2f;
    [SerializeField] private Color friendlyBorderColor = Color.yellow;
    [SerializeField] private Color enemyBorderColor = Color.black;    
    [SerializeField] private string playerTag = "Player"; 

    [Header("Transparency Settings")]
    [SerializeField][Range(0f, 1f)] private float healthBarAlpha = 0.7f;

    [Header("Positioning")]
    [SerializeField] private float verticalOffset = 1.5f; 

    [SerializeField] private float zOffset;          

    private Health targetHealth;
    private Camera mainCamera;
    private Coroutine damageOverlayCoroutine;

    public void Initialize(Health healthComponent)
    {
        targetHealth = healthComponent;
        mainCamera = Camera.main; 
        
        bool isLocalPlayerCharacter = false;
        if (targetHealth.photonView != null)
        {
            if (targetHealth.photonView.IsMine && targetHealth.CompareTag(playerTag))
            {
                isLocalPlayerCharacter = true;
            }
        }

        if (isLocalPlayerCharacter)
        {
            borderImage.color = friendlyBorderColor; 
            Debug.Log($"HealthBar for {targetHealth.name} (ViewID: {targetHealth.photonView.ViewID}, " +
                      $"Owner: {targetHealth.photonView.Owner.NickName}) is LOCAL PLAYER CHARACTER (Tag: " +
                      $"{targetHealth.tag}). Setting border to YELLOW.");
        }
        else
        {
            borderImage.color = enemyBorderColor;
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
        SetAlpha(healthBarAlpha);
        
        UpdateHealthBar(targetHealth.GetHealth(), targetHealth.GetMaxHealth());
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
        
        Vector3 targetPosition = targetHealth.transform.position + Vector3.up * verticalOffset;
        transform.position = targetPosition;
        
        Vector3 cameraEuler = mainCamera.transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(cameraEuler.x, 0f, 0f);

        transform.position += transform.forward * zOffset;
    }

    private void OnTargetDamaged(Transform attacker)
    {
        UpdateHealthBar(targetHealth.GetHealth(), targetHealth.GetMaxHealth());
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