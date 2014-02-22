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

		public MessageReponse VerifySDCard() {
			if (sdCard.IsCardInserted && sdCard.IsCardMounted) {
				return new MessageReponse(true, "SD card verified");
			}
			if (sdCard.IsCardInserted && !sdCard.IsCardMounted) {
				Debug.Print("SD card inserted, not mounted\nMounting...");
				try {
					sdCard.MountSDCard();
					return new MessageReponse(true, "Mounted");
				} catch {
					return new MessageReponse(false, "Failed to mount");
				}
			}
			return new MessageReponse(false, "SD card not found");
		}

		public GT.StorageDevice GetStorage() {
			return sdCard.GetStorageDevice();
		}

		public bool VerifyDirectory(string path) {
			if (!VerifySDCard().GetResponse()) {
				return false;
			}

			string[] dirs = GetStorage().ListRootDirectorySubdirectories();

			foreach (string dir in dirs) {
				if (dir.Equals(path)) {
					return true;
				}
			}

			GetStorage().CreateDirectory(path);
			return true;
		}
	}
}
