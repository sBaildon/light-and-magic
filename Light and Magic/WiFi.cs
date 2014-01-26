using System;
using System.IO;
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
using GHINet = GHI.Premium.Net;
using GHI.Premium.Net;

namespace Light_and_Magic {
	class WiFi {
		WiFiRS9110 wifi;
		HttpRequest request;
		readonly string SERVER = "http://www.ghielectronics.com";


		public void TouchUp(object sender, Microsoft.SPOT.Input.TouchEventArgs e) {

		}

		public WiFi(WiFi_RS21 wifiInput) {
			wifi = wifiInput.Interface;
		}

		public void Init(string ssid, string passphrase) {
			if (!wifi.IsOpen) {
				wifi.Open();
			}
			wifi.NetworkInterface.EnableDhcp();
			NetworkInterfaceExtension.AssignNetworkingStackTo(wifi);

			wifi.WirelessConnectivityChanged +=
				new WiFiRS9110.WirelessConnectivityChangedEventHandler(Interface_WirelessConnectivityChanged);
			wifi.NetworkAddressChanged +=
				new NetworkInterfaceExtension.NetworkAddressChangedEventHandler(Interface_NetworkAddressChanged);

			WiFiNetworkInfo[] ScanResults = wifi.Scan();
			foreach (WiFiNetworkInfo info in ScanResults) {
				Debug.Print("Found WLAN: " + info.SSID);
				if (info.SSID.ToString().Equals(ssid)) {
					wifi.Join(info, passphrase);
					HttpRequest wc = WebClient.GetFromWeb(SERVER);
					wc.ResponseReceived += new HttpRequest.ResponseHandler(wc_ResponseReceived);
					break;
				}
			}
		}

		void wc_ResponseReceived(HttpRequest sender, HttpResponse response) {
			Display.SendMessage(response.Text);
		}

		private void Interface_NetworkAddressChanged(object sender, EventArgs e) {
			Debug.Print("WiFi address changed to: " + wifi.NetworkInterface.IPAddress);
		}

		private void Interface_WirelessConnectivityChanged(object sender, WiFiRS9110.WirelessConnectivityEventArgs e) {
			Debug.Print("WiFi connectivity changed, new SSID: " + e.NetworkInformation.SSID.ToString());
		}
	}
}
