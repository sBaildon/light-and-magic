using System;
using System.IO;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Touch;
using Microsoft.SPOT.Net.NetworkInformation;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using GHINet = GHI.Premium.Net;
using GHI.Premium.Net;

namespace Light_and_Magic {

	public partial class Program {

		// Modules
		SDCard sdCard;
		Display display;
		WiFi wifi;

		// Book keeping
		bool isRecording;
		long minutesLogged;

		// Used for reading/writing to the SD card
		Stream stream;
		StreamWriter writer;
		GT.StorageDevice storage;

		Window window;

		#region Lights

		void setLED(bool state) {
			Mainboard.SetDebugLED(state);
		}

		#endregion

		#region Light Sensor

		string GetLightIntensitiy() {
			return lightSensor.ReadLightSensorPercentage().ToString();
		}
        
		#endregion

		#region Timer

		void timerTick(GT.Timer timer) {
			ColorSense.ColorChannels channel;
			string light;
			uint red, green, blue;
			double luma;

			light = GetLightIntensitiy();
			Debug.Print("Mins:  " + minutesLogged);
			Debug.Print("Light: " + light);

			channel = colorSense.ReadColorChannels();
			red = channel.Red;
			green = channel.Green;
			blue = channel.Blue;
			luma = ((0.2126 * red) + (0.7152 * green) + (0.0722 * blue));

			Debug.Print("Luma:  " + luma.ToString());

			Debug.Print("Red:   " + red.ToString() + "\n" +
				    "Green: " + green.ToString() + "\n" +
				    "Blue:  " + blue.ToString() + "\n");

			if (isRecording) {
				minutesLogged = minutesLogged + 10;
				writer.WriteLine(minutesLogged.ToString() + "," + 
						light + "," + 
						luma.ToString() + "," +
						red.ToString() + "," + 
						green.ToString() + "," + 
						blue.ToString());
			}
		}

		#endregion           

		#region Program

		public void StartRecording() {
			if (sdCard.verifySDCard().GetResponse()) {
				storage = sdCardModule.GetStorageDevice();
				stream = storage.Open(GetFileName(), FileMode.Create, FileAccess.Write);
				writer = new StreamWriter(stream);
				writer.WriteLine("Min, Percent, Luma, Red, Green, Blue");
				isRecording = true;
				setLED(true);
				Display.SendMessage("Recording");
				Debug.Print("Recording\n");
			} else {
				Debug.Print("Failed to start recording");
			}
		}

		public void StopRecording() {
			if (sdCard.verifySDCard().GetResponse()) {
				writer.Close();
				stream.Close();
				sdCardModule.UnmountSDCard();
				storage = null;
				isRecording = false;
				minutesLogged = 0;
				setLED(false);
				Display.SendMessage("Not recording");
				Debug.Print("Stopped recording\n");
			}
		}

		string GetFileName() {
			Random rand = new Random();
			return "day" + rand.Next(200).ToString() + ".csv";
		}

		#endregion

		#region Touch

		void InitTouch() {
			window = display.GetWindow();
			window.TouchUp += new Microsoft.SPOT.Input.TouchEventHandler(TouchUp);
		}

		public void TouchUp(object sender, Microsoft.SPOT.Input.TouchEventArgs e) {
			if (isRecording) {
				StopRecording();
			} else {
				StartRecording();
			}
		}

		#endregion

		void ProgramStarted() {
			sdCard = new SDCard(sdCardModule);
			display = new Display(displayModule);
			wifi = new WiFi(wifiModule);

			minutesLogged = 0;
			isRecording = false;

			int intervalInSeconds = 5;
			int intervalInMinutes = 0;
			int intervalInMillis = 0;

			if (intervalInSeconds > 0) {
				intervalInMillis = intervalInSeconds * 1000;
			} else {
				intervalInMillis = intervalInMinutes * 60000;
			}

			display.Init();
			InitTouch();
			wifi.Init("WIFI17", "rilasaci");

			GT.Timer timer = new GT.Timer(intervalInMillis);
			timer.Tick += new GT.Timer.TickEventHandler(timerTick);
			timer.Start();
		}

	}
}