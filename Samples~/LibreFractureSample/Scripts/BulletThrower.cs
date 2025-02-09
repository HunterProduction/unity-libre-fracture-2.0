using UnityEngine;

public class BulletThrower : MonoBehaviour
{
    public float shotPower = 10;

    private Rigidbody _rigidbody;
    private Vector3 _defaultPosition;
    private Quaternion _defaultRotation;

    private void Awake()
    {
        _defaultPosition = transform.position;
        _defaultRotation = transform.rotation;

        _rigidbody = GetComponent<Rigidbody>();

        _rigidbody.isKinematic = true;
    }

    private void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Shoot(transform.forward);
        }
    }

    public void Shoot(Vector3 direction)
    {
        if (!_rigidbody.isKinematic)
            return;

        _rigidbody.isKinematic = false;
        _rigidbody.AddForce(shotPower * direction, ForceMode.Impulse);
    }

    public void ResetBullet()
    {
        _rigidbody.isKinematic = true;

        transform.SetPositionAndRotation(_defaultPosition, _defaultRotation);
    }
}
