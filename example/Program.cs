using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using Mictlanix.DotNet.Rtsp;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RtspClientExample {
	class Program {
		static void Main (string [] args)
		{
			var shooter = new Program ();
			var tasks = new List<Task> {
				//// VStarcam C7824WIP (dev)
				//shooter.Snapshot("VStarcam C7824WIP", "admin", "888888", "rtsp://192.168.100.16/tcp/av0_0"),

				//// Siqura PD1103Z2-E
				//shooter.Snapshot("Siqura PD1103Z2-E", "admin", "root1234", "rtsp://10.1.254.125/VideoInput/1/h264/1"),

				//// Siqura HSD626
				//shooter.Snapshot ("Siqura HSD626", "Admin", "1234", "rtsp://10.1.254.130/VideoInput/1/h264/1"),

				//// Siqura HSD820
				//shooter.Snapshot("Siqura HSD820", "admin", "@root1234", "rtsp://10.1.254.128/VideoInput/1/h264/1"),

				//// Samsung SNB-6004
				//shooter.Snapshot("Samsung SNB-6004", "admin", "@root1234", "rtsp://10.1.254.126/profile2/media.smp"),

				//// Samsung SNP-5321H
				//shooter.Snapshot("Samsung SNP-5321H", "admin", "@root123", "rtsp://10.1.254.127/onvif/profile2/media.smp"),

				//Flir HD-XT
				//shooter.Snapshot("Flir HD-XT", "Admin", "1234", "rtsp://10.10.128.62/VideoInput/1/h264/1")
			};

			if (args.Length == 3) {
				tasks.Add (shooter.Snapshot ("cam_test", args [1], args [2], args [0]));
			}

			Task.WaitAll (tasks.ToArray ());
		}

		public async Task Snapshot (string name, string username, string password, string url)
		{
			//Stream fs_v = null;
			MemoryStream fs_v = null;
			var client = new RTSPClient ();
			var ts = DateTime.MaxValue;

			client.ParameterSetsReceived += async (byte [] sps, byte [] pps) => {
				if (fs_v == null) {
					fs_v = new MemoryStream (4 * 1024);
					//fs_v = new FileStream ($"{name.Replace (" ", "_")}.h264", FileMode.Create);
				}

				if (fs_v != null) {
					await fs_v.WriteAsync (new byte [] { 0x00, 0x00, 0x00, 0x01 }, 0, 4);  // Write Start Code
					await fs_v.WriteAsync (sps, 0, sps.Length);
					await fs_v.WriteAsync (new byte [] { 0x00, 0x00, 0x00, 0x01 }, 0, 4);  // Write Start Code
					await fs_v.WriteAsync (pps, 0, pps.Length);
				}
			};

			client.FrameReceived += async (List<byte []> nal_units) => {
				if (fs_v != null) {
					foreach (byte [] nal_unit in nal_units) {
						await fs_v.WriteAsync (new byte [] { 0x00, 0x00, 0x00, 0x01 }, 0, 4);      // Write Start Code
						await fs_v.WriteAsync (nal_unit, 0, nal_unit.Length);                      // Write NAL
					}

					await fs_v.FlushAsync ();

					if (DateTime.Now > ts)
						client.Stop ();
				}
			};

			try {
				// Connect to RTSP Server
				Console.WriteLine ($"Connecting {url}...");

				client.Timeout = 3000;
				client.Connect (url, username, password);

				ts = DateTime.Now.AddSeconds (5); // time to capture video

				while (!client.IsStreamingFinished ()) {
					await Task.Delay (100);
				}

				if (fs_v != null) {
					File.WriteAllBytes ($"{name.Replace (" ", "_")}.h264", fs_v.ToArray ());
					await ExecConverter (fs_v, $"{name.Replace (" ", "_")}.jpg", 1280, 720);
					fs_v.Close ();
					fs_v.Dispose ();
				} else {
					Console.WriteLine ($"Empty Streaming");
				}
			} catch (Exception ex) {
				Console.WriteLine ($"Message: {ex.Message}");
				Console.WriteLine ($"StackTrace: {ex.StackTrace}");
			}
		}

		async Task<int> ExecConverter (Stream stream, string filename, int width, int height)
		{
			var tcs = new TaskCompletionSource<int> ();
			var process = new Process {
				EnableRaisingEvents = true
			};
			var startInfo = new ProcessStartInfo {
				WindowStyle = ProcessWindowStyle.Hidden,
				FileName = @"ffmpeg",
				Arguments = $"-f h264 -i - -f image2 -vframes 1 -s {width}x{height} -y \"{filename}\"",
				//Arguments = $"-loglevel quiet -f h264 -i - -f image2 -vframes 1 -s {width}x{height} -y \"{filename}\"",
				RedirectStandardError = true,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			process.Exited += (sender, args) => {
				tcs.SetResult (process.ExitCode);
				process.Dispose ();
			};

			process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
				Console.WriteLine (e.Data);
			};

			process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
				Console.WriteLine (e.Data);
			};

			process.StartInfo = startInfo;
			process.Start ();

			process.BeginErrorReadLine ();
			process.BeginOutputReadLine ();

			var standard_input = process.StandardInput.BaseStream;

			try {
				stream.Position = 0;
				await stream.CopyToAsync (standard_input);
			} catch (IOException) {

			} finally {
				standard_input.Close ();
			}

			return await tcs.Task;
		}
	}
}
