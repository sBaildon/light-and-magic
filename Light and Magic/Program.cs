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

        public void setLED(bool state)
        {
            Mainboard.SetDebugLED(state);
        }

        public void toggleLED()
        {
            ledState = !ledState;
            Mainboard.SetDebugLED(ledState);
        }

        #endregion

        #region Buttons

        public void buttonPressed(Button sender, Button.ButtonState state)
        {
            toggleLED();
        }

        #endregion

        void ProgramStarted()
        {
            button.ButtonPressed += new Button.ButtonEventHandler(buttonPressed);
        }
    }
}