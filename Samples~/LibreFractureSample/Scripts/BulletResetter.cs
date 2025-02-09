using UnityEngine;

public class BulletResetter : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        var collingObject = collision.gameObject;

        if(collingObject.TryGetComponent<BulletThrower>(out var bulletThrower))
        {
            bulletThrower.ResetBullet();
        }
    }
}
