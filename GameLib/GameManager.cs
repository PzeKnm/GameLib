using RaspberryGPIOManager;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Timers;

namespace GameLib
{

  public delegate Game CreateGame(GameManager gm);
    
  public enum GameManagerState
  {
    Initialised = 0,  // Started
    Activated,        // Registered at server, but not available for clients
    Online,           // Registered at server, and available for clients
    Authenticating,   // Access Code generated, waiting for client to send code
    PreGame,          // |
    GamePlaying,      // |  - Client connected
    PostGame,         // |
    Deactivated       // 
  }  

  public class GameManager
  {

    private GameManagerState _gameState;

    private int _accessCode;

    Game _theGame;

    RestApi _restApi;

    // Resets the game
    Watchdog _dogReset;

    // Client takes too long to authenticate
    Watchdog _dogAuthenticationTimeout;

    // Client takes too long to Start game
    Watchdog _dogPreGameTimeout;
    
    // Client takes too long to confirm post game
    Watchdog _dogPostGameTimeout;


    public GameManager(CreateGame createGame)
    {
      _theGame = createGame(this);

      _dogReset = new Watchdog(1000 * 60 * 10);// 10 minutes.
      _dogReset.WatchdogBites += _dog_WatchdogResetBites;
      _dogReset.Start(); 

      _dogAuthenticationTimeout = new Watchdog(1000 * _theGame.GetAuthenticationTimeoutSec());
      _dogAuthenticationTimeout.WatchdogBites += _dog_WatchdogAuthenticationBites;
      
      _dogPreGameTimeout = new Watchdog(1000 * 30);// 30 seconds.
      _dogPreGameTimeout.WatchdogBites += _dog_WatchdogPreGameBites;
      
      _dogPostGameTimeout = new Watchdog(1000 * 30);// 30 seconds.
      _dogPostGameTimeout.WatchdogBites += _dog_WatchdogPostGameBites;
                 

      _restApi = new RestApi(_theGame.GetHubDeviceID(), _theGame.GetHubDeviceKey());
      _theGame.Initialise();
      _theGame.ErrorOccurred += _Game_ErrorOccurred;
      _theGame.ScoreChanged += _Game_ScoreChanged;
      _theGame.GameFinished += _Game_GameFinished;

   //   SetGameState(GameManagerState.Initialised);
           
      Activate();

   //   GameLib.MyUtils mu = new GameLib.MyUtils();
   //   mu.DoSomething();
    }

    public string GetHubDeviceID()
    {
      return _theGame.GetHubDeviceID();
    }

    public int GetHeartbeatMs()
    {
      return _theGame.GetHeartbeatMs();
    }

    private void _Game_ErrorOccurred(object sender, ErrorOccuredArgs e)
    {
      // TODO abort the game
    }

    private void _dog_WatchdogResetBites(object sender, ElapsedEventArgs e)
    {
      // No response from game for a while, reset.
      ResetGame();
    }

    private void _dog_WatchdogAuthenticationBites(object sender, ElapsedEventArgs e)
    {
      if (GetGameManagerState() != GameManagerState.Authenticating)
        return;
      // Client hasn't authorised within limit.
      DetachClient();
      SetGameState(GameManagerState.Online);
    }

    public int GetTotalPreGameSecs()
    {
      return _dogPreGameTimeout.GetTimoutIntervalSec();
    }

    public int GetRemainingPreGameSecs()
    {
      return _dogPreGameTimeout.GetRemainingTimeSec();
    }

    public int GetTotalAuthenticationSecs()
    {
      return _dogAuthenticationTimeout.GetTimoutIntervalSec();
    }

    public int GetRemainingAuthenticationSecs()
    {
      return _dogAuthenticationTimeout.GetRemainingTimeSec();
    }

    public int GetTotalPostGameSecs()
    {
      return _dogPostGameTimeout.GetTimoutIntervalSec();
    }

    public int GetRemainingPostGameSecs()
    {
      return _dogPostGameTimeout.GetRemainingTimeSec();
    }
    
    private void _dog_WatchdogPreGameBites(object sender, ElapsedEventArgs e)
    {
      if (GetGameManagerState() != GameManagerState.PreGame)
        return;
      // Client hasn't begun game within limit.
      DetachClient();
      SetGameState(GameManagerState.Online);
    }

    private void _dog_WatchdogPostGameBites(object sender, ElapsedEventArgs e)
    {
      if (GetGameManagerState() != GameManagerState.PostGame)
        return;      
      SetGameState(GameManagerState.Online);
    }

    private void ResetGame()
    {
      DetachClient();
      SetGameState(GameManagerState.Deactivated);
      Activate();      
    }

