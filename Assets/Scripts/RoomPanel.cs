using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class RoomPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomCodeText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] TextMeshProUGUI roomNameText;
    [SerializeField] private GameObject _startBtn, _readyBtn;
    
    private readonly List<LobbyPlayerPanel> _playerPanels = new();
    [SerializeField] private Transform playerPanelParent;
    [SerializeField] private LobbyPlayerPanel playerPanelPrefab;

    private bool _isReady, _allReady;

    public static event Action StartPressed; 

    public Lobby Lobby { get; private set; }

    public void InitRoom(Lobby lobby)
    {
        UpdateRoomUI(lobby);
    }

     void UpdateRoomUI(Lobby lobby)
    {
        Lobby = lobby;
        roomCodeText.text = lobby.LobbyCode;
        playerCountText.text = $"Players: {lobby.Players.Count}/{lobby.MaxPlayers}";
        roomNameText.text = lobby.Data["roomName"].Value;
    }

     private void OnEnable()
     {
         foreach (Transform child in playerPanelParent) Destroy(child.gameObject);
         _playerPanels.Clear();
         
         LobbyManager.LobbyPlayersUpdated += NetworkPlayersUpdated;
         LobbyManager.CurrentLobbyRefreshed += OnLobbyRefreshed;
         
         _readyBtn.SetActive(false);
         _startBtn.SetActive(false);
         _isReady = false;
     }

     private void OnDisable()
     {
         LobbyManager.LobbyPlayersUpdated -= NetworkPlayersUpdated;
         LobbyManager.CurrentLobbyRefreshed -= OnLobbyRefreshed;
     }

     void NetworkPlayersUpdated(Dictionary<ulong, bool> players)
     {
         var playerKeys = players.Keys;
         
             // Destroy Panels?
             var inactivePanels = _playerPanels.Where(p => !playerKeys.Contains(p.PlayerId)).ToList();
             foreach (var panel in _playerPanels)
             {
                 _playerPanels.Remove(panel);
                 Destroy(panel.gameObject);
             }

             foreach (var player in players)
             {
                 var currentPanel = _playerPanels.FirstOrDefault(p => p.PlayerId == player.Key);
                 if (currentPanel != null) {
                     if (player.Value) currentPanel.SetReady();
                 }
                 else {
                     var panel = Instantiate(playerPanelPrefab, playerPanelParent);
                     panel.Init(player.Key);
                     _playerPanels.Add(panel);
                 }
             }

             _startBtn.SetActive(NetworkManager.Singleton.IsHost && players.All(p => p.Value));
             _readyBtn.SetActive(!_isReady);
     }

     void OnLobbyRefreshed(Lobby lobby)
     {
         playerCountText.text = $"Players: {lobby.Players.Count}/{lobby.MaxPlayers}";
     }

     public void OnReadyClicked()
     {
         _readyBtn.SetActive(false);
         _isReady = true;
     }

     public void OnStartClicked()
     {
         StartPressed?.Invoke();
         
     }
}
