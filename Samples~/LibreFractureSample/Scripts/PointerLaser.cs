using System;
using UnityEngine;

public class PointerLaser : MonoBehaviour
{
    [SerializeField] private float range = 100f;
    [SerializeField] private float impulseForce = 10f;

    private void FixedUpdate()
    {
        if (Input.GetMouseButton(0))
            ShootLaser();
    }

    private void ShootLaser()
    {
        var mousePosition = Input.mousePosition;
        var camera = Camera.main;

        if (Physics.Raycast(camera.ScreenPointToRay(mousePosition), out var hit, range))
        {
            var hitRigidbody = hit.rigidbody;
            if (hitRigidbody != null)
            {
                hitRigidbody.AddForce(camera.transform.forward * impulseForce, ForceMode.Impulse);
            }
        }
    }
}
