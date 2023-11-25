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
    }

    public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    private int index;

    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(0);

        }else{
            NewPlayerSend(PhotonNetwork.NickName); 
        }
    }


    void Update()
    {

    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code < 200)
        {
            EventCodes theEvent = (EventCodes)photonEvent.Code;
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
        object[] package = new object[allPlayers.Count];

        for (int x = 0; x < allPlayers.Count; x++)
        {
            object[] piece = new object[4];
            piece[0] = allPlayers[x].name;
            piece[1] = allPlayers[x].actor;
            piece[2] = allPlayers[x].kills;
            piece[3] = allPlayers[x].deaths;

            package[x] = piece;
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

        for (int x = 0; x < dataReceived.Length; x++)
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
                index = x;
            }
        }
    }
    public void UpdateStatsSend()
    {

    }

    public void UpdateStatsReceive(object[] dataReceived)
    {

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