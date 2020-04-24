using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace GameLib
{
  class SignalRClient
  {
    HubConnection _connection;

    string _hubDeviceID;

    //public event EventHandler CommandReceived;
    public event EventHandler<ClientCommandEventArgs> CommandReceived;

    public SignalRClient(string sHubDeviceID)
    {
      _hubDeviceID = sHubDeviceID;
    }
                              
    public async void StartListening()
    {

      _connection = new HubConnectionBuilder()
        .WithUrl("https://sandgatethapi.azurewebsites.net/api/")
        .Build();

      _connection.Closed += async (error) =>
      {
        await Task.Delay(new Random().Next(0, 5) * 1000);
        await _connection.StartAsync();
      };

      _connection.On<ClientMessage>("ClientMessage", (cm) =>
      {
        if(cm.Direction == "SPA2Station" && cm.StationId == _hubDeviceID)
        { 
          Debug.WriteLine(cm.Command);

          ClientCommandEventArgs args = new ClientCommandEventArgs();
          GameCommand gc = new GameCommand(cm.Command, cm.Parameters);
          args.gameCommand = gc;
          OnCommandReceived(args);               
        }

      });


      try
      {
        await _connection.StartAsync();
        //   messagesList.Items.Add("Connection started");
        //   connectButton.IsEnabled = false;
        //    sendButton.IsEnabled = true;
      }
      catch (Exception ex)
      {
        string s = ex.Message;
        //    messagesList.Items.Add(ex.Message);
      }

    }


    protected virtual void OnCommandReceived(ClientCommandEventArgs e)
    {
      CommandReceived?.Invoke(this, e);
    }
          
  }

  public class ClientMessage
  {
    public string Sender;

    public string StationId;

    public string Direction;
    // Token so that the clients know if the message is for them
    public string AccessToken;
    // The Command
    public string Command;
    // The parameters
    public string Parameters;
  }

  public class ClientCommandEventArgs : EventArgs
  {
    public GameCommand gameCommand;
  }


  public class GameCommand
  {
    public GameCommand(string cmd, string parms)
    {
        Command = cmd;
        Parameters = parms;
    }

    public string Command;
    public string Parameters;
  }
}
