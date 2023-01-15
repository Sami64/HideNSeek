using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float speed;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane playerPlane = new Plane(Vector3.up, transform.position);
            
            if (playerPlane.Raycast(ray, out float hitDist))
            {
                var targetPoint = ray.GetPoint(hitDist);
                var moveDir = (targetPoint - transform.position).normalized;
                transform.position += moveDir * (speed * Time.deltaTime);
                transform.LookAt(targetPoint);
            }
        }
        
        
        
    }

    public override void OnNetworkSpawn()
    {
        if(!IsOwner) Destroy(this);
    }
}
