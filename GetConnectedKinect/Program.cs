using System;

namespace GetConnectedKinect
{
	class Program
	{
		static void Main(string[] args)
		{
			var p = new Program();
			p.Start();

			Console.WriteLine("[ENTER] to quit");
			Console.ReadLine();
		}

		private async void Start()
		{
			var kinect = await KinectConnector.KinectConnection.GetStartedKinectAsync();
			Console.WriteLine("Kinect id: " + kinect.UniqueKinectId);
		}
	}
}