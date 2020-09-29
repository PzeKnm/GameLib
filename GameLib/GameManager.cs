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
    Online_Ready,     // Registered at server, and available for clients
    Online_Dormant,   // Registered at server, and available for clients, low activity mode
    Online_Demo,      // Registered at server, and available for clients, high activity mode
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

    // Monitors when the game is online but seemingly inactive. Switches state to either dormant or demo.
    Watchdog _dogIdleProcessor;

    // Stores the last time the game/user did something. Used to calculate how long the inactivity period is
    // Doesn't include going from Online_xxx to Online_yyy.
    DateTime _lastGameActivity;

    // When the game started
    DateTime _BootTime;

    /// how long to be idle before entering demo mode 
  ////  int _EnterDemoDelaySecs = 60 * 10;

    // Stores when the demo begins
    //// DateTime _dteDemoBegins;

    // How long to stay in demo mode
    //// int _ExitDemoDelaySecs = 60 * 10;

    // Client takes too long to authenticate
    Watchdog _dogAuthenticationTimeout;

    // Client takes too long to Start game
    Watchdog _dogPreGameTimeout;
    
    // Client takes too long to confirm post game
    Watchdog _dogPostGameTimeout;


    public GameManager(CreateGame createGame)
    {
      _BootTime = DateTime.Now;
      _theGame = createGame(this);

      _dogIdleProcessor = new Watchdog(1000 * 60 * 1);// 1 minutes.
      _dogIdleProcessor.WatchdogBites += _dog_WatchdogIdleProcessorBites;
      _dogIdleProcessor.Start(); 

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

   ////   _EnterDemoDelaySecs = Convert.ToInt32(GetSettingFromApi("EnterDemoDelaySecs", _EnterDemoDelaySecs.ToString()));
   ////   _ExitDemoDelaySecs = Convert.ToInt32(GetSettingFromApi("ExitDemoDelaySecs", _ExitDemoDelaySecs.ToString()));

   //   SetGameState(GameManagerState.Initialised);
           
      Activate();
      
    }

    private string GetSettingFromApi(string name, string defValue)
    {
      string s = _restApi.GetSetting(name);
      if(s != "")
        return s;
      return defValue;
    }

    public string GetHubDeviceID()
    {
      return _theGame.GetHubDeviceID();
    }

    /// <summary>
    /// default every 30s
    /// </summary>
    /// <returns></returns>
    public int GetHeartbeatMs()
    {
      return _theGame.GetHeartbeatMs();
    }

    private void _Game_ErrorOccurred(object sender, ErrorOccuredArgs e)
    {
      // TODO abort the game
    }

    private void _dog_WatchdogIdleProcessorBites(object sender, ElapsedEventArgs e)
    {
      // If the game is busy, let it carry on
      _dogIdleProcessor.Reset();

      if( GetGameManagerState() != GameManagerState.Online_Demo &&
          GetGameManagerState() != GameManagerState.Online_Dormant &&
          GetGameManagerState() != GameManagerState.Online_Ready )
        return;


      // Special case for periodic rebooting, once a day after 1a.m.
      DateTime dte = DateTime.Now;

      TimeSpan spnActive = (dte - _BootTime);
      if(spnActive.TotalHours > 23 && dte.Hour == 1)
      {
        Console.WriteLine("Daily reboot...");
        SetGameState(GameManagerState.Deactivated);
        _theGame.RebootMachine();
      }

      Console.WriteLine(String.Format("Processing idle status...game active for {0:F2} hours", spnActive.TotalHours));
      GameManagerState newState = _theGame.GetGameManagerStateFromIdle();
      SetGameState(newState);
    }

    private void _dog_WatchdogAuthenticationBites(object sender, ElapsedEventArgs e)
    {
      if (GetGameManagerState() != GameManagerState.Authenticating)
        return;
      // Client hasn't authorised within limit.
      DetachClient();
      SetGameState(GameManagerState.Online_Ready);
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


    public int GetTimeWithoutGameActivitySecs()
    {
      TimeSpan ts = DateTime.Now - _lastGameActivity;
      return (int)ts.TotalSeconds;
    }


    private void _dog_WatchdogPreGameBites(object sender, ElapsedEventArgs e)
    {
      if (GetGameManagerState() != GameManagerState.PreGame)
        return;
      // Client hasn't begun game within limit.
      DetachClient();
      SetGameState(GameManagerState.Online_Ready);
    }

    private void _dog_WatchdogPostGameBites(object sender, ElapsedEventArgs e)
    {
      if (GetGameManagerState() != GameManagerState.PostGame)
        return;      
      SetGameState(GameManagerState.Online_Ready);
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
      if(s == GameManagerState.Online_Ready || 
         s == GameManagerState.Online_Demo || 
         s == GameManagerState.Online_Dormant || 
         s == GameManagerState.Authenticating ||
         s == GameManagerState.PreGame ||
         s == GameManagerState.GamePlaying ||
         s == GameManagerState.PostGame)
      {
        _restApi.SendHeartbeat();
      }


    }

    public void Cleanup()
    {
      _theGame.Deinitialise();

      SetGameState(GameManagerState.Deactivated);
    }

    public void Activate()
    {
      if(SetGameState(GameManagerState.Activated))
      {
        // TODO auto move to online, after a pause
        Thread.Sleep(1000);

        if (GetGameManagerState() == GameManagerState.Activated)
          SetGameState(GameManagerState.Online_Ready);
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

      if(state != GameManagerState.Online_Ready && state != GameManagerState.Online_Demo && state != GameManagerState.Online_Dormant)
        _lastGameActivity = DateTime.Now;

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
        if(GetGameManagerState() != GameManagerState.Online_Ready &&
           GetGameManagerState() != GameManagerState.Online_Dormant &&
           GetGameManagerState() != GameManagerState.Online_Demo )
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
