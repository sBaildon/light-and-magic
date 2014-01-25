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

		// Book keeping
		bool isRecording;
		long minutesLogged;

		// Used for reading/writing to the SD card
		Stream stream;
		StreamWriter writer;
		GT.StorageDevice storage;

		// Values for the display
		Window window;
		Canvas canvas;
		Font baseFont;
		Text txtMsg;

		// Wifi
		WiFiRS9110 wifiNetwork;

		#region Lights

		void setLED(bool state) {
			Mainboard.SetDebugLED(state);
		}

		#endregion

		#region Camera

		void pictureCaptured(Camera camera, GT.Picture picture) {
			Debug.Print("Image captured");
			storage.WriteFile("picture.bmp", picture.PictureData);
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

		#endregion

		#region Program

		void StartRecording() {
			if (verifySDCard()) {
				storage = sdCard.GetStorageDevice();
				stream = storage.Open(GetFileName(), FileMode.Create, FileAccess.Write);
				writer = new StreamWriter(stream);
				writer.WriteLine("Min, Percent, Luma, Red, Green, Blue");
				isRecording = true;
				setLED(true);
				Debug.Print("Started recording\n");
			} else {
				Debug.Print("Failed to start recording");
			}
		}

		void StopRecording() {
			if (verifySDCard()) {
				writer.Close();
				stream.Close();
				sdCard.UnmountSDCard();
				storage = null;
				isRecording = false;
				minutesLogged = 0;
				setLED(false);
				Debug.Print("Stopped recording\n");
			}
		}

		string GetFileName() {
			Random rand = new Random();
			return "day" + rand.Next(200).ToString() + ".csv";
		}

		#endregion

		#region Display

		void SetupDisplay() {
			canvas = new Canvas();
			window = display.WPFWindow;
			window.Child = canvas;
			baseFont = Resources.GetFont(Resources.FontResources.NinaB);

			txtMsg = new Text(baseFont, "Ready");
			canvas.Children.Add(txtMsg);
		}

		void SendMessageToDisplay(string message) {
			txtMsg.TextContent = message;
		}

		#endregion

		#region Wifi

		void InitWifi(string ssid, string passphrase) {
			if (!wifiNetwork.IsOpen) {
				wifiNetwork.Open();
			}
			wifiNetwork.NetworkInterface.EnableDhcp();
			NetworkInterfaceExtension.AssignNetworkingStackTo(wifiNetwork);

			wifiNetwork.WirelessConnectivityChanged +=
				new WiFiRS9110.WirelessConnectivityChangedEventHandler(Interface_WirelessConnectivityChanged);
			wifiNetwork.NetworkAddressChanged +=
				new NetworkInterfaceExtension.NetworkAddressChangedEventHandler(Interface_NetworkAddressChanged);

			WiFiNetworkInfo[] ScanResults = wifiNetwork.Scan();
			foreach (WiFiNetworkInfo info in ScanResults) {
				Debug.Print("Found WLAN: " + info.SSID);
				if (info.SSID.ToString().Equals(ssid)) {
					wifiNetwork.Join(info, passphrase);
					break;
				}
			}
		}

		void Interface_NetworkAddressChanged(object sender, EventArgs e) {
			Debug.Print("WiFi address changed to: " + wifiNetwork.NetworkInterface.IPAddress);
		}

		void Interface_WirelessConnectivityChanged(object sender, WiFiRS9110.WirelessConnectivityEventArgs e) {
			Debug.Print("WiFi connectivity changed, new SSID: " + e.NetworkInformation.SSID.ToString());
		}

		#endregion

		void ProgramStarted() {
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

			//SetupDisplay();
			//SendMessageToDisplay("Ready");

			//wifiNetwork = wifi.Interface;
			//InitWifi("WIFI17", "rilasaci");

			GT.Timer timer = new GT.Timer(intervalInMillis);
			timer.Tick += new GT.Timer.TickEventHandler(timerTick);
			timer.Start();
		}

	}
}