using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher instance;
    private void Awake()
    {
        instance = this;
    }

    public GameObject loadingScreen;
    public TMP_Text loadingText;

    public GameObject menuButtons;

    public GameObject createRoomScreen;
    public TMP_InputField roomNameInput;


    private List<TMP_Text> allPlayers = new List<TMP_Text>();
    public GameObject roomScreen;
    public TMP_Text roomNameText , playerNameLabel;

    public GameObject errorScreen;
    public TMP_Text errorText;

    public GameObject roomBrowserScreen;
    public RoomButton theRoomButton;
    private List<RoomButton> allRoomButtons = new List<RoomButton>();

    public GameObject nameInputScreen;
    public TMP_InputField nameInput;
    private bool hasSetNickName ;

    public string levelToPlay;
    public GameObject startButton;

    public GameObject roomTestButton;

    // Start is called before the first frame update
    void Start()
    {
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Connecting to Network...";
        PhotonNetwork.ConnectUsingSettings();

        #if UNITY_EDITOR
        roomTestButton.SetActive(true);
        #endif
    }

    void CloseMenus()
    {
        loadingScreen.SetActive(false);
        menuButtons.SetActive(false);
        createRoomScreen.SetActive(false);
        roomScreen.SetActive(false);
        errorScreen.SetActive(false);
        roomBrowserScreen.SetActive(false);
        nameInputScreen.SetActive(false);
        
        

    }

    public override void OnConnectedToMaster()
    {

        PhotonNetwork.JoinLobby();

        PhotonNetwork.AutomaticallySyncScene = true;

        loadingText.text = "Joining Lobby...";
        //  Debug.Log("Connected to Master");
    }

    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);
        //Debug.Log("Joined Lobby");

        PhotonNetwork.NickName = "Player " + Random.Range(0, 1000).ToString();

        if (!hasSetNickName)
        {
            CloseMenus();
            nameInputScreen.SetActive(true);

            if (PlayerPrefs.HasKey("PlayerName"))
            {
                nameInput.text = PlayerPrefs.GetString("PlayerName");
            }
        }
        else
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString("PlayerName");
        }
    }

    public void OpenRoomCreate()
    {
        CloseMenus();
        createRoomScreen.SetActive(true);
    }

    public void CreateRoom()
    {
        if (!string.IsNullOrEmpty(roomNameInput.text))
        {
            RoomOptions options = new RoomOptions();
            options.MaxPlayers = 8;
            PhotonNetwork.CreateRoom(roomNameInput.text, options);
            CloseMenus();
            loadingText.text = "Creating Room...";
            loadingScreen.SetActive(true);
        }



    }

    public override void OnJoinedRoom()
    {
        CloseMenus();
        roomScreen.SetActive(true);

        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        ListAllPlayers();

        if (PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }
        else
        {
            startButton.SetActive(false);
        }
        
    }

    private void ListAllPlayers()
    {
        foreach (TMP_Text player in allPlayers)
        {
            Destroy(player.gameObject);
        }
        allPlayers.Clear();

        Player[] players = PhotonNetwork.PlayerList;
        for (int x = 0; x < players.Length; x++)
        {
            TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent).GetComponent<TMP_Text>();
            newPlayerLabel.text = players[x].NickName;
            newPlayerLabel.gameObject.SetActive(true);
            allPlayers.Add(newPlayerLabel);
        }
    }

    override public void OnPlayerEnteredRoom(Player newPlayer)
    {
        TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent).GetComponent<TMP_Text>();
        newPlayerLabel.text = newPlayer.NickName;
        newPlayerLabel.gameObject.SetActive(true);

        allPlayers.Add(newPlayerLabel);
    }

    override public void OnPlayerLeftRoom(Player otherPlayer)
    {
        ListAllPlayers();
    }

       

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Failed to create room: " + message;
        CloseMenus();
        errorScreen.SetActive(true);
    }

    public void CloseErrorScreen()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        CloseMenus();
        loadingText.text = "Leaving Room...";
        loadingScreen.SetActive(true);
    }

    public override void OnLeftRoom()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void OpenRoomBrowser()
    {
        CloseMenus();
        roomBrowserScreen.SetActive(true);
    }

    public void CloseRoomBrowser()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomButton rb in allRoomButtons)
        {
            Destroy(rb.gameObject);
        }
        allRoomButtons.Clear();

        // Deactivate the room button in the UI by setting its active state to false
        theRoomButton.gameObject.SetActive(false);


        for (int x = 0; x < roomList.Count; x++)
        {
            // Check if the current room in the list is not full and has not been removed from the list
            if (roomList[x].PlayerCount != roomList[x].MaxPlayers && !roomList[x].RemovedFromList)
            {
                // Instantiate a new room button in the room browser screen
                RoomButton newButton = Instantiate(theRoomButton, theRoomButton.transform.parent);

                // Set the details of the new room button to match the current room
                newButton.SetButtonDetails(roomList[x]);

                // Activate the new room button in the UI by setting its active state to true
                newButton.gameObject.SetActive(true);

                // Add the new room button to the list of all room buttons
                allRoomButtons.Add(newButton);
            }
        }

    }

    public void JoinRoom(RoomInfo inputInfo)
    {
        PhotonNetwork.JoinRoom(inputInfo.Name);
        CloseMenus();
        loadingText.text = "Joining Room...";
        loadingScreen.SetActive(true);
    }

    public void SetNickName()
    {
        if (!string.IsNullOrEmpty(nameInput.text))
        {
            PhotonNetwork.NickName = nameInput.text;
            PlayerPrefs.SetString("PlayerName", nameInput.text);

            CloseMenus();
            menuButtons.SetActive(true);
            hasSetNickName = true;
        }
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(levelToPlay);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }
        else
        {
            startButton.SetActive(false);
        }
    }

    public void QuickJoin()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 8;

        PhotonNetwork.CreateRoom("Test Room", options);
        CloseMenus();
        loadingText.text = "Creating Room...";
        loadingScreen.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

}
