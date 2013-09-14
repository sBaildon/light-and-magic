using System;
using System.IO;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

namespace Light_and_Magic {

    public partial class Program {

        bool ledState;
        bool active;
        Stream stream;
        StreamWriter writer;
        GT.StorageDevice storage;

        #region Lights

        void setLED(bool state) {
            Mainboard.SetDebugLED(state);
        }

        void toggleLED() {
            ledState = !ledState;
            Mainboard.SetDebugLED(ledState);
        }

        #endregion

        #region Buttons

        void toggleState() {
            active = !active;
        }

        void buttonPressed(Button sender, Button.ButtonState state) {
            if (active) {
                StopLogging();
                setLED(false);
            } else {
                StartLogging();
                setLED(true);
            }
        }

        #endregion

        #region Light Sensor
        
        string GetLightIntensitiy() {
            return lightSensor.ReadLightSensorPercentage().ToString();
        }
        
        #endregion

        #region Timer

        void timerTick(GT.Timer timer) {
            string light = GetLightIntensitiy();
            Debug.Print("Sensed... " + light);
            if (active) {
                writer.WriteLine("Text, " + light + ", desc");
            }
        }

        void RunOnceTimer(GT.Timer timer) {
            StopLogging();
            Debug.Print("unmounted");
            timer.Stop();
        }

        #endregion           

        #region SD Card

        bool verifySDCard() {
            if (sdCard.IsCardInserted && sdCard.IsCardMounted) {
                Debug.Print("SD card verified");
                return true;
            }
            if (sdCard.IsCardInserted && !sdCard.IsCardMounted) {
                Debug.Print("SD card inserted, not mounted\nMounting...");
                try {
                    sdCard.MountSDCard();
                    Debug.Print("Mounted");
                    return true;
                } catch {
                    Debug.Print("Failed to mount");
                    return false;
                }
            }
            Debug.Print("SD card not found");
            return false;
        }

        #endregion

        #region Program

        //I should probably split this up more, put the stream/writer stuff into its own method
        void StartLogging() {
            if (verifySDCard()) {
                active = true;
                storage = sdCard.GetStorageDevice();
                stream = storage.Open(GetFileName(), FileMode.Create, FileAccess.Write);
                writer = new StreamWriter(stream);
                writer.WriteLine("Time, Percent, Details");
            }
            else {
                Debug.Print("Failed to start");
                active = false;
            }
        }

        void StopLogging() {
            writer.Close();
            stream.Close();
            sdCard.UnmountSDCard();
            storage = null;
            active = false;
        }

        //just a debug method until I can get an RTC working
        string GetFileName() {
            Random rand = new Random();
            return "day" + rand.Next(200).ToString() + ".csv";
        }

        #endregion

        void ProgramStarted() {
            button.ButtonPressed += new Button.ButtonEventHandler(buttonPressed);

            GT.Timer timer = new GT.Timer(5000);
            timer.Tick += new GT.Timer.TickEventHandler(timerTick);
            timer.Start();

            GT.Timer runOnce = new GT.Timer(30000);
            runOnce.Tick += new GT.Timer.TickEventHandler(RunOnceTimer);
            //runOnce.Start();

            active = false;
        }

    }
}