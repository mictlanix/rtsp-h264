using System.Collections.Generic;
using System.Linq;

namespace Mictlanix.DotNet.Rtsp.Sdp {
	public class Media {
		string mediaString;

		public Media (string mediaString)
		{
			// Example is   'video 0 RTP/AVP 26;
			this.mediaString = mediaString;

			var parts = mediaString.Split (new char [] { ' ' }, 4);

			if (parts.Any ()) {
				if (parts [0].Equals ("video")) MediaType = MediaTypes.video;
				else if (parts [0].Equals ("audio")) MediaType = MediaTypes.audio;
				else if (parts [0].Equals ("text")) MediaType = MediaTypes.text;
				else if (parts [0].Equals ("application")) MediaType = MediaTypes.application;
				else if (parts [0].Equals ("message")) MediaType = MediaTypes.message;
				else MediaType = MediaTypes.unknown; // standard does allow for future types to be defined
			}

			if (parts.Count () >= 4) {
				if (int.TryParse (parts [3], out int pt)) {
					PayloadType = pt;
				} else {
					PayloadType = 0;
				}
			}
		}

		// RFC4566 Media Types
		public enum MediaTypes { video, audio, text, application, message, unknown };

		public Connection Connection { get; set; }

		public Bandwidth Bandwidth { get; set; }

		public EncriptionKey EncriptionKey { get; set; }

		public MediaTypes MediaType { get; set; }

		public int PayloadType { get; set; }

		readonly List<Attribute> attributs = new List<Attribute> ();

		public IList<Attribute> Attributs {
			get {
				return attributs;
			}
		}
	}
}
