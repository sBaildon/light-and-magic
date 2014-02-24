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

		string ENDPOINT;
		string API_KEY;

		public WiFi(WiFi_RS21 module) {
			wifi = module.Interface;

			if (!wifi.IsOpen) {
				wifi.Open();
			}

			wifi.NetworkInterface.EnableDhcp();
			NetworkInterfaceExtension.AssignNetworkingStackTo(wifi);

			module.NetworkDown +=
				new GTM.Module.NetworkModule.NetworkEventHandler(Wifi_NetworkDown);
			module.NetworkUp +=
				new GTM.Module.NetworkModule.NetworkEventHandler(Wifi_NetworkUp);
			wifi.WirelessConnectivityChanged +=
				new WiFiRS9110.WirelessConnectivityChangedEventHandler(Interface_WirelessConnectivityChanged);
			wifi.NetworkAddressChanged +=
				new NetworkInterfaceExtension.NetworkAddressChangedEventHandler(Interface_NetworkAddressChanged);
		}

		public bool Connect(string ssid, string passphrase) {
			WiFiNetworkInfo[] ScanResults = wifi.Scan();
			foreach (WiFiNetworkInfo info in ScanResults) {
				Debug.Print("Found WLAN: " + info.SSID);
				if (info.SSID.ToString().Equals(ssid)) {
					wifi.Join(info, passphrase);
					Debug.Print("spinning");
					while (!wifi.IsLinkConnected) ;
					Debug.Print("no longer spinning");
					return true;
				}
			}
			return false;
		}
		

		public void SendData(Hashtable table) {
			if (wifi == null || !wifi.IsLinkConnected) {
				return;
			}

			HttpRequest request;
			PUTContent content;

			content = PUTContent.CreateTextBasedContent(JSON.Encode(table));
			request = HttpHelper.CreateHttpPutRequest(ENDPOINT + ".json", content, "application/json");
			request.AddHeaderField("X-ApiKey", API_KEY);
			Debug.Print("sending");
			request.SendRequest();
			Debug.Print("sent");
		}

		public void GetDateTime() {
			HttpRequest request;

			request = HttpHelper.CreateHttpGetRequest("http://www.timeapi.org/utc/now");
			request.ResponseReceived += new HttpRequest.ResponseHandler(DateResponse);
			request.SendRequest();
		}

		private void ResponseReceived(HttpRequest sender, HttpResponse response) {
			Display.SendMessage(response.StatusCode);
		}

		private void DateResponse(HttpRequest sender, HttpResponse response) {
			Program.UpdateDateTime(response.Text.ToString());			
		}

		private void Interface_NetworkAddressChanged(object sender, EventArgs e) {
			Debug.Print("WiFi address changed to: " + wifi.NetworkInterface.IPAddress);
		}

		private void Interface_WirelessConnectivityChanged(object sender, WiFiRS9110.WirelessConnectivityEventArgs e) {
			Debug.Print("WiFi connectivity changed, new SSID: " + e.NetworkInformation.SSID.ToString());

			Hashtable data = new Hashtable();
			data.Add("wifi-ssid", e.NetworkInformation.SSID.ToString());
			SendData(data);

			GetDateTime();
		}

		private void Wifi_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state) {
			Debug.Print("Connection down");
		}

		private void Wifi_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state) {
			Debug.Print("Connection up");
		}

		public void UpdateServerInformation(string server, string apiKey) {
			ENDPOINT = server;
			API_KEY = apiKey;			
		}

	}
}
