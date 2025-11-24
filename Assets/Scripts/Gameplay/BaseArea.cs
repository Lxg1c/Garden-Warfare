using UnityEngine;
using Photon.Pun;

namespace Gameplay
{
    public class BaseArea : MonoBehaviourPun
    {
        [Header("Base Settings")]
        public int owner;
        
        [Header("Base Shape")]
        public BaseShape baseShape = BaseShape.Sphere;
        public float baseRadius = 10f;
        public Vector3 baseSize; 
        
        [Header("Visual Settings")]
        public bool showGizmos = true;
        public GameObject baseVisual;
        
        public enum BaseShape
        {
            Sphere,
            Cube
        }
        
        private void Start()
        {
            UpdateBaseVisual();
        }
        
        /// <summary>
        /// Проверяет находится ли позиция внутри базы
        /// </summary>
        public bool IsPositionInsideBase(Vector3 position)
        {
            switch (baseShape)
            {
                case BaseShape.Sphere:
                    float distance = Vector3.Distance(transform.position, position);
                    return distance <= baseRadius;
                    
                case BaseShape.Cube:
                    // ✅ Проверка для кубической зоны
                    Vector3 localPos = transform.InverseTransformPoint(position);
                    return Mathf.Abs(localPos.x) <= baseSize.x / 2f &&
                           Mathf.Abs(localPos.y) <= baseSize.y / 2f &&
                           Mathf.Abs(localPos.z) <= baseSize.z / 2f;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Проверяет находится ли игрок внутри базы
        /// </summary>
        public bool IsPlayerInsideBase(PhotonView playerView)
        {
            if (playerView == null) return false;
            return IsPositionInsideBase(playerView.transform.position);
        }
        
        /// <summary>
        /// Обновляет визуальное представление базы
        /// </summary>
        private void UpdateBaseVisual()
        {
            if (baseVisual != null)
            {
                switch (baseShape)
                {
                    case BaseShape.Sphere:
                        baseVisual.transform.localScale = Vector3.one * baseRadius * 2f;
                        break;
                    case BaseShape.Cube:
                        baseVisual.transform.localScale = baseSize;
                        break;
                }
            }
        }
        
        // ✅ Визуализация в редакторе
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            switch (baseShape)
            {
                case BaseShape.Sphere:
                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.DrawWireSphere(transform.position, baseRadius);
                    break;
                    
                case BaseShape.Cube:
                    Gizmos.DrawWireCube(Vector3.zero, baseSize);
                    break;
            }
            
            // Крест в центре
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.red;
            float crossSize = 1f;
            Gizmos.DrawLine(
                transform.position - Vector3.right * crossSize,
                transform.position + Vector3.right * crossSize
            );
            Gizmos.DrawLine(
                transform.position - Vector3.forward * crossSize,
                transform.position + Vector3.forward * crossSize
            );
        }
        
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.matrix = transform.localToWorldMatrix;
            
            switch (baseShape)
            {
                case BaseShape.Sphere:
                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.DrawSphere(transform.position, baseRadius);
                    break;
                    
                case BaseShape.Cube:
                    Gizmos.DrawCube(Vector3.zero, baseSize);
                    break;
            }
        }
        
        /// <summary>
        /// Получить случайную позицию внутри базы
        /// </summary>
        public Vector3 GetRandomPositionInsideBase()
        {
            switch (baseShape)
            {
                case BaseShape.Sphere:
                    Vector2 randomCircle = Random.insideUnitCircle * baseRadius;
                    return transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
                    
                case BaseShape.Cube:
                    Vector3 randomPoint = new Vector3(
                        Random.Range(-baseSize.x / 2f, baseSize.x / 2f),
                        0,
                        Random.Range(-baseSize.z / 2f, baseSize.z / 2f)
                    );
                    return transform.TransformPoint(randomPoint);
                    
                default:
                    return transform.position;
            }
        }
        
        /// <summary>
        /// Получить границы базы в мировых координатах
        /// </summary>
        public Bounds GetBaseBounds()
        {
            switch (baseShape)
            {
                case BaseShape.Sphere:
                    return new Bounds(transform.position, Vector3.one * baseRadius * 2f);
                    
                case BaseShape.Cube:
                    return new Bounds(transform.position, baseSize);
                    
                default:
                    return new Bounds(transform.position, Vector3.one);
            }
        }
        
        // ✅ Public методы для изменения параметров
        public void SetBaseShape(BaseShape shape)
        {
            baseShape = shape;
            UpdateBaseVisual();
        }
        
        public void SetBaseSize(Vector3 size)
        {
            baseSize = size;
            UpdateBaseVisual();
        }
        
        public void SetBaseRadius(float radius)
        {
            baseRadius = radius;
            UpdateBaseVisual();
        }
    }
}