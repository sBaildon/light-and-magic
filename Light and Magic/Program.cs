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
using GHIE = Gadgeteer.Modules.GHIElectronics;
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
		bool enableImageCapture;
		String sessionDate;

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

		#region LEDs

		private void setMainboardLED(bool state) {
			Mainboard.SetDebugLED(state);
		}

		private void setButtonLED(bool state) {
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

		#region Button

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

			wifi.SendData(data);

			pollingTimer.Start();
		}

		private void heartbeatTick(GT.Timer timer) {
			Hashtable data = new Hashtable();

			string value = (tickTock) ? "1" : "0";
			data.Add("heartbeat", value);
			tickTock = !tickTock;

			wifi.SendData(data);
		}

		private void pollingTick(GT.Timer timer) {
			Debug.Print("tick");
			GHIE.ColorSense.ColorChannels channel;
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
			wifi.SendData(dataToSend);

			if (isRecording) {
				sdCard.WriteLineToFile(sessionDate, "Records.csv", DateTime.Now.ToString("u") + "," +
						light + "," +
						luminance + "," +
						red.ToString() + "," +
						green.ToString() + "," +
						blue.ToString());

				if (enableImageCapture) {
					camera.TakePicture();
				}
			}
		}

		#endregion

		#region Program

		private void StartRecording() {
			if (sdCard.VerifySDCard()) {
				sessionDate = GetSessionDate();

				if (!sdCard.VerifyDirectory(sessionDate)) {
					sdCard.CreateDirectory(sessionDate);
				}

				if (!sdCard.VerifyFile(sessionDate, "Records.csv")) {
					sdCard.CreateFile(sessionDate, "Records.csv");
					sdCard.WriteLineToFile(sessionDate, "Records.csv", "Time, Percent, Luminance, Red, Green, Blue");
				}

				isRecording = true;
				setButtonLED(true);

				Display.SendMessage("Recording");
				Debug.Print("Recording\n");
			} else {
				Debug.Print("Failed to start recording");
			}
		}

		private void StopRecording() {
			if (sdCard.VerifySDCard()) {
				sdCard.Unmount();
				isRecording = false;
				setButtonLED(false);

				Display.SendMessage("Not recording");
				Debug.Print("Stopped recording\n");
			}
		}

		private Hashtable ReadConfig() {
			if (sdCard.VerifySDCard()) {
				string contents;
				contents = sdCard.ReadFile("config.json");

				return JsonSerializer.DeserializeString(contents) as Hashtable;
			}

			return new Hashtable();
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

		public static void UpdateDateTime(string response) {
			Debug.Print("Got time: " + response);
			DateTime datetime = DateTimeExtensions.FromIso8601(response);

			Utility.SetLocalTime(datetime);			
		}

		#endregion

		#region Helpers

		private string GetSessionDate() {
			string date;

			date = DateTime.Now.ToString("yyyy-MM-dd");

			return date;
		}

		private string GetSessionTime() {
			string time;

			time = DateTime.Now.ToString("HH-mm-ss");

			return time;
		}

		#endregion

		private void SetupFromConfig() {

			Hashtable config = ReadConfig();

			if (config.Contains("wifi")) {
				Hashtable wifiDetails = config["wifi"] as Hashtable;
				if (wifiDetails.Contains("ssid") && wifiDetails.Contains("passphrase")) {
					wifi.Connect(wifiDetails["ssid"].ToString(), wifiDetails["passphrase"].ToString());
				}
			}

			if (config.Contains("xively")) {
				Hashtable xivelyDetails = config["xively"] as Hashtable;
				if (xivelyDetails.Contains("endpoint") && xivelyDetails.Contains("api_key")) {
					wifi.UpdateServerInformation(xivelyDetails["endpoint"].ToString(), xivelyDetails["api_key"].ToString());
				}
			}

			if (config.Contains("image_capture")) {
				Hashtable pictureDetails = config["image_capture"] as Hashtable;
				if (pictureDetails.Contains("enable_capture")) {
					enableImageCapture = pictureDetails["enable_capture"].ToString().Equals("yes");
				}
			}
		}

		private void InitialiseTimers() {
			pollingRatePosition = 0;
			pollingTimer = new GT.Timer(pollingRates[pollingRatePosition]);
			pollingTimer.Tick += new GT.Timer.TickEventHandler(pollingTick);
			pollingTimer.Start();

			heartbeat = new GT.Timer(heartbeatRate);
			heartbeat.Tick += new GT.Timer.TickEventHandler(heartbeatTick);
			heartbeat.Start();

			delayTimer = new GT.Timer(delayTiming, GT.Timer.BehaviorType.RunOnce);
			delayTimer.Tick += new GT.Timer.TickEventHandler(delayTick);
		}

		private void InitialiseModules() {
			sdCard = new SDCard(sdCardModule);
			wifi = new WiFi(wifiModule);
			display = new Display(displayModule);
		}

		private void PictureCaptured(GHIE.Camera camera, GT.Picture picture) {
			Debug.Print("picture captured");
			sdCard.SavePicture(sessionDate + "\\" + GetSessionDate() + " " + GetSessionTime() + ".bmp", picture);
		}

		void ProgramStarted() {
			InitialiseModules();

			SetupFromConfig();

			isRecording = false;

			InitTouch();

			button.ButtonPressed += new GHIE.Button.ButtonEventHandler(buttonPressed);
			camera.PictureCaptured += new GHIE.Camera.PictureCapturedEventHandler(PictureCaptured);

			InitialiseTimers();
		}

	}
}