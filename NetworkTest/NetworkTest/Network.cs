using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Network;
using GHIElectronics.TinyCLR.Pins;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using GHIElectronics.TinyCLR.Drivers.Microchip.Enc28J60;

namespace NetworkTest
{
    public delegate void NetworkDelegate(bool HasLink);
    public class Network
    {
        string IP;
        string SubnetMask;
        string Gateway;
        string DNS;
        byte[] MacAddress;

        private readonly GpioPin chipSelectPin;
        private readonly GpioPin ethResetPin;
        private readonly GpioPin ethInterruptPin;

        private string SpiAPI;
        public bool HasLink { get; set; }

        public NetworkDelegate OnLinkChanged;
        public byte[] DeviceIP { get; set; }

        private Thread WatchENCThread;

        public Network(string ip, string subnetmask, string gateway, string dns, byte[] mac, int chipSelect = SC20100.GpioPin.PB8, int ethReset = SC20100.GpioPin.PB9, int ethInterrupt = SC20100.GpioPin.PD6, string spi = SC20260.SpiBus.Spi3)
        {
            IP = ip;
            SubnetMask = subnetmask;
            Gateway = gateway;
            DNS = dns;
            MacAddress = mac;

            DeviceIP = StringToIP(ip);

            chipSelectPin = GpioController.GetDefault().OpenPin(chipSelect);
            ethResetPin = GpioController.GetDefault().OpenPin(ethReset);
            ethInterruptPin = GpioController.GetDefault().OpenPin(ethInterrupt);

            SpiAPI = spi;
        }

        public void Initialize()
        {
            //Get the network controller
            var networkController = NetworkController.FromName("GHIElectronics.TinyCLR.NativeApis.ENC28J60.NetworkController");

            var networkInterfaceSetting = new EthernetNetworkInterfaceSettings();

            var networkCommunicationInterfaceSettings = new SpiNetworkCommunicationInterfaceSettings();

            var settings = new GHIElectronics.TinyCLR.Devices.Spi.SpiConnectionSettings()
            {
                ChipSelectLine = chipSelectPin,
                ClockFrequency = 20_000_000,
                Mode = GHIElectronics.TinyCLR.Devices.Spi.SpiMode.Mode0,
                ChipSelectType = GHIElectronics.TinyCLR.Devices.Spi.SpiChipSelectType.Gpio,
                ChipSelectHoldTime = TimeSpan.FromTicks(10),
                ChipSelectSetupTime = TimeSpan.FromTicks(10)
            };

            networkCommunicationInterfaceSettings.SpiApiName = SpiAPI;
            networkCommunicationInterfaceSettings.GpioApiName = SC20100.GpioPin.Id;
            networkCommunicationInterfaceSettings.SpiSettings = settings;

            networkCommunicationInterfaceSettings.InterruptPin = ethInterruptPin;
            networkCommunicationInterfaceSettings.InterruptEdge = GpioPinEdge.FallingEdge;
            networkCommunicationInterfaceSettings.InterruptDriveMode = GpioPinDriveMode.InputPullUp;

            networkCommunicationInterfaceSettings.ResetPin = ethResetPin;
            networkCommunicationInterfaceSettings.ResetActiveState = GpioPinValue.Low;

            networkInterfaceSetting.Address = new IPAddress(StringToIP(IP));
            networkInterfaceSetting.SubnetMask = new IPAddress(StringToIP(SubnetMask));
            networkInterfaceSetting.GatewayAddress = new IPAddress(StringToIP(Gateway));
            networkInterfaceSetting.DnsAddresses = new IPAddress[] { new IPAddress(StringToIP(DNS)) };

            networkInterfaceSetting.MacAddress = MacAddress;
            networkInterfaceSetting.DhcpEnable = false;
            networkInterfaceSetting.DynamicDnsEnable = false;

            networkController.SetInterfaceSettings(networkInterfaceSetting);
            networkController.SetCommunicationInterfaceSettings(networkCommunicationInterfaceSettings);

            networkController.SetAsDefaultController();

            networkController.NetworkLinkConnectedChanged += NetworkController_NetworkLinkConnectedChanged;

            networkController.Enable();

            WatchENCThread = new Thread(WatchENC);
            WatchENCThread.Start();
        }

        private void WatchENC()
        {
            while (true)
            {
                if (Enc28J60Interface.TransmitErrorCounter() > 0 || Enc28J60Interface.ReceiveErrorCounter() > 0)
                {
                    Debug.WriteLine("Resetting ENC!");
                    Enc28J60Interface.SoftReset();
                }
                Thread.Sleep(1000);
            }
        }

        private void NetworkController_NetworkLinkConnectedChanged(NetworkController sender, NetworkLinkConnectedChangedEventArgs e)
        {
            HasLink = e.Connected;
            if (HasLink)
                Debug.WriteLine("Network link achieved!");
            else
                Debug.WriteLine("Network link lost!");

            if (OnLinkChanged != null)
                OnLinkChanged(HasLink);
        }

        public static byte[] StringToIP(string stringIP)
        {
            byte[] IP = new byte[4];
            string[] splitIP = stringIP.Split('.');

            for (int i = 0; i < 4; i++)
                IP[i] = byte.Parse(splitIP[i]);

            return IP;
        }

    }
}
