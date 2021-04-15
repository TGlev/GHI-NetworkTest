using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Network;
using GHIElectronics.TinyCLR.Devices.Spi;
using GHIElectronics.TinyCLR.Pins;
using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;

namespace NetworkTest
{
    class Program
    {
        static bool linkReady = false;

        static USB USB;
        static Network Network;

        static void Main()
        {
            //Comment this line to disable USB host
            USB.Initialize();

            Network = new Network("192.168.31.1", "255.255.255.0", "192.168.31.1", "192.168.31.1", new byte[] { 0x00, 0x4, 0x00, 0x00, 0x00, 0x00 }, ethReset: SC20100.GpioPin.PD4, ethInterrupt: SC20100.GpioPin.PC5, chipSelect: SC20100.GpioPin.PD3);
            Network.OnLinkChanged = NetworkLinkChanged;
            Network.Initialize();

            Thread LED = new Thread(LedThread);
            LED.Start();

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        public static void LedThread()
        {
            //init led
            var led = GpioController.GetDefault().OpenPin(SC20100.GpioPin.PB0);
            led.SetDriveMode(GpioPinDriveMode.Output);

            while (true)
            {
                led.Write(GpioPinValue.High);
                Thread.Sleep(500);
                led.Write(GpioPinValue.Low);
                Thread.Sleep(500);
            }
        }

        private static void NetworkLinkChanged(bool HasLink)
        {
            if (HasLink)
                Debug.WriteLine("Network link achieved!");
            else
                Debug.WriteLine("Network link lost!");
        }

    }
}
