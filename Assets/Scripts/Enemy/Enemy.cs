using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health = 100;
    
    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log($"Enemy took {damage} damage. Health: {health}");
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        Debug.Log("Enemy died!");
        Destroy(gameObject);
    }
}
