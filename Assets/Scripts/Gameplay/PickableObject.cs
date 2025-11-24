using UnityEngine;

namespace Gameplay
{
    public class PickableObject : MonoBehaviour
    {
        [Header("Pickable Settings")]
        public string objectName = "Object";
        public PickableType objectType;
        public bool canBePickedUp = true;
        
        [Header("Visual Feedback")]
        public GameObject highlightEffect;
        
        // ✅ Сохраняем оригинальный масштаб
        private Vector3 _originalScale;
        private bool _scaleSaved;
        
        private void Start()
        {
            // ✅ Сохраняем оригинальный масштаб при старте
            SaveOriginalScale();
        }
        
        public void SaveOriginalScale()
        {
            _originalScale = transform.localScale;
            _scaleSaved = true;
            Debug.Log($"Сохранен масштаб для {name}: {_originalScale}");
        }
        
        public void RestoreOriginalScale()
        {
            if (_scaleSaved)
            {
                transform.localScale = _originalScale;
                Debug.Log($"Восстановлен масштаб для {name}: {_originalScale}");
            }
        }
        
        // Метод для внешнего взаимодействия
        public virtual void OnPickedUp()
        {
            Debug.Log($"Поднят объект: {objectName}");
        }
        
        public virtual void OnDropped()
        {
            Debug.Log($"Брошен объект: {objectName}");
        }
    }

    public enum PickableType
    {
        Resource,
        Weapon,
        Potion,
        Artifact,
        Plant,
        Other
    }
}