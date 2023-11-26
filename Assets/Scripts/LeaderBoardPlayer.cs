using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderBoardPlayer : MonoBehaviour
{
  public TMP_Text playerNameText, killsText, deathsText;

  public void setDetails(string playerName, int kills, int deaths)
  {
    playerNameText.text = playerName;
    killsText.text = kills.ToString();
    deathsText.text = deaths.ToString();
  }
}
