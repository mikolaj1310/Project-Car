using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyMember : NetworkRoomPlayer
{
    private CarNetworkManager m_NetworkManager;
    private NetworkRoomPlayer[] m_NetworkRoomPlayers;
    private GameObject m_ReadyButton;
    private RawImage m_ReadyImage;
    private GameObject m_LobbyPlayerObject;

    [SerializeField] private Texture2D m_ReadyButtonTick;
    [SerializeField] private Texture2D m_ReadyButtonCross;
    
    
    // Start is called before the first frame update
    
    public override void Start()
    {
        base.Start();
        m_ReadyButtonTick = Resources.Load<Texture2D>("Sprites/Tick");
        m_ReadyButtonCross = Resources.Load<Texture2D>("Sprites/Cross");
        m_NetworkManager = GameObject.Find("NetworkManager").GetComponent<CarNetworkManager>();
        m_NetworkRoomPlayers = FindObjectsOfType<NetworkRoomPlayer>();
        GameObject.Find("ReadyButton").GetComponent<Button>().onClick.AddListener(delegate { LobbyPlayerReady(true); });
        m_ReadyImage = transform.Find("LobbyMember").transform.Find("ReadyImage").GetComponent<RawImage>();
        m_LobbyPlayerObject = transform.Find("LobbyMember").gameObject;
        //index = NetworkServer.connections.Count;
        Debug.Log("Index: " + index);
        
    }

    // Update is called once per frame
    void Update()
    {
        //if(NetworkManager.loadingSceneAsync == null) { return;}
        m_NetworkRoomPlayers = FindObjectsOfType<NetworkRoomPlayer>();
        for (int i = 0; i < m_NetworkRoomPlayers.Length; i++)
        {
            if (m_NetworkRoomPlayers[i] == this)
            {
                if(GameObject.Find("LobbyPivots"))
                    m_LobbyPlayerObject.transform.position = GameObject.Find("LobbyPivots").transform.GetChild(i).position;
            }
        }

        //Debug.Log(index);
        foreach (var player in m_NetworkRoomPlayers)
        {
            if (player.readyToBegin)
                player.gameObject.transform.Find("LobbyMember").Find("ReadyImage").GetComponent<RawImage>().texture = m_ReadyButtonTick;
            else
                player.gameObject.transform.Find("LobbyMember").Find("ReadyImage").GetComponent<RawImage>().texture = m_ReadyButtonCross;

        }
    }

    public void LobbyPlayerReady(bool isReady)
    {
        if (!isOwned) { return; }
        CmdChangeReadyState(isReady);
        if (isReady)
        {
            GameObject.Find("MembersPanel").transform.Find("ReadyButton").GetComponent<Button>().onClick
                .AddListener(delegate { LobbyPlayerReady(false); });
        }
        else
        {
            GameObject.Find("MembersPanel").transform.Find("ReadyButton").GetComponent<Button>().onClick
                .AddListener(delegate { LobbyPlayerReady(true); });
        }
    }
}
