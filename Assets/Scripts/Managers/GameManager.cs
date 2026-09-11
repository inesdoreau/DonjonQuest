using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private MultiplayerUI m_multiplayerUI;
    [SerializeField] private GameObject m_playerPrefab;

    [SerializeField] private List<ResourceDropZone> m_dropZones;

    private void Start()
    {
        if(m_multiplayerUI != null)
        {
            m_multiplayerUI.OnStartHost += StartHost;
            m_multiplayerUI.OnStartClient += StartClient;
            m_multiplayerUI.OnDiconnectClient += DisconnectClient;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(IsServer == false)
            return;
        
        NetworkManager.OnClientConnectedCallback += SpawnPlayer;
        NetworkManager.SceneManager.OnLoadEventCompleted+= HandleSceneLoadCompleted;
        foreach(ResourceDropZone dropZone in m_dropZones)
        {
            dropZone.OnDropZoneFilled += CheckWinCondition;
        }

    }
    public override void OnNetworkDespawn()
    {

        if(IsServer)
        NetworkManager.OnClientConnectedCallback -= SpawnPlayer;

        NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneLoadCompleted;
        foreach(ResourceDropZone dropZone in m_dropZones)
        {
            dropZone.OnDropZoneFilled += CheckWinCondition;
        }
        base.OnNetworkDespawn();
    }

    private void CheckWinCondition()
    {
        Debug.Log("Win");
        int points = 0;
        foreach(ResourceDropZone dropZone in m_dropZones)
        {
            points += dropZone.StackedRessources;
        }
        if(points >= m_dropZones.Count * 3)
        {
            NetworkManager.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }
    }

    private void SpawnPlayer(ulong clientID)
    {
        if(NetworkManager.ConnectedClients[clientID].PlayerObject != null)
        {
            return;
        }
        GameObject player = Instantiate(m_playerPrefab);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, true);
    }


    private void HandleSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        foreach(ulong clientId in clientsCompleted)
        {
            SpawnPlayer(clientId);
        }
    }


    private void DisconnectClient()
    {
        m_multiplayerUI.EnableButtons();
        NetworkManager.Shutdown();
    }

    private void StartClient()
    {
        m_multiplayerUI.DisableButtons();
        NetworkManager.StartClient();
    }

    private void StartHost()
    {
       m_multiplayerUI.DisableButtons();
        NetworkManager.StartHost();
    }
}
