using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace GameLib
{
  public class Watchdog
  {
    private Timer _timer;

    private DateTime _dteLastReset;

    public event EventHandler<ElapsedEventArgs> WatchdogBites;

    public Watchdog(int nTimeoutMs)
    {
      _timer = new Timer();
      _timer.Elapsed += Timer_Elapsed;
      _timer.Interval = nTimeoutMs;
    }
       
    public void Start()
    {
      _timer.Start();
      _dteLastReset = DateTime.Now;
    }

    public void Stop()
    {
      _timer.Stop();
    }

    public void Reset(int nTimeoutMs)
    {
      _timer.Interval = nTimeoutMs;
      Reset();
    }

    public void Reset()
    {
      Stop();
      Start();     
    }

    public int GetRemainingTimeSec()
    {
      TimeSpan tsElapsed = DateTime.Now - _dteLastReset;
      int nElapsed = (int)tsElapsed.TotalSeconds;
      int nIntervalSecs = (int)_timer.Interval / 1000;
      return nIntervalSecs - nElapsed;
    }

    public int GetTimoutIntervalSec()
    {
      return (int)_timer.Interval / 1000;
    }
    

    private void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      _timer.Stop();
      WatchdogBites?.Invoke(this, e);
    }
  }
}
