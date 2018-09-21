using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using Mictlanix.DotNet.Rtsp;

namespace RtspClientExample {
	class Program {
		static void Main (string [] args)
		{
			var shooter = new Program ();

			// VStarcam C7824WIP (dev)
			shooter.Snapshot ("admin", "888888", "rtsp://192.168.100.16/tcp/av0_0");

			//// Siqura PD1103Z2-E
			//shooter.Snapshot ("admin", "root1234", "rtsp://10.1.254.125/VideoInput/1/h264/1");

			//// Siqura HSD626
			//shooter.Snapshot ("Admin", "1234", "rtsp://10.1.254.130/VideoInput/1/h264/1");

			//// Siqura HSD820
			//shooter.Snapshot ("admin", "@root1234", "rtsp://10.1.254.128/VideoInput/1/h264/1");

			//// Samsung SNB-6004
			//shooter.Snapshot ("admin", "@root1234", "rtsp://10.1.254.126/profile2/media.smp");

			//// Samsung SNP-5321H
			//shooter.Snapshot ("admin", "@root123", "rtsp://10.1.254.127/onvif/profile2/media.smp");

			// Flir HD-XT
			//shooter.Snapshot ("Admin", "1234", "rtsp://10.10.128.62/VideoInput/1/h264/1");
		}

		public void Snapshot (string username, string password, string url)
		{
			MemoryStream fs_v = null;
			//var url = "rtsp://187.178.23.206/tcp/av0_0";
			var client = new RTSPClient ();
			int count = 30;

			client.ParameterSetsReceived += (byte [] sps, byte [] pps) => {
				if (fs_v == null) {
					fs_v = new MemoryStream (4 * 1024);
				}

				if (fs_v != null) {
					fs_v.Write (new byte [] { 0x00, 0x00, 0x00, 0x01 }, 0, 4);  // Write Start Code
					fs_v.Write (sps, 0, sps.Length);
					fs_v.Write (new byte [] { 0x00, 0x00, 0x00, 0x01 }, 0, 4);  // Write Start Code
					fs_v.Write (pps, 0, pps.Length);
				}
			};

			client.FrameReceived += (List<byte []> nal_units) => {
				if (fs_v != null) {
					foreach (byte [] nal_unit in nal_units) {
						fs_v.Write (new byte [] { 0x00, 0x00, 0x00, 0x01 }, 0, 4);  	// Write Start Code
						fs_v.Write (nal_unit, 0, nal_unit.Length);                 	// Write NAL
					}

					if(count <= 0)
						client.Stop ();

					count--;
				}
			};

			// Connect to RTSP Server
			Console.WriteLine ("Connecting");

			client.Timeout = 3000;
			client.Connect (url, username, password);

			// Wait for user to terminate programme
			// Check for null which is returned when running under some IDEs
			// OR wait for the Streaming to Finish - eg an error on the RTSP socket
			while (!client.IsStreamingFinished ()) {
				// Avoid maxing out CPU
				Thread.Sleep (100);
			}

			if (fs_v != null) {
				//File.WriteAllBytes ("video.h264", fs_v.ToArray ());
				ExecConverter (fs_v, $"frame_{DateTime.Now.ToString ("yyyyMMdd_HHmmss")}.jpg", 1280, 720);
			}

			Console.WriteLine ("Finished");
		}

		void ExecConverter (Stream stream, string filename, int width, int height)
		{
			System.Diagnostics.Process process = new System.Diagnostics.Process ();
			System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo ();
			startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			startInfo.FileName = @"/usr/local/bin/ffmpeg";
			startInfo.Arguments = $"-i - -f image2 -vframes 1 -s {width}x{height} -y {filename}";
			//startInfo.Arguments = $"-i - -frames:v 1 -f image2 -t 1 -r 1 -y frame.jpg";
			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardInput = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.UseShellExecute = false;
			startInfo.CreateNoWindow = true;
			process.StartInfo = startInfo;
			process.Start ();

			//Read (process.StandardOutput);
			//Read (process.StandardError);

			stream.Position = 0;
			stream.CopyTo (process.StandardInput.BaseStream);
			process.StandardInput.Close ();

			process.WaitForExit (3000);
			Console.WriteLine ("Converter Finished");
		}

		void Read (StreamReader reader)
		{
			new Thread (() =>
			{
				while (true) {
					int current;
					while ((current = reader.Read ()) >= 0)
						Console.Write ((char)current);
				}
			}).Start ();
		}

	}
}
