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
            toggleState();
            if (active) {
                setLED(true);
            }
            else {
                setLED(false);
            }
        }

        #endregion

        #region Light Sensor
        

        
        #endregion

        #region Timer

        void timerTick(GT.Timer timer) {
            Debug.Print("Sensed... " + lightSensor.ReadLightSensorPercentage().ToString());
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

        void writeNewFile(string file, string data) {
            byte[] dataBytes;

            dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
            storage.WriteFile(file + ".csv", dataBytes);
        }

        #endregion

        #region Program

        void startLogging() {
            if (!sdCard.IsCardMounted) {
                sdCard.MountSDCard();
            }
            storage = sdCard.GetStorageDevice();
            writeNewFile("day 500", "gerome, mark, ninja");
        }

        void finishLogging() {
            sdCard.UnmountSDCard();
            storage = null;
        }

        #endregion

        void ProgramStarted() {
            button.ButtonPressed += new Button.ButtonEventHandler(buttonPressed);

            GT.Timer timer = new GT.Timer(5000);
            timer.Tick += new GT.Timer.TickEventHandler(timerTick);
            timer.Start();
        }

    }
}