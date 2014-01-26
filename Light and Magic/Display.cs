using System;
using System.IO;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Touch;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using GHINet = GHI.Premium.Net;

namespace Light_and_Magic {
	class Display {
		Display_TE35 display;

		Window window;
		Canvas canvas;
		Font baseFont;
		static Text txtMsg;

		public Display(Display_TE35 displayInput) {
			display = displayInput;
		}

		public void Init() {
			canvas = new Canvas();
			window = display.WPFWindow;
			window.Child = canvas;
			baseFont = Resources.GetFont(Resources.FontResources.NinaB);

			txtMsg = new Text(baseFont, "Ready");
			canvas.Children.Add(txtMsg);
		}

		static public void SendMessage(string message) {
			txtMsg.TextContent = message;
		}

		public Window GetWindow() {
			return window;
		}
	}
}
