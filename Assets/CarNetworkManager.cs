using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

public class CarNetworkManager : NetworkManager
{
	private List<CarController> m_Cars;
	public override void OnServerAddPlayer(NetworkConnectionToClient conn)
	{
		for (int i = 0; i < GameObject.Find("SpawnPoints").transform.childCount - 1; i++)
		{
			startPositions.Add(GameObject.Find("SpawnPoints").transform.GetChild(i));
			//RegisterStartPosition(GameObject.Find("SpawnPoints").transform.GetChild(i));
		}
		Transform m_StartPos = GetStartPosition();
		GameObject m_Player = m_StartPos != null
			? Instantiate(playerPrefab, m_StartPos.position, m_StartPos.rotation)
			: Instantiate(playerPrefab);

		// instantiating a "Player" prefab gives it the name "Player(clone)"
		// => appending the connectionId is WAY more useful for debugging!
		m_Player.name = $"{playerPrefab.name} [connId={numPlayers}]";

		NetworkServer.AddPlayerForConnection(conn, m_Player);
	}
}
