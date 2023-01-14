using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#if UNITY_EDITOR
using ParrelSync;
#endif
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : NetworkBehaviour
{
    private const int LobbyRefreshRate = 2;
        
    [SerializeField] private LobbyPanel lobbyPanel;
    [SerializeField] private RoomPanel roomPanel;
    [SerializeField] private TMP_InputField codeInputField;

    private static CancellationTokenSource _updateLobbySource;
    

    [SerializeField] private UnityTransport _unityTransport;

    public static event Action<Lobby> CurrentLobbyRefreshed;

     static Lobby _currentLobby;
    
    private async void Awake()
    {
        await Authenticate();
       lobbyPanel.gameObject.SetActive(true);
       roomPanel.gameObject.SetActive(false);
    }

    private void Start()
    {
        LobbyPanel.LobbyCreated += CreateGame;
        LobbyPanel.LobbyJoined += JoinGame;

        RoomPanel.StartPressed += OnGameStart;
    }

    static async Task Authenticate()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            var options = new InitializationOptions();
#if UNITY_EDITOR
            // Remove this if you don't have ParrelSync installed. 
            // It's used to differentiate the clients, otherwise lobby will count them as the same
            if (ClonesManager.IsClone()) options.SetProfile(ClonesManager.GetArgument());
            else options.SetProfile("Primary");
#endif

            await UnityServices.InitializeAsync(options);

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                PlayerPrefs.SetString("UserId",AuthenticationService.Instance.PlayerId);
                Debug.Log($"Player Id {AuthenticationService.Instance.PlayerId}");
            }
        }

    }


     async void JoinGame()
     {
         try
         {
             _currentLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(codeInputField.text);
             var a = await RelayService.Instance.JoinAllocationAsync(_currentLobby.Data["relayJoinKey"].Value);
             _unityTransport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port,a.AllocationIdBytes,a.Key,a.ConnectionData,a.HostConnectionData);
             
             PeriodicallyRefreshLobby();
             
             lobbyPanel.gameObject.SetActive(false);
             roomPanel.InitRoom(_currentLobby);
             roomPanel.gameObject.SetActive(true);

             

             NetworkManager.Singleton.StartClient();
         }
         catch (Exception e)
         {
             Debug.LogError($"Cant join {e.Message}");
         }
         
     }
     
     private static async void PeriodicallyRefreshLobby() {
         _updateLobbySource = new CancellationTokenSource();
         await Task.Delay(LobbyRefreshRate * 1000);
         while (!_updateLobbySource.IsCancellationRequested && _currentLobby != null) {
             _currentLobby = await Lobbies.Instance.GetLobbyAsync(_currentLobby.Id);
             CurrentLobbyRefreshed?.Invoke(_currentLobby);
             await Task.Delay(LobbyRefreshRate * 1000);
         }
     }

     async void CreateGame(LobbyData lobbyData)
     {
         try
         {
             
             
             var a = await RelayService.Instance.CreateAllocationAsync(lobbyData.MaxPlayers);
             var joinKey = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);
             
             var options = new CreateLobbyOptions
             {
                 Data = new Dictionary<string, DataObject>()
                 {
                     {"roomName", new DataObject(DataObject.VisibilityOptions.Member, lobbyData.Name)},
                     {"relayJoinKey", new DataObject(DataObject.VisibilityOptions.Member, joinKey)}
                 }

             };
             
             _currentLobby = await Lobbies.Instance.CreateLobbyAsync(lobbyData.Name, lobbyData.MaxPlayers, options);

             _unityTransport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);
             
             StartCoroutine(LobbyHeartbeat(_currentLobby.Id, 15));
             PeriodicallyRefreshLobby();
             
             lobbyPanel.gameObject.SetActive(false);
             
             roomPanel.InitRoom(_currentLobby);
             roomPanel.gameObject.SetActive(true);

             NetworkManager.Singleton.StartHost();
         }
         catch (Exception e)
         {
             Debug.Log($"Unable to Create Lobby {e.Message}");
         }
         
     }

     IEnumerator LobbyHeartbeat(string lobbyId, float waitTime)
     {
         var delay = new WaitForSecondsRealtime(waitTime);
         while (true)
         {
             Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
             yield return delay;
         }
     }

     private readonly Dictionary<ulong, bool> _lobbyPlayers = new();
     public static event Action<Dictionary<ulong, bool>> LobbyPlayersUpdated;
     private float _nextLobbyUpdate;

     public override void OnNetworkSpawn()
     {
         if (IsServer)
         {
             NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
             _lobbyPlayers.Add(NetworkManager.Singleton.LocalClientId, false);
             
             UpdateRoomUI();
         }

         NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;
     }

     void OnClientConnectedCallback(ulong playerId)
     {
         if(!IsServer) return;
         
         if(_lobbyPlayers.ContainsKey(playerId)) _lobbyPlayers.Add(playerId, false);
         
         NotifyClients();
         
         UpdateRoomUI();
     }

     void NotifyClients()
     {
         foreach (var player in _lobbyPlayers)
         {
             UpdatePlayerClientRpc(player.Key, player.Value);
         }
     }

     [ClientRpc]
     void UpdatePlayerClientRpc(ulong clientId, bool isReady)
     {
         if (IsServer) return;
         
         if(!_lobbyPlayers.ContainsKey(clientId)) _lobbyPlayers.Add(clientId, isReady);
         else
         {
             _lobbyPlayers[clientId] = isReady;
         }
         UpdateRoomUI();
     }

     void OnClientDisconnectedCallback(ulong playerId)
     {
         if (IsServer)
         {
             if (_lobbyPlayers.ContainsKey(playerId)) _lobbyPlayers.Remove(playerId);
             
             RemovePlayerClientRpc(playerId);
             
             UpdateRoomUI();
         }
     }

     [ClientRpc]
     void RemovePlayerClientRpc(ulong clientId)
     {
         if (IsServer) return;

         if (_lobbyPlayers.ContainsKey(clientId)) _lobbyPlayers.Remove(clientId);
         UpdateRoomUI();
     }

     public void OnReadyClicked()
     {
         SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
     }

     [ServerRpc(RequireOwnership = false)]
     void SetPlayerReadyServerRpc(ulong playerId)
     {
         _lobbyPlayers[playerId] = true;
         NotifyClients();
         UpdateRoomUI();
     }

     void UpdateRoomUI()
     {
         LobbyPlayersUpdated?.Invoke(_lobbyPlayers);
     }

     public override void OnDestroy()
     {
         base.OnDestroy();
         LobbyPanel.LobbyCreated -= CreateGame;
         LobbyPanel.LobbyJoined -= JoinGame;

         if (NetworkManager.Singleton != null)
         {
             NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedCallback;
         }
     }

      void OnGameStart()
     {
         NetworkManager.Singleton.SceneManager.LoadScene("Main", LoadSceneMode.Single);
     }
}

public struct LobbyData
{
    public string Name;
    public int MaxPlayers;
    public string lobbyCode;
}
