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

    public GameObject roomScreen;
    public TMP_Text roomNameText;

    public GameObject errorScreen;
    public TMP_Text errorText;

    public GameObject roomBrowserScreen;
    public RoomButton theRoomButton;
    private List<RoomButton> allRoomButtons = new List<RoomButton>();

    // Start is called before the first frame update
    void Start()
    {
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Connecting to Network...";
        PhotonNetwork.ConnectUsingSettings();
    }

    void CloseMenus()
    {
        loadingScreen.SetActive(false);
        menuButtons.SetActive(false);
        createRoomScreen.SetActive(false);
        roomScreen.SetActive(false);
        errorScreen.SetActive(false);
        roomBrowserScreen.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
       
      PhotonNetwork.JoinLobby();
      loadingText.text = "Joining Lobby...";
      //  Debug.Log("Connected to Master");
    }

    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);
        //Debug.Log("Joined Lobby");
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
            PhotonNetwork.CreateRoom(roomNameInput.text,options);
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
       // PhotonNetwork.LoadLevel("Game");
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
        foreach(RoomButton rb in allRoomButtons)
        {
            Destroy(rb.gameObject);
        }
        allRoomButtons.Clear();

        // Deactivate the room button in the UI by setting its active state to false
        theRoomButton.gameObject.SetActive(false);

        
        for(int x = 0; x < roomList.Count; x++)
        {
            // Check if the current room in the list is not full and has not been removed from the list
            if(roomList[x].PlayerCount != roomList[x].MaxPlayers && !roomList[x].RemovedFromList)
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

}
