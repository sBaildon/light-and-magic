using System;
using Microsoft.SPOT;

namespace Light_and_Magic {
	class JSON {
		public static string JSONEncode(string redval, string greenval, string blueval, string intval, string lumval) {
			return "{\"version\":\"1.0.0\",\"datastreams\" : [ {\"id\" : \"Red\",\"current_value\" : \"" + redval + "\"},{ \"id\" : \"Green\",\"current_value\" : \"" + greenval + "\"},{\"id\" : \"Blue\",\"current_value\" : \"" + blueval + "\"},{\"id\": \"Intensity\",\"current_value\" : \"" + intval + "\"},{\"id\": \"Luminosity\",\"current_value\" : \"" + lumval + "\"}]}";
		}
	}
}
