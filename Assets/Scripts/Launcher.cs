using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{

    //singleton pattern to make sure there is only one instance of this class
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
    public TMP_Text roomNameText, playerNameLabel;

    public GameObject errorScreen;
    public TMP_Text errorText;

    public GameObject roomBrowserScreen;
    public RoomButton theRoomButton;
    private List<RoomButton> allRoomButtons = new List<RoomButton>();

    public GameObject nameInputScreen;
    public TMP_InputField nameInput;
    public static bool hasSetNickName;

    public string levelToPlay;
    public GameObject startButton;

    public GameObject roomTestButton;

    public string[] allMaps;
    public bool changeMapBetweenRounds = true;



    // Start is called before the first frame update
    void Start()
    {
        //closes any open menus in the game UI. 
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Connecting to Network...";

        if (!PhotonNetwork.IsConnected)
        {
            // If the Photon network is not connected,
            // attempt to connect using the settings.
            PhotonNetwork.ConnectUsingSettings();

        }

        //if the game is in the editor, show the room test button
#if UNITY_EDITOR
        roomTestButton.SetActive(true);
#endif

        //unlock the cursor and make it visible.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseMenus()
    {
        //closes any open menus in the game UI.

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
        // Attempt to join a lobby on the Master Server
        PhotonNetwork.JoinLobby();

        // If any client loads a new Unity scene, that scene will be loaded for all connected clients
        PhotonNetwork.AutomaticallySyncScene = true;

        loadingText.text = "Joining Lobby...";
        //  Debug.Log("Connected to Master");
    }

    //triggered when the client successfully joins a lobby.
    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);
        //Debug.Log("Joined Lobby");

        // Set a temporary nickname for the player
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
            // If the player has set a nickname, set the Photon Network nickname to the player's name from PlayerPrefs
            PhotonNetwork.NickName = PlayerPrefs.GetString("PlayerName");
        }
    }

    //  open a screen where a player can create a new room
    public void OpenRoomCreate()
    {
        CloseMenus();
        createRoomScreen.SetActive(true);
    }

    public void CreateRoom()
    {
        //checks if the text of roomNameInput is not null or empty
        if (!string.IsNullOrEmpty(roomNameInput.text))
        {
            //RoomOptions is a class in PUN that allows you to set various options for the room
            RoomOptions options = new RoomOptions();
            options.MaxPlayers = 8;

            //creates a new room with the name entered by the player and the options specified
            PhotonNetwork.CreateRoom(roomNameInput.text, options);
            CloseMenus();
            loadingText.text = "Creating Room...";
            loadingScreen.SetActive(true);
        }



    }
    //client successfully joins a room - Photon
    public override void OnJoinedRoom()
    {
        CloseMenus();
        roomScreen.SetActive(true);

        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        ListAllPlayers();

        //checks if the current client is the master client of the room
        if (PhotonNetwork.IsMasterClient)
        {
            //if atleast 2 players are in the room, the start button is activated
            Debug.Log(PhotonNetwork.CurrentRoom.PlayerCount);

            UpdateStartButton();






        }
        else
        {
            startButton.SetActive(false);
        }

    }

    //list all the players
    private void ListAllPlayers()
    {
        //clear the list of players currently displayed in the game UI.
        foreach (TMP_Text player in allPlayers)
        {
            //removes the player's name from the game UI.
            Destroy(player.gameObject);
        }
        //clears the allPlayers list. This is done to prepare for a fresh list of players.
        allPlayers.Clear();


        //players currently in the room from the PhotonNetwork and stores it in an array
        Player[] players = PhotonNetwork.PlayerList;
        for (int x = 0; x < players.Length; x++)
        {
            //creates a new instance of a TextMeshPro text object for each player
            TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent).GetComponent<TMP_Text>();
            //sets the text of the new label to the nickname of the current player
            newPlayerLabel.text = players[x].NickName;
            // player's name visible in the UI.
            newPlayerLabel.gameObject.SetActive(true);
            //adds the new label to the allPlayers list.
            allPlayers.Add(newPlayerLabel);
        }
    }
    //PUN framework, which is triggered when a new player enters the room
    override public void OnPlayerEnteredRoom(Player newPlayer)
    {
       // ListAllPlayers();
        TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent).GetComponent<TMP_Text>();
        newPlayerLabel.text = newPlayer.NickName;
        newPlayerLabel.gameObject.SetActive(true);
        allPlayers.Add(newPlayerLabel);
        Debug.Log("A new player joined the room. Total players: " + PhotonNetwork.CurrentRoom.PlayerCount);

    UpdateStartButton();
    }

    // (PUN) framework, which is triggered when a player leaves the room.
    override public void OnPlayerLeftRoom(Player otherPlayer)
    {
        //display a list of all players currently in the PhotonNetwork room
        ListAllPlayers();
         Debug.Log("Player left room. Current player count: " + PhotonNetwork.CurrentRoom.PlayerCount);

    UpdateStartButton();
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
        // PhotonNetwork.LoadLevel(levelToPlay);

        PhotonNetwork.LoadLevel(allMaps[Random.Range(0, allMaps.Length)]);
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

    void UpdateStartButton()
{
    //checks if the current client is the master client of the room
    if (PhotonNetwork.IsMasterClient)
    {
        //if atleast 2 players are in the room, the start button is activated
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            startButton.SetActive(true);
        }
        else
        {
            startButton.SetActive(false);
        }
    }
    else
    {
        startButton.SetActive(false);
    }
}

}
