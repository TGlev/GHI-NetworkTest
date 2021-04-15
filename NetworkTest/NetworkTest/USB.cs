using GHIElectronics.TinyCLR.Devices.Storage;
using GHIElectronics.TinyCLR.Devices.UsbHost;
using GHIElectronics.TinyCLR.IO;
using GHIElectronics.TinyCLR.Pins;
using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace NetworkTest
{
    public class USB
    {
        public static bool FlashDriveInserted;
        public static StorageController StorageController;
        public static IDriveProvider Driver;

        public static void Initialize()
        {
            //Get the default USB controller, we only have 1 on the chip so it's always the correct one
            var usbHostController = UsbHostController.GetDefault();

            //Subscribe to the connection changed event
            usbHostController.OnConnectionChangedEvent += UsbHostController_OnConnectionChangedEvent;

            //Enable the usb host controller
            usbHostController.Enable();
        }

        private static void UsbHostController_OnConnectionChangedEvent(UsbHostController sender, DeviceConnectionEventArgs e)
        {
            //If the status is not connected, we assume the flashdrive has been taken out
            if (e.DeviceStatus != DeviceConnectionStatus.Connected)
            {
                FlashDriveInserted = false;
                FileSystem.Unmount(StorageController.Hdc);
                Debug.WriteLine("USB flashdrive lost!");
                return;
            }

            //Don't do anything if the user did not insert a USB mass storage device
            if (e.Type != BaseDevice.DeviceType.MassStorage)
                return;

            StorageController = StorageController.FromName(SC20100.StorageController.UsbHostMassStorage);
            Driver = FileSystem.Mount(StorageController.Hdc);
            FileSystem.Flush(StorageController.Hdc);

            FlashDriveInserted = true;
            Debug.WriteLine("USB flashdrive found!");
        }
    }
}
