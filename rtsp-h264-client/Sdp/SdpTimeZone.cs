using System;
using System.Diagnostics.Contracts;

namespace Mictlanix.DotNet.Rtsp.Sdp {
	public class SdpTimeZone {
		public static SdpTimeZone ParseInvariant (string value)
		{
			if (value == null)
				throw new ArgumentNullException (nameof (value));

			Contract.EndContractBlock ();

			throw new NotImplementedException ();
		}
	}
}
