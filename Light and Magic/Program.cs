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

		//Calculators
		Calc ledCalculator;
		Calc sunCalculator;
		Calc incCalculator;

		//Timers 
		GT.Timer pollingTimer;
		int pollingRatePosition;
		int[] pollingRates = { 20000, 300000, 600000, 1200000 };

		GT.Timer delayTimer;
		int delayTiming = 4000;

		GT.Timer heartbeat;
		int heartbeatRate = 30000;
		bool tickTock = true;

		GT.Timer displayTimeout;
		int displayTimeoutValue = 10000;

		GT.Timer startSessionRecordTimer;
		GT.Timer stopSessionRecordTimer;

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

		private void displayTimedOut(GT.Timer timer) {
			if (displayModule.BBackLightOn) {
				displayModule.SetBacklight(false);
			} else {
				displayModule.SetBacklight(true);
			}
		}

		private void pollingTick(GT.Timer timer) {
			Debug.Print("tick");
			GHIE.ColorSense.ColorChannels channel;
			double light;
			double luminance;
			uint red, green, blue;
            string interpretedSource;

			light = GetLightIntensitiy();

			channel = colorSense.ReadColorChannels();
			red = channel.Red;
			green = channel.Green;
			blue = channel.Blue;
			luminance = CalculateLuminance(red, green, blue);

            interpretedSource = CalculateInterpretedSource(light, luminance);

			Hashtable dataToSend = new Hashtable();
			dataToSend.Add("Red", red);
			dataToSend.Add("Green", green);
			dataToSend.Add("Blue", blue);
			dataToSend.Add("Intensity", light);
			dataToSend.Add("Luminosity", luminance);
            dataToSend.Add("interpreted_source", interpretedSource);
			wifi.SendData(dataToSend);

			if (isRecording) {
				sdCard.WriteLineToFile(sessionDate, "Records.csv", DateTime.Now.ToString("u") + "," +
						light + "," +
						luminance + "," +
						red.ToString() + "," +
						green.ToString() + "," +
						blue.ToString() + "," + 
                        interpretedSource);

				if (enableImageCapture) {
					camera.TakePicture();
				}
			}
		}

		private void startSessionTick(GT.Timer timer) {
			StopRecording();
			StartRecording();
		}

		private void stopSessionTick(GT.Timer timer) {
			StopRecording();
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
					sdCard.WriteLineToFile(sessionDate, "Records.csv", "Time, Percent, Luminance, Red, Green, Blue, Estimated");
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

        #region Light Source Interpretation

        private void CreateCalculators() {
            CreateIncCalculator();
            CreateLEDCalculator();
            CreateSunCalculator();
        }

        private void CreateIncCalculator() {
            double[] incIntensity = { 43, 61, 74, 84 };
            double[] incLuminosity = { 41, 94, 225, 629 };
            incCalculator = new Calc(incIntensity, incLuminosity, "Incandescent");
        }

        private void CreateSunCalculator() {
            double[] sunIntensity = { 43, 60, 74, 84 };
            double[] sunLuminosity = { 13, 35, 78, 218 };
            sunCalculator = new Calc(sunIntensity, sunLuminosity, "Sun");
        }

        private void CreateLEDCalculator() {
            double[] ledIntensity = { 44, 60, 75, 86 };
            double[] ledLuminosity = { 6, 14, 40, 139 };
            ledCalculator = new Calc(ledIntensity, ledLuminosity, "LED");
        }

        private string CalculateInterpretedSource(double intensity, double luminosity) {
            int totalMatches = 0;
            string interpretedSource = "Unknown";

            if (luminosity < 10) {
                interpretedSource = "Off";
            }

            double ledLuminosity = ledCalculator.getLuminosity((int)intensity);
            if ((ledLuminosity * 1.3) > luminosity && luminosity > (ledLuminosity * 0.7)) {
                interpretedSource = "LED";
                totalMatches++;
            }

            double sunLuminosity = sunCalculator.getLuminosity((int)intensity);
            if ((sunLuminosity * 1.3) > luminosity && luminosity > (sunLuminosity * 0.7)) {
                interpretedSource = "Off";
                totalMatches++;
            }

            double incLuminosity = incCalculator.getLuminosity((int)intensity);
            if ((incLuminosity * 1.3) > luminosity && luminosity > (sunLuminosity * 0.7)) {
                interpretedSource = "Incandescent";
                totalMatches++;
            }

            if (totalMatches > 1) {
                interpretedSource = "Multiple";
            }

            return interpretedSource;
        }

        #endregion

        #region Touch

        void InitTouch() {
			window = display.GetWindow();
			window.TouchUp += new Microsoft.SPOT.Input.TouchEventHandler(TouchUp);
			displayModule.SetBacklight(false);
		}

		public void TouchUp(object sender, Microsoft.SPOT.Input.TouchEventArgs e) {
			if (!displayModule.BBackLightOn) {
				displayModule.SetBacklight(true);
				displayTimeout.Start();
				return;
			}

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

			if (config.Contains("session")) {
				Hashtable sessionDetails = config["session"] as Hashtable;
				if (sessionDetails.Contains("start_date") && sessionDetails.Contains("end_date")) {
					TimeSpan span;
					DateTime now, startDate, stopDate;

					now = DateTime.Now;
					startDate = DateTimeExtensions.FromIso8601(sessionDetails["start_date"].ToString());
					stopDate = DateTimeExtensions.FromIso8601(sessionDetails["end_date"].ToString());

					if (stopDate > now) {
						Display.SendMessage("Session end is after now");
						Debug.Print("Session end is after now");
					} else if (startDate > stopDate) {
						Display.SendMessage("Start date is AFTER end date");
						Debug.Print("Start date is AFTER end date");
					} else {
						span = startDate - now;
						startSessionRecordTimer = new GT.Timer(span, GT.Timer.BehaviorType.RunOnce);
						startSessionRecordTimer.Tick += new GT.Timer.TickEventHandler(startSessionTick);

						span = stopDate - now;
						stopSessionRecordTimer = new GT.Timer(span, GT.Timer.BehaviorType.RunOnce);
						stopSessionRecordTimer.Tick += new GT.Timer.TickEventHandler(stopSessionTick);

						startSessionRecordTimer.Start();
						stopSessionRecordTimer.Start();
					}
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

			displayTimeout = new GT.Timer(displayTimeoutValue, GT.Timer.BehaviorType.RunOnce);
			displayTimeout.Tick += new GT.Timer.TickEventHandler(displayTimedOut);
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
            CreateCalculators();
            isRecording = false;

			InitialiseModules();

			SetupFromConfig();

			InitTouch();

			button.ButtonPressed += new GHIE.Button.ButtonEventHandler(buttonPressed);
			camera.PictureCaptured += new GHIE.Camera.PictureCapturedEventHandler(PictureCaptured);

			InitialiseTimers();
		}

	}
}