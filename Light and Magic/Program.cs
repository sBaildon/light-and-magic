using System;
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

namespace Light_and_Magic
{

    public partial class Program
    {

        bool ledState = false;
        
        #region Lights

        void setLED(bool state)
        {
            Mainboard.SetDebugLED(state);
        }

        void toggleLED()
        {
            ledState = !ledState;
            Mainboard.SetDebugLED(ledState);
        }

        #endregion

        #region Buttons

        void buttonPressed(Button sender, Button.ButtonState state)
        {
            toggleLED();
        }

        #endregion

        #region Light Sensor
        

        
        #endregion

        #region Timer

        void timerTick(GT.Timer timer)
        {
            Debug.Print("Sensed... " + lightSensor.ReadLightSensorPercentage().ToString());
        }

        #endregion

        #region SD Card

        bool verifySDCard()
        {
            if (sdCard.IsCardInserted || sdCard.IsCardMounted)
            {
                Debug.Print("Found SD Card");
                return true;
            }
            return false;
        }

        #endregion
        void ProgramStarted()
        {
            button.ButtonPressed += new Button.ButtonEventHandler(buttonPressed);

            GT.Timer timer = new GT.Timer(30000);
            timer.Tick += new GT.Timer.TickEventHandler(timerTick);
            timer.Start();

            verifySDCard();
        }
    }
}