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

namespace Light_and_Magic {

	public partial class Program {

		bool isRecording;
		long minutesLogged;

		Stream stream;
		StreamWriter writer;
		GT.StorageDevice storage;

		#region Lights

		void setLED(bool state) {
			Mainboard.SetDebugLED(state);
		}

		#endregion

		#region Buttons

		void buttonPressed(Button sender, Button.ButtonState state) {
			if (isRecording) {
				StopRecording();
			} else {
				StartRecording();
			}
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
				writer.WriteLine(minutesLogged.ToString() + "," + 
						light + "," + 
						luma.ToString() + "," +
						red.ToString() + "," + 
						green.ToString() + "," + 
						blue.ToString());
				camera.TakePicture();
				minutesLogged = minutesLogged + 10;
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

		void ProgramStarted() {
			minutesLogged = 0;
			isRecording = false;

			button.ButtonPressed += new Button.ButtonEventHandler(buttonPressed);

			camera.PictureCaptured += new Camera.PictureCapturedEventHandler(pictureCaptured);


			int intervalInSeconds = 5;
			int intervalInMillis = intervalInSeconds * 1000;

			int intervalInMinutes = 1;
			//int intervalInMillis = intervalInMinutes * 60000;

			GT.Timer timer = new GT.Timer(intervalInMillis);
			timer.Tick += new GT.Timer.TickEventHandler(timerTick);
			timer.Start();
		}

	}
}