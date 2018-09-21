using System;
using System.Diagnostics.Contracts;

namespace Mictlanix.DotNet.Rtsp.Sdp {
	public class EncriptionKey {
		public EncriptionKey (string p)
		{
		}

		public static EncriptionKey ParseInvariant (string value)
		{
			if (value == null)
				throw new ArgumentNullException (nameof (value));

			Contract.EndContractBlock ();

			throw new NotImplementedException ();
		}
	}
}
