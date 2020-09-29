using RaspberryGPIOManager;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace GameLib
{
   

  public class Game
  {

    private GameManager _mgr;
    private int _score;

    private string _hubDeviceID;
    private string _hubDeviceKey;

    // how long the game runs for
    private int _GameLengthSec;

    // Time allowed to key in the access code before giving up and resetting
    private int _AuthenticationTimeoutSec = 30;

    protected int _HeartbeatSec = 30;


    Watchdog _dogEndOfGameTimeout;
    private bool _bEndOfGameHasFlagged;



    //=================================================================
    // These event are captured by the Game Manager
    // Called when score changes
    public event EventHandler<ScoreChangedArgs> ScoreChanged;

    // Called when game actually finishes. Should be called as soon as possible after game timeout triggers.
    public event EventHandler<GameFinishedArgs> GameFinished;

    // Called if an error occurs.
    public event EventHandler<ErrorOccuredArgs> ErrorOccurred;
    //=================================================================

    // These virtual methods can be implemented by the actual game implementation.


    public Game(GameManager mgr)
    {
      _mgr = mgr;
      _hubDeviceID = "Not Set";
      _hubDeviceKey = "Not Set";
      _GameLengthSec = 60;

      _dogEndOfGameTimeout = new Watchdog(1000 * 60);// 
      _dogEndOfGameTimeout.WatchdogBites += _dogEndOfGameTimeout_WatchdogBites;
      _dogEndOfGameTimeout.Start();
    }

    public string GetHubDeviceID() { return _hubDeviceID;}
    public string GetHubDeviceKey() { return _hubDeviceKey;}
    public int GetHeartbeatMs() { return _HeartbeatSec * 1000;}

    protected void SetHubDeviceDetails(string hubDeviceID, string hubDeviceKey)
    {
      _hubDeviceID = hubDeviceID;
      _hubDeviceKey = hubDeviceKey;
    }

    protected RestApi GetRestApi()
    {
      return _mgr.GetRestApi();
    }

    protected int GetAccessCode()
    {
      return _mgr.GetAccessCode();
    }

    protected void SetGameLength(int nSeconds)
    {
      _GameLengthSec = nSeconds;
    }

    protected void SetAuthenticationTimeoutSec(int nSeconds)
    {
      _AuthenticationTimeoutSec = nSeconds;
    }
    
    protected void StartEndOfGameWatchdog()
    {
      _dogEndOfGameTimeout.Reset(_GameLengthSec * 1000);
      _bEndOfGameHasFlagged = false;
    }

    private void _dogEndOfGameTimeout_WatchdogBites(object sender, ElapsedEventArgs e)
    {
      if(GetGameMangerState() != GameManagerState.GamePlaying)
        return;

      _bEndOfGameHasFlagged = true;
      // Call derived game method, which is responsible for finishing up then informing GameManager
      ChangeToEndGame();  
    }

    protected bool HasEndOfGameFlagged()
    {
      return _bEndOfGameHasFlagged;
    }

    protected int GetRemainingGameTimeSec()
    {
      return _dogEndOfGameTimeout.GetRemainingTimeSec();
    }

    protected int GetTotalAthenticationSec()
    {
      return _mgr.GetTotalAuthenticationSecs();
    }

    protected int GetRemainingAthenticationSec()
    {
      return _mgr.GetRemainingAuthenticationSecs();
    }
    
    
    protected void NotifyClientTimeRemaining()
    {
      int nRemaining = GetRemainingGameTimeSec();
      PublishMessageToClient("TimeRemaining", nRemaining.ToString());
    }

    protected int GetTotalPreGameTimeSec()
    {
      return _mgr.GetTotalPreGameSecs();
    }

    protected int GetRemainingPreGameTimeSec()
    {
      return _mgr.GetRemainingPreGameSecs();
    }

    protected int GetTotalPostGameTimeSec()
    {
      return _mgr.GetTotalPostGameSecs();
    }

    protected int GetRemainingPostGameTimeSec()
    {
      return _mgr.GetRemainingPostGameSecs();
    }

    protected int GetTimeWithoutGameActivitySecs()
    {
      return _mgr.GetTimeWithoutGameActivitySecs();
    }

    protected void InformManagerGameFinished()
    {
      GameFinishedArgs args = new GameFinishedArgs();
      args.newScore = _score;
      GameFinished?.Invoke(this, args);
    }



    //=================================================================

    public virtual void Initialise()    {   }

    public virtual void Deinitialise()  {   }

    public int GetAuthenticationTimeoutSec() { return _AuthenticationTimeoutSec; }

    // Called by the manager as the game state changes.
    public virtual void GameStateHasChanged() { }

    // Called by the manager as the game transitions to PreGame state.
    public virtual void ChangeToPreGame() { }

    // Called by the manager as the game transitions to GamePlaying
    public virtual void ChangeToBeginGame() 
    { 
      ResetScore();
    }

    public virtual void ChangeToEndGame() 
    {
      // Override to do something other than immediately end the game. Call
      // base method when the game is actually finished.
      InformManagerGameFinished();
    }
       
    // Called by the manager to abort the game
    public virtual void AbortGame() { }

    // Called when a console command come in via testing
    public virtual bool ProcessConsoleCommand(ConsoleKeyInfo k)  {  return false;  }

    // Called by the manager when a command comes in from client
    public virtual void ProcessGameCommand(GameCommand gc) {  }

    public virtual string GetGameCommands() { return "";}
       
    // Called from the manager to let the game determine whether it should go into 
    // or come out of a demo or dormant state
    public virtual GameManagerState GetGameManagerStateFromIdle() { return GameManagerState.Online_Ready; }

    // Called to reboot the game due to periodic reboot
    public virtual bool RebootMachine() {return false; }

    //=================================================================
    // Score related functions
    public int GetScore()
    {
      return _score;
    }

    protected void ResetScore()
    {
      _score = 0;
      UpdateScore(_score);
    }

    protected void IncreaseScore(int nAdd)
    {
      _score += nAdd;
      UpdateScore(_score);
    }

    protected void UpdateScore(int score)
    {
      ScoreChangedArgs a = new ScoreChangedArgs();
      a.newScore = score;
      ScoreChanged?.Invoke(this, a);
    }
    //=================================================================


    protected void PerformErrorOccured(string sMsg)
    {
      ErrorOccuredArgs args = new ErrorOccuredArgs();
      args.ErrorMessage = sMsg;
      ErrorOccurred?.Invoke(this, args);
    }

    protected GameManagerState GetGameMangerState()
    {
      return _mgr.GetGameManagerState();
    }

    protected bool PublishMessageToClient(string cmd, string parms)
    {
      if(_mgr.GetRestApi() == null)
        return false;

      return _mgr.GetRestApi().PublishMessageToClient(cmd, parms);
    }


    public bool DeactivateGame()
    {
      _mgr.Cleanup();
      return true;
    }
  }

  public class ScoreChangedArgs : EventArgs
  {
    public int newScore;
  }
  
  public class GameFinishedArgs : EventArgs
  {
    public int newScore;
  }

  public class ErrorOccuredArgs : EventArgs
  {
    public string ErrorMessage;
  }

}
