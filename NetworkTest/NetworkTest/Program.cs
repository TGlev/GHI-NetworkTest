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
using GHIElectronics.TinyCLR.Devices.Can;

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

            //80:1F:12:EE:E0:6B
            Network = new Network("192.168.31.1", "255.255.255.0", "192.168.31.1", "192.168.31.1",
                new byte[] { 0x80, 0x1F, 0x12, 0xEE, 0xE0, 0x6B }, ethReset: SC20100.GpioPin.PD4, ethInterrupt: SC20100.GpioPin.PC5, chipSelect: SC20100.GpioPin.PD3);
            Network.OnLinkChanged = NetworkLinkChanged;
            Network.Initialize();

            //Initialize the CAN interfaces with their filters
            CAN.Initialize(Bitrate1: 250_000);

            //Comment this line and the code will run forever
            //CAN.ReceiveCAN1(CAN_Received);

            Thread LED = new Thread(LedThread);
            LED.Start();

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        private static CanMessage _outMessage;
        private static void CAN_Received(CanController sender, MessageReceivedEventArgs e)
        {
            while (sender.MessagesToRead > 0)
            {
                sender.ReadMessage(out _outMessage);

                //Check if it's a retina message
                /*if ((_outMessage.ArbitrationId & 0x16E00000) == 0x16E00000)
                {
                    //Handle it as retina message
                    if (_outMessage.ArbitrationId == 0x16EEE001) //DS user interface message
                        UserInterfaceData.ParseHardwareMessage(_outMessage);
                    else if(_outMessage.ArbitrationId == 0x16EEE021)
                        UserInterfaceData.ParseTouchMessage(_outMessage);
                    else if(_outMessage.ArbitrationId == 0x16E40001) //UCAN analog ROT message
                        VesselData.ParseMessage(_outMessage);
                }
                else if ((_outMessage.ArbitrationId & 0x0CAC0000) == 0x0CAC0000)
                    Thrusters.ParseMessage(_outMessage); //Send it over to the thrusters class to handle*/
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
