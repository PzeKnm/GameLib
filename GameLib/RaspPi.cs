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
      GPIOPinDriver _drv17;
      GPIOPinDriver _drv27;

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

    }
}
