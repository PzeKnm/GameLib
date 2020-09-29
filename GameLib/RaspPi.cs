using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RaspberryGPIOManager;

namespace GameLib
{

  // Allows you to assign logical names to the pins
  public class RaspPin
  {
    public GPIOPinDriver.Pin pinNumber;
    public GPIOPinDriver driver;
    public GPIOPinDriver.GPIODirection direction;
    public string name;
  }


  public class RaspPi 
  {
    // DO for led
    //GPIOPinDriver _drv17;
    // DO for led
    //GPIOPinDriver _drv27;

    // DI for Demo mode on off
    //GPIOPinDriver _drv22;  
    
    List<RaspPin> m_lstPins;

    bool _bSimulationMode;

    public RaspPi(bool bSim)
    {
      _bSimulationMode = bSim;
      m_lstPins = new List<RaspPin>();
    }


    public void InitialiseFromPinArray(List<RaspPin> lstPins)
    {
      foreach(RaspPin pn in lstPins)
      {
        RaspPin pin = new RaspPin();
        pin.driver = new GPIOPinDriver(pn.pinNumber, pn.direction, _bSimulationMode);
        //// pin.direction = pn.direction;
        pin.name = pn.name;
        m_lstPins.Add(pin);
      }

    }
/*
    public void Initialise()
    {
        // Deinitialise();
        
        _drv17 = new GPIOPinDriver(GPIOPinDriver.Pin.GPIO17, _bSimulationMode);
        _drv17.Direction = GPIOPinDriver.GPIODirection.Out;

        _drv27 = new GPIOPinDriver(GPIOPinDriver.Pin.GPIO27, _bSimulationMode);
        _drv27.Direction = GPIOPinDriver.GPIODirection.Out;

        _drv22 = new GPIOPinDriver(GPIOPinDriver.Pin.GPIO22, _bSimulationMode);
        _drv22.Direction = GPIOPinDriver.GPIODirection.In;
    }*/


    public void Deinitialise()
    {
      foreach(RaspPin pin in m_lstPins)
      {
        pin.driver.Unexport();
      }

      m_lstPins.Clear();
    //    _drv17.Unexport();
    //    _drv27.Unexport();
    //    _drv22.Unexport();

        }

    private GPIOPinDriver GetDriver(string name)
    {
      foreach(RaspPin pin in m_lstPins)
      {
        if(pin.name == name)
          return pin.driver;
      }
      /*
        if (pin == GPIOPinDriver.Pin.GPIO17)
            return _drv17;
        if (pin == GPIOPinDriver.Pin.GPIO27)
            return _drv27;
        if (pin == GPIOPinDriver.Pin.GPIO22)
            return _drv22;*/
        return null;
    }

    /*
    public void DoTest()
    {

        GPIOPinDriver drv = new GPIOPinDriver(GPIOPinDriver.Pin.GPIO17, _bSimulationMode);
        drv.Direction = GPIOPinDriver.GPIODirection.Out;
           
        for (int n = 0; n < 10; n++)
        {
            drv.State = GPIOPinDriver.GPIOState.High;                
            Thread.Sleep(100);
            drv.State = GPIOPinDriver.GPIOState.Low;
            Thread.Sleep(100);
        }


        drv.Unexport();

    }*/

    
    public void PulsePin(string name, int nDurationMs)
    {
      Task<string> obTask = Task.Run(() =>
      {
        GPIOPinDriver drv = GetDriver(name);
        if(drv == null)
        {
          Console.WriteLine("Driver not found");
          return "Not OK";
        }
        Console.WriteLine("Switch on:" + name);
        drv.State = GPIOPinDriver.GPIOState.High;
        Thread.Sleep(nDurationMs);
        drv.State = GPIOPinDriver.GPIOState.Low;    
        Console.WriteLine("Switch off:" + name);
        return "OK";
      });    
    }

    public int ReadPin(string name)
    {
      GPIOPinDriver drv = GetDriver(name);
      if(drv == null)
      {
        Console.WriteLine("Driver not found");
        return 0;
      }
      if(drv.State == GPIOPinDriver.GPIOState.High)
        return 1;
      return 0;
    }

    public bool Reboot()
    {
      Console.WriteLine("Rebooting...");
      System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() { FileName = "sudo", Arguments = "reboot" });
      return true;
    }

  }
}
