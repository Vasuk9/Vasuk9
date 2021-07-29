using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;
using UnityEngine.UI;

public class PlayFabManager : MonoBehaviour
{
    private string deviceLoginId;
    [SerializeField]
    private string gameTitleAndroid, gameTitleIOS;//copy and paste title Id from playfab account
    private string titleId;


    private PlayerData currentPlayer;
    private List<PlayerData> playerData = new List<PlayerData>();
    private bool addPlayer = true;
    public List<GameObject> leaderBoardobjects;
    public GameObject leaderBoardPlayer;
    public Transform leaderboardParent, currentPlayerParent;
    public GameObject leaderboardPanel;
    private string currentPlayerId; //Current player PlayfabID
    private int rankGetPlayer; //rank of current player
    private bool isMyRankPresent = false;



    public static PlayFabManager instance;
    public void Awake()
    {
        if (instance == null)
            instance = this;

        #if UNITY_ANDROID
        {
            titleId = gameTitleAndroid;
            deviceLoginId = SystemInfo.deviceUniqueIdentifier;
        }
        #elif UNITY_IOS
        {
           titleId = gameTitleIOS;
           deviceLoginId = "//IOS Device Id//";
        }
        #endif
    }

    public void Start()
    {

        LoginWithCustomIDRequest request = new LoginWithCustomIDRequest()
        {
            TitleId = titleId,
            CreateAccount = true,
            CustomId = deviceLoginId, // add android or ios deviceid or facebook userid//
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true,
            }
        };

