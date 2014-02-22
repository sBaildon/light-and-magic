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

		public bool VerifyDirectory(string path) {
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
