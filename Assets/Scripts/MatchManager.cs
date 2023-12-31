using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static MatchManager instance;
    private void Awake()
    {
        instance = this;
    }

    public enum EventCodes : byte
    {
        NewPlayer,
        ListPlayers,
        UpdateStat,
        NextMatch,
        TimerSync
    }

    public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    private int index;

    private List<LeaderBoardPlayer> lBoardPlayers = new List<LeaderBoardPlayer>();

    public enum GameState
    {
        Waiting,
        Playing,
        Ending
    }

    public int killsToWin = 3;
    public Transform mapCamPoint;
    public GameState state = GameState.Waiting;
    public float waitAfterEnding = 5f;

    public bool perpetual;

    public float matchLength = 100f;
    private float currentMatchTime;
    private float sendTimer;

    void Start()
    {
        //hecks if the player is not connected to the PhotonNetwork
        //player is not connected, it loads the scene with index 0
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(0);

        }
        else
        {
            NewPlayerSend(PhotonNetwork.NickName);

            state = GameState.Playing;

            SetupTimer();

            if(!PhotonNetwork.IsMasterClient)
            {
                UIController.instance.timerText.gameObject.SetActive(false);
            }
        }
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && state != GameState.Ending)
        {
            if (UIController.instance.leaderboard.activeInHierarchy)
            {
                UIController.instance.leaderboard.SetActive(false);
            }
            else
            {
                ShowLeaderBoard();
            }
        }

        if (PhotonNetwork.IsMasterClient)
        {
            // If the match time is greater than zero and the game state is Playing
            if (currentMatchTime > 0f && state == GameState.Playing)
            {
                // Decrease the match time by the time that has passed since the last frame
                currentMatchTime -= Time.deltaTime;

                // If the match time is less than or equal to zero
                if (currentMatchTime <= 0f)
                {
                    currentMatchTime = 0f;
                    state = GameState.Ending;

                   //send a list of players and check the game state.
                        ListPlayerSend();

                        StateCheck();
                    
                }

//update the timer display in the game.
                UpdaeTimerDispay();

                sendTimer -= Time.deltaTime;

                if (sendTimer <= 0f)
                {
                    sendTimer += 1f;
                    TimerSend();
                }
            }
        }
    }