        PlayFabClientAPI.LoginWithCustomID(request, (result) => {
            currentPlayerId = result.PlayFabId;
          
            if (result.NewlyCreated)
            {
                Debug.Log("-New Account Created-Playfab-");
            }
            else
            {
                Debug.Log("-Existing Account-Playfab-");
            }
            string name = null;
            if (result.InfoResultPayload.PlayerProfile != null)
                name = result.InfoResultPayload.PlayerProfile.DisplayName;
            Debug.Log("--Login-Playfab Success--");
        },
             (error) => {
                 Debug.Log(error.ErrorMessage);
             });
    } 

   // Submit scores from game
    public void SendScoresPlayfab()
    {
        PostScores(500);
    }


    //** Get UserName from the Login Panel**//
    public void GetUsernameFromLogin(string name) 
    {
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = name.ToString(),
        };
        PlayFabClientAPI.UpdateUserTitleDisplayName(request, nameUpdated, nameError);
    }

    private void nameError(PlayFabError obj)
    {
        Debug.Log(obj.GenerateErrorReport());
    }

    private void nameUpdated(UpdateUserTitleDisplayNameResult obj)
    {
        Debug.Log("--Name Updated--Playfab");
    }


    //** Post the Player Total Score /  high scores / best time **//
    public void PostScores(int starCount)
    {
        PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate> {
            new StatisticUpdate {
                StatisticName = "Global",
                Value = starCount
              
            }
        }
        }, result => OnStatisticsUpdated(result), FailureCallback);
    }

    private void FailureCallback(PlayFabError obj)
    {
        Debug.LogError(obj.GenerateErrorReport());
    }

    private void OnStatisticsUpdated(UpdatePlayerStatisticsResult result)
    {
        GetCurrentPlayerPosition();
    }

    public void RequestLeaderboard()
    {
        leaderboardPanel.SetActive(true);
        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
        {
            StatisticName = "Global", //Name of leaderboard 
            StartPosition = 0,
            MaxResultsCount = 10 // Total no of players u want to retrive
        }, result => DisplayLeaderBoard(result), FailureCallbackShow);
    }

    private void FailureCallbackShow(PlayFabError obj)
    {
        throw new NotImplementedException();
    }

    // Get rank of Current player from leaderboard
    public void GetCurrentPlayerPosition()
    {
      
        PlayFabClientAPI.GetLeaderboardAroundPlayer(
            new PlayFab.ClientModels.GetLeaderboardAroundPlayerRequest()
            {
               
                MaxResultsCount = 1,   //Get current player to display
                StatisticName = "Global",
            },

              result => GetMyRankFromLeaderBoard(result),
              error => Debug.Log(error.GenerateErrorReport())
              );
    }
    
    void GetMyRankFromLeaderBoard(GetLeaderboardAroundPlayerResult result)
    {
        foreach (var item in result.Leaderboard)
        {
            GameObject temp = Instantiate(leaderBoardPlayer, currentPlayerParent) as GameObject;
            int rank = item.Position;
            rank = rank + 1;
            temp.transform.GetChild(1).GetComponent<Text>().text = rank.ToString();
            temp.transform.GetChild(2).GetComponent<Text>().text = item.DisplayName;
            temp.transform.GetChild(3).GetComponent<Text>().text = item.StatValue.ToString();
            Text[] txt =  temp.GetComponentsInChildren<Text>();
            temp.transform.GetChild(0).transform.GetChild(0).gameObject.SetActive(true);
           

        }
    }

    private void DisplayLeaderBoard(GetLeaderboardResult result)
    {
        
        foreach (var item in result.Leaderboard)
        {
            for (int i = 0; i <playerData.Count; i++)
            {
              
                if (playerData[i].rank == item.Position + 1 && playerData[i].name == item.DisplayName && playerData[i].stars == item.StatValue)
                {
                    print("present");
                    addPlayer = false;
                }
            }
            if (addPlayer)
            {
                PlayerData temp = new PlayerData
                {
                    rank = item.Position + 1,
                    name = item.DisplayName,
                    stars = item.StatValue

                };

                playerData.Add(temp);
                if (item.PlayFabId == currentPlayerId) // current user playfab id
                {
                    isMyRankPresent = true;
                    rankGetPlayer = temp.rank; //Current user Rank
                }
            }
            AssignLeaderBoarditems();
            
        }

        if (isMyRankPresent == false)
             GetCurrentPlayerPosition();
        
    }

    public void AssignLeaderBoarditems()
    {
        if (leaderBoardobjects.Count < playerData.Count)
        {
            CreateLeaderBoardNew(leaderBoardobjects.Count, playerData.Count);
            UpdateLeaderBoardDataPlayer();
        }

    }
    public void CreateLeaderBoardNew(int first, int last)
    {
        for (int i = first; i < last; i++)
        {
            GameObject temp = Instantiate(leaderBoardPlayer, leaderboardParent) as GameObject;
            leaderBoardobjects.Add(temp);
        }

    }

    public void UpdateLeaderBoardDataPlayer()
    {
        if (playerData.Count > 0)
        {
            for (int i = 0; i < playerData.Count; i++)
            {
                if (leaderBoardobjects[i] != null && playerData[i] != null)
                {
                  AssignLeaderBoardDataNew(leaderBoardobjects[i], playerData[i]);
                }
            }
        }
    }

    public void AssignLeaderBoardDataNew(GameObject displayplayer, PlayerData data)
    {
        displayplayer.SetActive(true);
        switch (data.rank)
        {
            case 1:
                displayplayer.transform.GetChild(1).transform.GetChild(0).gameObject.SetActive(true);
                if (data.rank == rankGetPlayer)
                    displayplayer.transform.GetChild(0).transform.GetChild(0).gameObject.SetActive(true);
                break;
            case 2:
                displayplayer.transform.GetChild(1).transform.GetChild(1).gameObject.SetActive(true);
                if (data.rank == rankGetPlayer)
                    displayplayer.transform.GetChild(0).transform.GetChild(0).gameObject.SetActive(true);
                break;
            case 3:
                displayplayer.transform.GetChild(1).transform.GetChild(2).gameObject.SetActive(true);
                if (data.rank == rankGetPlayer)
                    displayplayer.transform.GetChild(0).transform.GetChild(0).gameObject.SetActive(true);
                break;
            default:
               if(data.rank == rankGetPlayer)
                    displayplayer.transform.GetChild(0).transform.GetChild(0).gameObject.SetActive(true);
                break;
        }

        displayplayer.transform.GetChild(1).GetComponent<Text>().text = data.rank.ToString();
        displayplayer.transform.GetChild(2).GetComponent<Text>().text = data.name.ToString();
        displayplayer.transform.GetChild(3).GetComponent<Text>().text = data.stars.ToString();

    }

    

    public void ClearPlayers()
    {
        foreach(var item in leaderBoardobjects)
        {
            item.gameObject.SetActive(false);
        }
    }
    public class PlayerData
    {
       public string name;
       public int stars;
       public int rank;
    }
}
