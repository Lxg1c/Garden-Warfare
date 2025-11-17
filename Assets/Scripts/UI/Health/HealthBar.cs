using UnityEngine;
using UnityEngine.UI;

namespace UI.HealthBar
{
    public class HealthBar : MonoBehaviour
    {
        public Slider slider;

        public void SetHealth(float health, float maxHealth)
        {
            slider.maxValue = maxHealth;
            slider.value = health;
        }
    }
}