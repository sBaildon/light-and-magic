using System;
using Microsoft.SPOT;

namespace Light_and_Magic {
	class MessageReponse {
		bool resp;
		string info;

		public MessageReponse(bool resp, string info) {
			this.resp = resp;
			this.info = info;
		}

		public bool GetResponse() {
			return resp;
		}

		public string GetInfo() {
			return info;
		}
	}
}