    public void SendHeartbeat()
    {
      GameManagerState s = GetGameManagerState();
      if(s == GameManagerState.Online || 
         s == GameManagerState.Authenticating ||
         s == GameManagerState.PreGame ||
         s == GameManagerState.GamePlaying ||
         s == GameManagerState.PostGame)
        _restApi.SendHeartbeat();
    }

    public void Cleanup()
    {
      _theGame.Deinitialise();

      SetGameState(GameManagerState.Deactivated);
    }

    private void Activate()
    {
      if(SetGameState(GameManagerState.Activated))
      {
        // TODO auto move to online, after a pause
        Thread.Sleep(1000);

        if (GetGameManagerState() == GameManagerState.Activated)
          SetGameState(GameManagerState.Online);
      }
      else
      {
        // Activation not allowed.
        Console.WriteLine("Activation refused by server. Is another station already running?");
        SetGameState(GameManagerState.Initialised);
      }
    }

    private void DetachClient()
    {
      _restApi.PublishMessageToClient("ClientDetached", "");
    }

    /// <summary>
    /// Returns true if game state was changed successfully.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    private bool SetGameState(GameManagerState state)
    {
      if (_gameState == state)
        return true;

      _gameState = state;
      DateTime dte = DateTime.Now;
      Console.WriteLine(dte.ToShortDateString() + " - " + dte.ToLongTimeString() + "\t\t\tNew game state: " + _gameState.ToString());
      _dogReset.Reset();
      if(_restApi.UploadStationStatus(_gameState.ToString()))
      {
        _theGame.GameStateHasChanged();
        return true;
      }
     
      return false;
    }

    public GameManagerState GetGameManagerState()
    {
      return _gameState;
    }

    public RestApi GetRestApi() { return _restApi;}

    public int GetAccessCode() { return _accessCode;}

    public bool ProcessConsoleCommand(ConsoleKeyInfo k)
    {
      return _theGame.ProcessConsoleCommand(k);
    }

    public void DoTestCommand(string cmd, string parms)
    {
        GameCommand gc = new GameCommand(cmd, parms);
        ProcessGameCommand(gc);
    }

    public void ProcessGameCommand(GameCommand gc)
    {
      Console.WriteLine("ProcessGameCommand: " + gc.ToString()); 

      if (gc.Command == "GenerateAccessCode")
      {
        if(GetGameManagerState() != GameManagerState.Online)
        {
          string s = "Cannot generate access code in this state.";
          Console.WriteLine(s);
          _restApi.PublishMessageToClient("Response:GenerateAccessCode:Error", s);
          return;
        }

        Console.WriteLine("Generating new access code and sending to server....");
        GenerateNewAccessCode();
        if (_restApi.SendAccessCodeToServer(_accessCode, _theGame.GetAuthenticationTimeoutSec()))
        {
          Console.WriteLine("New Access Code: " + _accessCode.ToString());
          _dogAuthenticationTimeout.Reset();
          SetGameState(GameManagerState.Authenticating);
        }
        else
          Console.WriteLine("Failed to upload access code");

        return;
      }

      if (gc.Command == "AttachClient")
      {
        if (GetGameManagerState() != GameManagerState.Authenticating)
        {
          Console.WriteLine("Cannot attach client in this state.");
          return;
        }

        Console.WriteLine("Attaching client");

        _theGame.ChangeToPreGame();
        _dogPreGameTimeout.Reset();
        // Todo inform client that he was successfully attached.
        SetGameState(GameManagerState.PreGame);
        return;
      }
      

      if (GetGameManagerState() == GameManagerState.PreGame)
      {
        if (gc.Command == "BeginGame")
        {

          _theGame.ChangeToBeginGame();
          SetGameState(GameManagerState.GamePlaying);
        }          

        return;
      }

      if (GetGameManagerState() == GameManagerState.GamePlaying)
      {
        _theGame.ProcessGameCommand(gc);
        return;
      }

    }

    
    private void GenerateNewAccessCode()
    {
      Random r = new Random();
      _accessCode = r.Next(1000, 9999);
    }

    private void _Game_ScoreChanged(object sender, ScoreChangedArgs e)
    {
      Console.WriteLine("new score: " + e.newScore);
      _restApi.PublishMessageToClient("NewScore", e.newScore.ToString());
    }

    private void _Game_GameFinished(object sender, GameFinishedArgs e)
    {
      int nFinalScore = _theGame.GetScore();
      Console.WriteLine("Final Score: " + nFinalScore);
      _restApi.PublishMessageToClient("GameOver", "");
    ////  ResetGame();
      _dogPostGameTimeout.Reset();
      SetGameState(GameManagerState.PostGame);
    }

    public string GetGameCommands()
    {
      return _theGame.GetGameCommands();
    }

  }
   
}
