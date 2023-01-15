using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private PlayerController _playerPrefab;

    

    
    public override void OnNetworkSpawn()
    {
        SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    void SpawnPlayerServerRpc(ulong playerId)
    {
        var player = Instantiate(_playerPrefab);
        player.NetworkObject.SpawnWithOwnership(playerId);
    }

    

    public override void OnDestroy()
    {
        base.OnDestroy();
        if(NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
    }
}
