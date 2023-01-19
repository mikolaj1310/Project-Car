using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using UnityEngine.UI;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

public class CarNetworkManager : NetworkRoomManager
{
	private List<CarController> m_Cars;
	private GameObject m_StartGameButton;

	public override void Awake()
	{
		base.Awake();
		DontDestroyOnLoad(gameObject);
	}

	public override void Start()
	{
		m_StartGameButton = GameObject.Find("StartGameButton");
		m_StartGameButton.SetActive(false);
		m_StartGameButton.GetComponent<Button>().onClick.AddListener(delegate { GameStartButtonPressed(); });
	}
	public override void OnStartServer()
	{
		base.OnStartServer();
		//NetworkServer.RegisterHandler<CarController>(OnCreateCharacter);
	}

	public override void OnServerAddPlayer(NetworkConnectionToClient conn)
	{
		Transform m_StartPos = GetStartPosition();
		GameObject m_Player = m_StartPos != null
			? Instantiate(playerPrefab, m_StartPos.position, m_StartPos.rotation)
			: Instantiate(playerPrefab);


		// instantiating a "Player" prefab gives it the name "Player(clone)"
		// => appending the connectionId is WAY more useful for debugging!
		m_Player.name = $"{playerPrefab.name} [connId={numPlayers}]";
		NetworkServer.AddPlayerForConnection(conn, m_Player);
	}

	public override void OnRoomServerPlayersReady()
	{
		if(SceneManager.GetActiveScene().name != "MenuScene") return;
		m_StartGameButton.SetActive(true);
	}

	public override void OnRoomServerPlayersNotReady()
	{
		if(SceneManager.GetActiveScene().name != "MenuScene") return;
		m_StartGameButton.SetActive(false);
	}

	public override void OnRoomClientEnter()
	{
		//Start game button disappears even if both ready when host is ready before client joins
		base.OnRoomClientEnter();
		OnRoomServerPlayersNotReady();
	}

	public void GameStartButtonPressed()
	{
		if(SceneManager.GetActiveScene().name != "MenuScene") return;
		Debug.Log("Game started");
		ReplacePlayer(Resources.Load("Prefabs/Cars/P_CarBase") as GameObject);
	}

	public override void OnClientSceneChanged()
	{
		base.OnClientSceneChanged();
		var players = FindObjectsOfType<CarController>();
		for (int i = 0; i < players.Length; i++)
		{
			players[i].transform.position = GameObject.Find("SpawnPoints").transform.GetChild(i).transform.position;
		}

	}

	public void ReplacePlayer(GameObject newPrefab)
	{
		
		foreach (var slot in roomSlots)
		{
			GameObject oldPlayer = slot.connectionToClient.identity.gameObject;
			NetworkServer.ReplacePlayerForConnection(slot.connectionToClient, Instantiate(newPrefab), true);
			
			if(!clientLoadedScene)
				ServerChangeScene(GameplayScene);
			Destroy(oldPlayer, 0.1f);
		}
	}
	public override void OnRoomClientConnect()
	{
		base.OnRoomClientConnect();
	}

	public override void OnRoomClientSceneChanged()
	{
		base.OnRoomClientSceneChanged();
	}

	public override void OnServerSceneChanged(string sceneName)
	{
		base.OnServerSceneChanged(sceneName);
	}
}
