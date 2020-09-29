using RaspberryGPIOManager;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Timers;

namespace GameLib
{
   
  public class GameManagerWorker : WorkerBase
  {

    GameManager _gm;

    private CreateGame _createGame;

    public GameManagerWorker(CreateGame createGame)
    {
      _createGame = createGame;
    }

    public bool ProcessStandardConsoleCommand(ConsoleKeyInfo k)
    {
      if (k.Key == ConsoleKey.G)
      {
        DoTestCommand("GenerateAccessCode", "");
        return true;
      }

      if (k.Key == ConsoleKey.A)
      {
        DoTestCommand("AttachClient", "");
        return true;
      }

      if (k.Key == ConsoleKey.B)
      {
        DoTestCommand("BeginGame", "");
        return true;
      }

      return false;
    }

    public bool ProcessConsoleCommand(ConsoleKeyInfo k)
    {
      return _gm.ProcessConsoleCommand(k);
    }

    public void DoTestCommand(string cmd, string parms)
    {
      if(_gm == null)
      {
        Console.WriteLine("Game Manager not started");
        return;
      }
      _gm.DoTestCommand(cmd, parms);
    }


    /// <summary>
    /// Main work loop of the class.
    /// </summary>
    public void Run()
    {
      Console.WriteLine("GameManagerWorker starts.");
      _gm = new GameManager(_createGame);

      Console.Write(GetGameCommands());

      try
      {
        Console.WriteLine("SignalR starts.");
        SignalRClient _signalClient;
        _signalClient = new SignalRClient(_gm.GetHubDeviceID());
        _signalClient.CommandReceived += _signalClient_CommandReceived; ;
        _signalClient.StartListening();

        while (!Stopping)
        {
          _gm.SendHeartbeat();

          if(_gm.GetGameManagerState() == GameManagerState.Initialised)
          {
            Thread.Sleep(30000);
            Console.WriteLine("Retrying Activate...");
            _gm.Activate();
          }
          Thread.Sleep(_gm.GetHeartbeatMs());


          // Insert work here. Make sure it doesn't tight loop!
          // (If work is arriving periodically, use a queue and Monitor.Wait,
          // changing the Stop method to pulse the monitor as well as setting
          // stopping.)

          // Note that you may also wish to break out *within* the loop
          // if work items can take a very long time but have points at which
          // it makes sense to check whether or not you've been asked to stop.
          // Do this with just:
          // if (Stopping)
          // {
          //     return;
          // }
          // The finally block will make sure that the stopped flag is set.
        }

      }
      finally
      {
        _gm.Cleanup();
        SetStopped();
      }
      Console.WriteLine("GameManagerWorker ends.");
    }

    private void _signalClient_CommandReceived(object sender, ClientCommandEventArgs e)
    {
      Console.WriteLine("_signalClient_CommandReceived");

      _gm.ProcessGameCommand(e.gameCommand);
    }

    public string GetGameCommands()
    {
      string s = "";
      s += "G : GenerateAccessCode" + Environment.NewLine;
      s += "A : AttachClient" + Environment.NewLine;      
      s += "B : BeginGame" + Environment.NewLine;      
      
      s += s = _gm.GetGameCommands();
      return s;
    }
  }

}
