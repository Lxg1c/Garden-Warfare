using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HealthBar : MonoBehaviour
    {
        public Slider slider;

        public void SetHealth(float health, float maxHealth)
        {
            slider.maxValue = maxHealth;
            slider.value = health;

            // Оставлять ли полосу видимой всегда?
            // Если только при повреждении — раскомментируй:
            // slider.gameObject.SetActive(health < maxHealth);
        }
    }
}