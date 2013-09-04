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

        #region Lights

        public void toggleLED(bool state)
        {
            Mainboard.SetDebugLED(state);
        }

        #endregion

        void ProgramStarted()
        {
        }
    }
}