namespace Mictlanix.DotNet.Rtsp.Messages {
	public class RtspRequestDescribe : RtspRequest {
		// constructor
		public RtspRequestDescribe ()
		{
			Command = "DESCRIBE * RTSP/1.0";
		}

		public void AddAccept (string newAccept)
		{
			string actualAccept = string.Empty;

			if (Headers.ContainsKey (RtspHeaderNames.Accept))
				actualAccept = Headers [RtspHeaderNames.Accept] + ",";

			Headers [RtspHeaderNames.Accept] = actualAccept + newAccept;
		}
	}
}
