namespace Mictlanix.DotNet.Rtsp.Sdp {
	public class FmtpAttribute : Attribute {
		public const string NAME = "fmtp";

		public override string Key {
			get {
				return NAME;
			}
		}

		public override string Value {
			get {
				return string.Format ("{0} {1}", PayloadNumber, FormatParameter);
			}
			protected set {
				ParseValue (value);
			}
		}

		public int PayloadNumber { get; set; }

		// temporary aatibute to store remaning data not parsed
		public string FormatParameter { get; set; }

		protected override void ParseValue (string value)
		{
			var parts = value.Split (new char [] { ' ' }, 2);

			if (int.TryParse (parts [0], out int payloadNumber)) {
				this.PayloadNumber = payloadNumber;
			}

			if (parts.Length > 1) {
				FormatParameter = parts [1];
			}
		}
	}
}
