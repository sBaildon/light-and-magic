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

using Json.NETMF;

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

		// Touch screen
		Window window;

		#region Lights

		private void setLED(bool state) {
			Mainboard.SetDebugLED(state);
		}

		#endregion

		#region Sensors

		private string GetLightIntensitiy() {
			return lightSensor.ReadLightSensorPercentage().ToString();
		}

		private string CalculateLuminance(uint red, uint green, uint blue) {
			return (((0.2126 * red) + (0.7152 * green) + (0.0722 * blue))).ToString();
		}
        
		#endregion

		#region Timer

		private void timerTick(GT.Timer timer) {
			ColorSense.ColorChannels channel;
			string light;
			string luminance;
			uint red, green, blue;

			light = GetLightIntensitiy();
			Debug.Print("Mins:  " + minutesLogged);
			Debug.Print("Light: " + light);

			channel = colorSense.ReadColorChannels();
			red = channel.Red;
			green = channel.Green;
			blue = channel.Blue;
			luminance = CalculateLuminance(red, green, blue);

			Debug.Print("Luma:  " + luminance);

			Debug.Print("Red:   " + red.ToString() + "\n" +
				    "Green: " + green.ToString() + "\n" +
				    "Blue:  " + blue.ToString() + "\n");

			Hashtable dataToSend = new Hashtable();
			dataToSend.Add("Red", red.ToString());
			dataToSend.Add("Green", green.ToString());
			dataToSend.Add("Blue", blue.ToString());
			dataToSend.Add("Intensity", light);
			dataToSend.Add("Luminosity", luminance);

			WiFi.SendData(dataToSend);

			if (isRecording) {
				minutesLogged = minutesLogged + 10;
				writer.WriteLine(minutesLogged.ToString() + "," + 
						light + "," + 
						luminance + "," +
						red.ToString() + "," + 
						green.ToString() + "," + 
						blue.ToString());
			}
		}

		#endregion           

		#region Program

		private void StartRecording() {
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

		private void StopRecording() {
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

		private string GetFileName() {
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

			minutesLogged = 0;
			isRecording = false;

			int intervalInSeconds = 20;
			int intervalInMinutes = 5;
			int intervalInMillis;

			if (intervalInSeconds > 0) {
				intervalInMillis = intervalInSeconds * 1000;
			} else if (intervalInMinutes > 0) {
				intervalInMillis = intervalInMinutes * 60000;
			} else {
				intervalInMillis = 60000;
			}
			
			display.Init();
			WiFi.Init(wifiModule, "WIFI17", "rilasaci");

			GT.Timer timer = new GT.Timer(intervalInMillis);
			timer.Tick += new GT.Timer.TickEventHandler(timerTick);
			timer.Start(); 


		}

	}
}