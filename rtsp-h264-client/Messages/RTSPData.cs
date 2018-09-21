﻿namespace Mictlanix.DotNet.Rtsp.Messages {
	/// <summary>
	/// Message wich represent data. ($ limited message)
	/// </summary>
	public class RtspData : RtspChunk {
		static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger ();

		/// <summary>
		/// Logs the message to debug.
		/// </summary>
		public override void LogMessage (NLog.LogLevel aLevel)
		{
			// Default value to debug
			if (aLevel == null)
				aLevel = NLog.LogLevel.Debug;
			// if the level is not logged directly return
			if (!_logger.IsEnabled (aLevel))
				return;
			_logger.Log (aLevel, "Data message");
			if (Data == null)
				_logger.Log (aLevel, "Data : null");
			else
				_logger.Log (aLevel, "Data length :-{0}-", Data.Length);
		}

		public int Channel { get; set; }

		/// <summary>
		/// Clones this instance.
		/// <remarks>Listner is not cloned</remarks>
		/// </summary>
		/// <returns>a clone of this instance</returns>
		public override object Clone ()
		{
			var result = new RtspData {
				Channel = Channel
			};

			if (this.Data != null)
				result.Data = this.Data.Clone () as byte [];
			
			result.SourcePort = this.SourcePort;

			return result;
		}
	}
}
