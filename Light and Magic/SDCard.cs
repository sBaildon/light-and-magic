using System;
using System.IO;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Touch;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using GHIE = Gadgeteer.Modules.GHIElectronics;
using GHINet = GHI.Premium.Net;

namespace Light_and_Magic {
	class SDCard {

		GHIE.SDCard sdCard;

		public SDCard(GHIE.SDCard card) {
			sdCard = card;
		}

		public bool VerifySDCard() {
			if (sdCard.IsCardInserted && sdCard.IsCardMounted) {
				return true;
			}
			if (sdCard.IsCardInserted && !sdCard.IsCardMounted) {
				try {
					sdCard.MountSDCard();
					return true;
				} catch {
					return false;
				}
			}
			return false;
		}

		public GT.StorageDevice GetStorage() {
			return sdCard.GetStorageDevice();
		}		

		public void Unmount() {
			sdCard.UnmountSDCard();
		}

		public bool VerifyDirectory(string path) {
			GT.StorageDevice storage;
			storage = GetStorage();

			string[] dirs = storage.ListRootDirectorySubdirectories();

			foreach (string dir in dirs) {
				if (dir.Equals(path)) return true;
			}

			return false;
		}

		public void CreateDirectory(string path) {
			GT.StorageDevice storage;
			storage = GetStorage();

			storage.CreateDirectory(path);
		}

		public bool VerifyFile(string path, string fileName) {
			GT.StorageDevice storage;
			storage = GetStorage();

			string[] files = storage.ListFiles(path);

			foreach (string file in files) {
				if (file.Equals(path + "\\" + fileName)) return true;
			}

			return false;
		}

		public void CreateFile(string path, string fileName) {
			GT.StorageDevice storage;
			storage = GetStorage();

			Stream stream;
			stream = storage.Open(path + "\\" + fileName, FileMode.Create, FileAccess.ReadWrite);
			stream.Close();
		}

		public string ReadFile(string fileName) {
			Stream stream;
			GT.StorageDevice storage;

			storage = GetStorage();
			stream = storage.OpenRead(fileName);

			byte[] data = new byte[stream.Length];
			stream.Read(data, 0, (data.Length));
			stream.Close();

			string fileContents = new string(System.Text.Encoding.UTF8.GetChars(data));

			return fileContents;
		}

		public void WriteLineToFile(string path, string fileName, string data) {
			Stream stream;
			GT.StorageDevice storage;
			StreamWriter writer;

			storage = GetStorage();
			stream = storage.Open(path + "\\" + fileName, FileMode.Append, FileAccess.Write);
			writer = new StreamWriter(stream);

			writer.WriteLine(data);

			writer.Close();
			stream.Close();
		}


	}
}
