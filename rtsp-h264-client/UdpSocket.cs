using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Mictlanix.DotNet.Rtsp {
	public class UDPSocket {
		UdpClient data_socket = null;
		UdpClient control_socket = null;

		Thread data_read_thread = null;
		Thread control_read_thread = null;

		public int data_port = 50000;
		public int control_port = 50001;

		bool is_multicast = false;
		IPAddress data_mcast_addr;
		IPAddress control_mcast_addr;

		/// <summary>
		/// Initializes a new instance of the <see cref="UDPSocket"/> class.
		/// Creates two new UDP sockets using the start and end Port range
		/// </summary>
		public UDPSocket (int startPort, int endPort)
		{
			is_multicast = false;

			// open a pair of UDP sockets - one for data (video or audio) and one for the status channel (RTCP messages)
			data_port = startPort;
			control_port = startPort + 1;

			bool ok = false;
			while (ok == false && (control_port < endPort)) {
				// Video/Audio port must be odd and command even (next one)
				try {
					data_socket = new UdpClient (data_port);
					control_socket = new UdpClient (control_port);
					ok = true;
				} catch (SocketException) {
					// Fail to allocate port, try again
					if (data_socket != null)
						data_socket.Close ();
					if (control_socket != null)
						control_socket.Close ();

					// try next data or control port
					data_port += 2;
					control_port += 2;
				}
			}

			data_socket.Client.ReceiveBufferSize = 100 * 1024;

			control_socket.Client.DontFragment = false;
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="UDPSocket"/> class.
		/// Used with Multicast mode with the Multicast Address and Port
		/// </summary>
		public UDPSocket (String dataMulticastAddress, int dataMulticastPort, String controlMulticastAddress, int controlMulticastPort)
		{
			is_multicast = true;

			// open a pair of UDP sockets - one for data (video or audio) and one for the status channel (RTCP messages)
			this.data_port = dataMulticastPort;
			this.control_port = controlMulticastPort;

			try {
				IPEndPoint data_ep = new IPEndPoint (IPAddress.Any, data_port);
				IPEndPoint control_ep = new IPEndPoint (IPAddress.Any, control_port);

				data_mcast_addr = IPAddress.Parse (dataMulticastAddress);
				control_mcast_addr = IPAddress.Parse (controlMulticastAddress);

				data_socket = new UdpClient ();
				data_socket.Client.Bind (data_ep);
				data_socket.JoinMulticastGroup (data_mcast_addr);

				control_socket = new UdpClient ();
				control_socket.Client.Bind (control_ep);
				control_socket.JoinMulticastGroup (control_mcast_addr);


				data_socket.Client.ReceiveBufferSize = 100 * 1024;

				control_socket.Client.DontFragment = false;
			} catch (SocketException) {
				// Fail to allocate port, try again
				if (data_socket != null)
					data_socket.Close ();
				
				if (control_socket != null)
					control_socket.Close ();

				return;
			}
		}

		/// <summary>
		/// Starts this instance.
		/// </summary>
		public void Start ()
		{
			if (data_socket == null || control_socket == null) {
				throw new InvalidOperationException ("UDP Forwader host was not initialized, can't continue");
			}

			if (data_read_thread != null) {
				throw new InvalidOperationException ("Forwarder was stopped, can't restart it");
			}

			data_read_thread = new Thread (() => DoWorkerJob (data_socket, data_port));
			data_read_thread.Name = "DataPort " + data_port;
			data_read_thread.Start ();

			control_read_thread = new Thread (() => DoWorkerJob (control_socket, control_port));
			control_read_thread.Name = "ControlPort " + control_port;
			control_read_thread.Start ();
		}

		/// <summary>
		/// Stops this instance.
		/// </summary>
		public void Stop ()
		{
			if (is_multicast) {
				// leave the multicast groups
				data_socket.DropMulticastGroup (data_mcast_addr);
				control_socket.DropMulticastGroup (control_mcast_addr);
			}

			data_socket.Close ();
			control_socket.Close ();
		}

		/// <summary>
		/// Occurs when message is received.
		/// </summary>
		public event EventHandler<Mictlanix.DotNet.Rtsp.RtspChunkEventArgs> DataReceived;

		/// <summary>
		/// Raises the <see cref="E:DataReceived"/> event.
		/// </summary>
		/// <param name="rtspChunkEventArgs">The <see cref="Mictlanix.DotNet.Rtsp.RtspChunkEventArgs"/> instance containing the event data.</param>
		protected void OnDataReceived (Mictlanix.DotNet.Rtsp.RtspChunkEventArgs rtspChunkEventArgs)
		{
			DataReceived?.Invoke (this, rtspChunkEventArgs);
		}

		/// <summary>
		/// Does the video job.
		/// </summary>
		void DoWorkerJob(UdpClient socket, int dataPort)
		{
			IPEndPoint ipEndPoint = new IPEndPoint (IPAddress.Any, dataPort);

			try {
				// loop until we get an exception eg the socket closed
				while (true) {
					byte[] frame = socket.Receive (ref ipEndPoint);

					// We have an RTP frame.
					// Fire the DataReceived event with 'frame'
					Console.WriteLine ("Received RTP data on port " + dataPort);

					Mictlanix.DotNet.Rtsp.Messages.RtspChunk currentMessage = new Mictlanix.DotNet.Rtsp.Messages.RtspData ();
					// aMessage.SourcePort = ??
					currentMessage.Data = frame;
					((Mictlanix.DotNet.Rtsp.Messages.RtspData)currentMessage).Channel = dataPort;

					OnDataReceived (new Mictlanix.DotNet.Rtsp.RtspChunkEventArgs (currentMessage));

				}
			} catch (ObjectDisposedException) {
			} catch (SocketException) {
			}
		}
	}
}
