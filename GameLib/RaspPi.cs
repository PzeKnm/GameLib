using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RaspberryGPIOManager;

namespace GameLib
{
    public class RaspPi 
    {
      // DO for led
      GPIOPinDriver _drv17;
      // DO for led
      GPIOPinDriver _drv27;

      // DI for Demo mode on off
      GPIOPinDriver _drv22;     

      bool _bSimulationMode;

      public RaspPi(bool bSim)
      {
        _bSimulationMode = bSim;
      }

      public void Initialise()
      {
         // Deinitialise();
          _drv17 = new GPIOPinDriver(GPIOPinDriver.Pin.GPIO17, _bSimulationMode);
          _drv17.Direction = GPIOPinDriver.GPIODirection.Out;

          _drv27 = new GPIOPinDriver(GPIOPinDriver.Pin.GPIO27, _bSimulationMode);
          _drv27.Direction = GPIOPinDriver.GPIODirection.Out;

          _drv22 = new GPIOPinDriver(GPIOPinDriver.Pin.GPIO22, _bSimulationMode);
          _drv22.Direction = GPIOPinDriver.GPIODirection.In;
      }


      public void Deinitialise()
      {
          _drv17.Unexport();
          _drv27.Unexport();
      }

      private GPIOPinDriver GetDriver(GPIOPinDriver.Pin pin)
      {
          if (pin == GPIOPinDriver.Pin.GPIO17)
              return _drv17;
          if (pin == GPIOPinDriver.Pin.GPIO27)
              return _drv27;
          if (pin == GPIOPinDriver.Pin.GPIO22)
              return _drv22;
          return null;
      }


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

      }

      public void PulsePin(GPIOPinDriver.Pin pin, int nDurationMs)
      {
          Task<string> obTask = Task.Run(() =>
          {
              GPIOPinDriver drv = GetDriver(pin);
              if(drv == null)
              {
                  Console.WriteLine("Driver not found");
                  return "Not OK";
              }
              Console.WriteLine("Switch on:" + pin.ToString());
              drv.State = GPIOPinDriver.GPIOState.High;
              Thread.Sleep(nDurationMs);
              drv.State = GPIOPinDriver.GPIOState.Low;    
              Console.WriteLine("Switch off:" + pin.ToString());
              return "OK";
          });      
            
      }

      public int ReadPin(GPIOPinDriver.Pin pin)
      {
        GPIOPinDriver drv = GetDriver(pin);
        if(drv == null)
        {
            Console.WriteLine("Driver not found");
            return 0;
        }
        if(drv.State == GPIOPinDriver.GPIOState.High)
          return 1;
        return 0;
      }

    }
}
