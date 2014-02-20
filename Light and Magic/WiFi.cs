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
		static WiFiRS9110 wifi;

		static string ssid;
		static string passphrase;

		static string ENDPOINT;
		static string API_KEY;

		static public void Init(WiFi_RS21 module, string ssid, string passphrase) {
			wifi = module.Interface;

			if (!wifi.IsOpen) {
				wifi.Open();
			}

			wifi.NetworkInterface.EnableDhcp();
			NetworkInterfaceExtension.AssignNetworkingStackTo(wifi);

			wifi.WirelessConnectivityChanged +=
				new WiFiRS9110.WirelessConnectivityChangedEventHandler(Interface_WirelessConnectivityChanged);
			wifi.NetworkAddressChanged +=
				new NetworkInterfaceExtension.NetworkAddressChangedEventHandler(Interface_NetworkAddressChanged);

			Connect(ssid, passphrase);

		}

		static private bool Connect(string ssid, string passphrase) {
			WiFiNetworkInfo[] ScanResults = wifi.Scan();
			foreach (WiFiNetworkInfo info in ScanResults) {
				Debug.Print("Found WLAN: " + info.SSID);
				if (info.SSID.ToString().Equals(ssid)) {
					wifi.Join(info, passphrase);
					return true;
				}
			}
			return false;
		}
		

		static public void SendData(Hashtable table) {
			HttpRequest request;
			PUTContent content;

			content = PUTContent.CreateTextBasedContent(JSON.Encode(table));
			request = HttpHelper.CreateHttpPutRequest(ENDPOINT + ".json", content, "application/json");
			request.AddHeaderField("X-ApiKey", API_KEY);
			request.ResponseReceived += new HttpRequest.ResponseHandler(ResponseReceived);
			request.SendRequest();
		}

		static private void ResponseReceived(HttpRequest sender, HttpResponse response) {
			Display.SendMessage(response.StatusCode);
		}

		static private void Interface_NetworkAddressChanged(object sender, EventArgs e) {
			Debug.Print("WiFi address changed to: " + wifi.NetworkInterface.IPAddress);
		}

		static private void Interface_WirelessConnectivityChanged(object sender, WiFiRS9110.WirelessConnectivityEventArgs e) {
			Debug.Print("WiFi connectivity changed, new SSID: " + e.NetworkInformation.SSID.ToString());
			Hashtable data = new Hashtable();
			data.Add("wifi-ssid", e.NetworkInformation.SSID.ToString());
			SendData(data);
		}

		static private void Wifi_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state) {
			Debug.Print("Connection down");
		}

		static public void UpdateServerInformation(string server, string apiKey) {
			ENDPOINT = server;
			API_KEY = apiKey;			
		}

	}
}
