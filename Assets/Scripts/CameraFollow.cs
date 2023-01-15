using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CameraFollow : NetworkBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] Vector3 offset;

    

    

    void CameraFound(Transform transform)
    {
        if (IsOwner)
            player = transform;
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
            Vector3 desiredPosition = player.position + offset;
                    Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
                    transform.position = smoothedPosition;
                    transform.LookAt(player);
        
        
    }
    
    
}
