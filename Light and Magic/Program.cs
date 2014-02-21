using System;
using System.IO;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
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

		// Book keeping
		bool isRecording;

		// Used for reading/writing to the SD card
		Stream stream;
		StreamWriter writer;
		GT.StorageDevice storage;

		//Timers 
		GT.Timer pollingTimer;
		int pollingRatePosition;
		int[] pollingRates = { 20000, 300000, 600000, 1200000 };

		GT.Timer delayTimer;
		int delayTiming = 4000;

		GT.Timer heartbeat;
		int heartbeatRate = 30000;
		bool tickTock = true;

		// Touch screen
		Window window;

		#region Lights

		private void setMainboardLED(bool state) {
			Mainboard.SetDebugLED(state);
		}

		private void setButtonLED(bool state) {
			Debug.Print(button.GetType().ToString());
			if (state) {
				button.TurnLEDOn();
			} else {
				button.TurnLEDOff();
			}
		}

		#endregion

		#region Sensors

		private double GetLightIntensitiy() {
			return System.Math.Round(lightSensor.ReadLightSensorPercentage());			
		}

		private double CalculateLuminance(uint red, uint green, uint blue) {
			return System.Math.Round(((0.2126 * red) + (0.7152 * green) + (0.0722 * blue)));
		}
        
		#endregion

		#region button

		public void buttonPressed(GHIE.Button sender, GHIE.Button.ButtonState state) {
			if (isRecording) {
				StopRecording();
			} else {
				StartRecording();
			}
		}

		#endregion

		#region Timer

		private void delayTick(GT.Timer timer) {
			Debug.Print("New polling rate " + pollingRates[pollingRatePosition]);

			Hashtable data = new Hashtable();
			data.Add("polling-rate", pollingRates[pollingRatePosition].ToString());

			WiFi.SendData(data);

			pollingTimer.Start();
		}

		private void heartbeatTick(GT.Timer timer) {
			Hashtable data = new Hashtable();

			string value = (tickTock) ? "1" : "0";
			data.Add("heartbeat", value);
			tickTock = !tickTock;

			WiFi.SendData(data);		
		}

		private void timerTick(GT.Timer timer) {
			Debug.Print("tick");
			ColorSense.ColorChannels channel;
			double light;
			double luminance;
			uint red, green, blue;

			light = GetLightIntensitiy();

			channel = colorSense.ReadColorChannels();
			red = channel.Red;
			green = channel.Green;
			blue = channel.Blue;
			luminance = CalculateLuminance(red, green, blue);

			Hashtable dataToSend = new Hashtable();
			dataToSend.Add("Red", red);
			dataToSend.Add("Green", green);
			dataToSend.Add("Blue", blue);
			dataToSend.Add("Intensity", light);
			dataToSend.Add("Luminosity", luminance);
			WiFi.SendData(dataToSend);

			if (isRecording) {
				writer.WriteLine(DateTime.Now.ToString("u") + "," + 
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
				stream = storage.Open(GetFileName(), FileMode.OpenOrCreate, FileAccess.Write);
				writer = new StreamWriter(stream);
				writer.WriteLine("Time, Percent, Luma, Red, Green, Blue");
				isRecording = true;
				setMainboardLED(true);
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
				setMainboardLED(false);

				Display.SendMessage("Not recording");
				Debug.Print("Stopped recording\n");
			}
		}

		private Hashtable ReadConfig() {
			Stream stream;
			GT.StorageDevice storage;

			Hashtable config;
			config = new Hashtable();

			if (sdCard.verifySDCard().GetResponse()) {
				storage = sdCardModule.GetStorageDevice();
				stream = storage.OpenRead("config.json");

				byte[] data = new byte[stream.Length];
				stream.Read(data, 0, (data.Length));
				stream.Close();

				string fileContents = new string(System.Text.Encoding.UTF8.GetChars(data));

				config = JsonSerializer.DeserializeString(fileContents) as Hashtable;
			}
			
			return config;
		}

		private string GetFileName() {
			string fileName;

			fileName = DateTime.Now.ToString("yyyy-MM-dd");

			return fileName + ".csv";
		}

		#endregion

		#region Touch

		void InitTouch() {
			window = display.GetWindow();
			window.TouchUp += new Microsoft.SPOT.Input.TouchEventHandler(TouchUp);
		}

		public void TouchUp(object sender, Microsoft.SPOT.Input.TouchEventArgs e) {
			pollingRatePosition++;
			if (pollingRatePosition >= pollingRates.Length) {
				pollingRatePosition = 0;
			}

			pollingTimer.Stop();
			pollingTimer.Interval = new TimeSpan(0, 0, 0, 0, pollingRates[pollingRatePosition]);

			if (!delayTimer.IsRunning) {
				delayTimer.Start();
			}
		}

		#endregion

		#region DateTime

		private void SendDateTimeRequest() {
			WiFi.GetDateTime();
		}

		public static void UpdateDateTime(string response) {
			DateTime datetime = DateTimeExtensions.FromIso8601(response);

			Utility.SetLocalTime(datetime);
		}

		#endregion

		private void SetupFromConfig() {

			Hashtable config = ReadConfig();

			if (config.Contains("wifi")) {
				Hashtable wifiDetails = config["wifi"] as Hashtable;
				if (wifiDetails.Contains("ssid") && wifiDetails.Contains("passphrase")) {
					WiFi.Init(wifiModule, wifiDetails["ssid"].ToString(), wifiDetails["passphrase"].ToString());
				}
			}

			if (config.Contains("xively")) {
				Hashtable xivelyDetails = config["xively"] as Hashtable;
				if (xivelyDetails.Contains("endpoint") && xivelyDetails.Contains("api_key")) {
					WiFi.UpdateServerInformation(xivelyDetails["endpoint"].ToString(), xivelyDetails["api_key"].ToString());
				}
			}

			SendDateTimeRequest();
		}

		void ProgramStarted() {
			sdCard = new SDCard(sdCardModule);
			display = new Display(displayModule);

			isRecording = false;

			SetupFromConfig();

			display.Init();
			InitTouch();

			pollingRatePosition = 0;

			pollingTimer = new GT.Timer(pollingRates[pollingRatePosition]);
			pollingTimer.Tick += new GT.Timer.TickEventHandler(timerTick);
			pollingTimer.Start();

			heartbeat = new GT.Timer(heartbeatRate);
			heartbeat.Tick += new GT.Timer.TickEventHandler(heartbeatTick);
			heartbeatTick(heartbeat);
			heartbeat.Start();

			delayTimer = new GT.Timer(delayTiming, GT.Timer.BehaviorType.RunOnce);
			delayTimer.Tick += new GT.Timer.TickEventHandler(delayTick);

			button = new GTM.GHIElectronics.Button(8);
			button.ButtonPressed += new GTM.GHIElectronics.Button.ButtonEventHandler(buttonPressed);
		}

	}
}