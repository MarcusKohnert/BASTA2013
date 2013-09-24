using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectConnector
{
	public static class KinectConnection
	{
		public static Task<KinectSensor> GetStartedKinectAsync()
		{
			return Task.Factory.StartNew(() =>
			{
				var kinect = KinectSensor.KinectSensors
					.FirstOrDefault(_ => _.Status == KinectStatus.Connected);

				if (kinect != null)
					return kinect.StartKinect();

				using (var signal = new ManualResetEventSlim())
				{
					KinectSensor.KinectSensors.StatusChanged += (o, args) =>
					{
						if (args.Status == KinectStatus.Connected)
						{
							kinect = args.Sensor;
							signal.Set();
						}
					};

					signal.Wait();
				}

				return kinect.StartKinect();
			});
		}

		public static KinectSensor GetStartedKinect()
		{
			var kinect = KinectSensor.KinectSensors
									.FirstOrDefault(_ => _.Status == KinectStatus.Connected);

			if (kinect == null)
				throw new ApplicationException("No kinect connected.");

			return kinect.StartKinect();
		}

		public static KinectSensor StartKinect(this KinectSensor kinect)
		{
			kinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
			kinect.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
			kinect.SkeletonStream.Enable();

			kinect.Start();
			return kinect;
		}
	}
}