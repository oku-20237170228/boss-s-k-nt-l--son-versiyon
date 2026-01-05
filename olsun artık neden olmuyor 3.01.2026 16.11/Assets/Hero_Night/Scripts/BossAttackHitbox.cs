using UnityEngine;

public class BossAttackHitbox : MonoBehaviour
{
    public int damage = 15;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player_Script player = other.GetComponent<Player_Script>();
            if (player != null)
            {
                player.TriggerHurt(damage);
            }
        }
    }
}