//triggered when a custom event is received from the network. - photon 
    public void OnEvent(EventData photonEvent)
    {
        //Photon uses event codes less than 200 for internal events
        if (photonEvent.Code < 200)
        {
            //event code to the EventCodes enum,
            EventCodes theEvent = (EventCodes)photonEvent.Code;
            // extracts the custom data from the event. This data can be anything related to the event.
            object[] data = (object[])photonEvent.CustomData;

            //  Debug.Log("Received event " + theEvent);

            switch (theEvent)
            {
                case EventCodes.NewPlayer:
                    NewPlayerReceive(data);
                    break;
                case EventCodes.ListPlayers:
                    ListPlayerReceive(data);
                    break;
                case EventCodes.UpdateStat:
                    UpdateStatsReceive(data);
                    break;
                case EventCodes.NextMatch:
                    NextMatchReceive();
                    break;
                case EventCodes.TimerSync:
                    TimerReceive(data);
                    break;


            }
        }
    }

    //
    public override void OnEnable()
    {
        // When the MatchManager component is enabled, register it as a callback target.
        // This means that it will start receiving callbacks from the Photon Network.
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        // When the MatchManager component is disabled, unregister it as a callback target.
        // This means that it will stop receiving callbacks from the Photon Network.
        PhotonNetwork.RemoveCallbackTarget(this);

    }

    public void NewPlayerSend(string username)
    {
        object[] package = new object[4];
        package[0] = username;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[2] = 0;
        package[3] = 0;

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }

    public void NewPlayerReceive(object[] dataReceived)
    {
        PlayerInfo player = new PlayerInfo(
            (string)dataReceived[0],
            (int)dataReceived[1],
            (int)dataReceived[2],
            (int)dataReceived[3]
        );

        allPlayers.Add(player);

        ListPlayerSend();
    }

    public void ListPlayerSend()
    {
        object[] package = new object[allPlayers.Count + 1];

        package[0] = state;

        for (int x = 0; x < allPlayers.Count; x++)
        {
            object[] piece = new object[4];
            piece[0] = allPlayers[x].name;
            piece[1] = allPlayers[x].actor;
            piece[2] = allPlayers[x].kills;
            piece[3] = allPlayers[x].deaths;

            package[x + 1] = piece;
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ListPlayers,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void ListPlayerReceive(object[] dataReceived)
    {
        allPlayers.Clear();

        state = (GameState)dataReceived[0];

        for (int x = 1; x < dataReceived.Length; x++)
        {
            object[] piece = (object[])dataReceived[x];

            PlayerInfo player = new PlayerInfo(
                (string)piece[0],
                (int)piece[1],
                (int)piece[2],
                (int)piece[3]
            );

            allPlayers.Add(player);

            if (player.actor == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                index = x - 1;
            }
        }

        StateCheck();
    }
    public void UpdateStatsSend(int actorSending, int statType, int amount)
    {
        object[] package = new object[] { actorSending, statType, amount };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdateStat,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }


    public void UpdateStatsReceive(object[] dataReceived)
    {
        int actor = (int)dataReceived[0];
        int statType = (int)dataReceived[1];
        int amount = (int)dataReceived[2];

        for (int x = 0; x < allPlayers.Count; x++)
        {
            if (allPlayers[x].actor == actor)
            {
                switch (statType)
                {
                    case 0://kills
                        allPlayers[x].kills += amount;
                        Debug.Log("player " + allPlayers[x].name + " has " + allPlayers[x].kills + " kills");
                        break;
                    case 1: //deaths
                        allPlayers[x].deaths += amount;
                        Debug.Log("player " + allPlayers[x].name + " has " + allPlayers[x].deaths + " deaths");
                        break;
                }

                if (x == index)
                {
                    UpdateStatsDisplay();
                }

                if (UIController.instance.leaderboard.activeInHierarchy)
                {
                    ShowLeaderBoard();
                }

                break;
            }
        }

        ScoreCheck();
    }

    public void UpdateStatsDisplay()
    {
        if (allPlayers.Count > index)
        {
            UIController.instance.killsText.text = "Kills: " + allPlayers[index].kills.ToString();
            UIController.instance.deathsText.text = "Deaths: " + allPlayers[index].deaths.ToString();
        }
        else
        {
            UIController.instance.killsText.text = "Kills: 0";
            UIController.instance.deathsText.text = "Deaths: 0";
        }

    }

    void ShowLeaderBoard()
    {
        UIController.instance.leaderboard.SetActive(true);

        foreach (LeaderBoardPlayer lplayer in lBoardPlayers)
        {
            Destroy(lplayer.gameObject);
        }
        lBoardPlayers.Clear();

        UIController.instance.leaderBoardPlayerDisplay.gameObject.SetActive(false);

        List<PlayerInfo> sorted = SortPlayers(allPlayers);
        foreach (PlayerInfo player in sorted)
        {
            LeaderBoardPlayer newPlayerDisplay = Instantiate(UIController.instance.leaderBoardPlayerDisplay, UIController.instance.leaderBoardPlayerDisplay.transform.parent);
            newPlayerDisplay.setDetails(player.name, player.kills, player.deaths);

            newPlayerDisplay.gameObject.SetActive(true);

            lBoardPlayers.Add(newPlayerDisplay);

        }
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> players)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        while (sorted.Count < players.Count)
        {
            int highest = -1;
            PlayerInfo selectedPlayer = players[0];

            foreach (PlayerInfo player in players)
            {
                if (!sorted.Contains(player))
                {
                    if (player.kills > highest)
                    {
                        selectedPlayer = player;
                        highest = player.kills;
                    }
                }
            }

            sorted.Add(selectedPlayer);
        }

        return sorted;
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(0);
    }

    void ScoreCheck()
    {
        bool winnerFound = false;
        foreach (PlayerInfo player in allPlayers)
        {
            if (player.kills >= killsToWin)
            {
                winnerFound = true;
                break;
            }
        }

        if (winnerFound)
        {
            if (PhotonNetwork.IsMasterClient && state != GameState.Ending)
            {
                state = GameState.Ending;
                ListPlayerSend();

            }
        }
    }
    void StateCheck()
    {
        if (state == GameState.Ending)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        state = GameState.Ending;

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }

        UIController.instance.endScreen.SetActive(true);
        ShowLeaderBoard();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Camera.main.transform.position = mapCamPoint.position;
        Camera.main.transform.rotation = mapCamPoint.rotation;

        StartCoroutine(EndCo());

    }

    private IEnumerator EndCo()
    {
        yield return new WaitForSeconds(waitAfterEnding);


        if (!perpetual)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (!Launcher.instance.changeMapBetweenRounds)
                {
                    NextMatchSend();
                }
                else
                {
                    int mapIndex = Random.Range(0, Launcher.instance.allMaps.Length);

                    if (Launcher.instance.allMaps[mapIndex] == SceneManager.GetActiveScene().name)
                    {
                        NextMatchSend();
                    }
                    else
                    {
                        PhotonNetwork.LoadLevel(Launcher.instance.allMaps[mapIndex]);
                    }


                }

            }
        }

    }

    public void NextMatchSend()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NextMatch,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void NextMatchReceive()
    {
        state = GameState.Playing;

        UIController.instance.endScreen.SetActive(false);
        UIController.instance.leaderboard.SetActive(false);

        foreach (PlayerInfo player in allPlayers)
        {
            player.kills = 0;
            player.deaths = 0;
        }

        UpdateStatsDisplay();

        PlayerSpawner.instance.SpawnPlayer();

        SetupTimer();

    }

    public void SetupTimer()
    {
        if (matchLength > 0)
        {
            currentMatchTime = matchLength;
            UpdaeTimerDispay();
        }
    }

    public void UpdaeTimerDispay()
    {
        var timeToDisplay = System.TimeSpan.FromSeconds(currentMatchTime);
        UIController.instance.timerText.text = timeToDisplay.Minutes.ToString("00") + ":" + timeToDisplay.Seconds.ToString("00");
    }

    public void TimerSend()
    {
        object[] package = new object[] { (int)currentMatchTime, state };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.TimerSync,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );

    }

    public void TimerReceive(object[] dataReceived)
    {
        currentMatchTime = (int)dataReceived[0];
        state = (GameState)dataReceived[1];

        UpdaeTimerDispay();

        UIController.instance.timerText.gameObject.SetActive(true);
    }

}



[System.Serializable]
public class PlayerInfo
{
    public string name;
    public int actor, kills, deaths;

    public PlayerInfo(string _name, int _actor, int _kills, int _deaths)
    {
        name = _name;
        actor = _actor;
        kills = _kills;
        deaths = _deaths;
    }

}