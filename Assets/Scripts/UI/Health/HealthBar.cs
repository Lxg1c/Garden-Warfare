using UnityEngine;
using UnityEngine.UI;

namespace UI.Health
{
    public class HealthBar : MonoBehaviour
    {
        public Slider slider;

        public void SetHealth(float health)
        {
            slider.value = health;
        }

        public void SetMaxHealth(float maxHealth)
        {
            slider.maxValue = maxHealth;
        }
    }
}