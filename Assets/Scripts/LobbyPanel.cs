using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyPanel : MonoBehaviour
{

    public static event Action<LobbyData> LobbyCreated;
    public static event Action LobbyJoined; 

    public void LobbyCreateClick()
    {
        var lobbyData = new LobbyData
        {
            Name = $"User#{PlayerPrefs.GetString("UserId").Substring(0,5)}",
            MaxPlayers = 2
        };
        
        LobbyCreated?.Invoke(lobbyData);
    }

    public void LobbyJoinClick()
    {
        LobbyJoined?.Invoke();
    }
}
