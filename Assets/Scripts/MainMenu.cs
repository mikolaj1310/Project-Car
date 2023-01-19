using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private CarNetworkManager networkManager;

    private TMP_InputField ipInputField;
    // Start is called before the first frame update
    void Start()
    {
        networkManager = GameObject.Find("NetworkManager").GetComponent<CarNetworkManager>();
        ipInputField = GameObject.Find("IPInput").GetComponent<TMP_InputField>();
        networkManager.networkAddress = ipInputField.text;
        ipInputField.onValueChanged.AddListener(delegate { UpdateInputField(ipInputField.text); });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void UpdateInputField(string text)
    {
        networkManager.networkAddress = ipInputField.text;
    }

    public void HostLocal()
    {
        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            networkManager.StartHost();
        }
    }

    public void JoinLocal()
    {
        networkManager.StartClient();
    }
}
