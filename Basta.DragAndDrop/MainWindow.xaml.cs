using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Interaction;

namespace Basta.DragAndDrop
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private TextBlock lastTouched;
		private TaskScheduler uiScheduler;
		private Skeleton[] skeletonData;
		private DepthImagePixel[] depthData;
		private UserInfo[] userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];

		public MainWindow()
		{
			InitializeComponent();
			this.uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
			this.DataContext = this;
			Task.Factory.StartNew(() => this.ConnectKinect());
		}

		private async void ConnectKinect()
		{
			this.Kinect = await KinectConnector.KinectConnection.GetStartedKinectAsync();
			this.OnPropertyChanged("Kinect");

			this.skeletonData = new Skeleton[this.Kinect.SkeletonStream.FrameSkeletonArrayLength];
			this.depthData = new DepthImagePixel[this.Kinect.DepthStream.FramePixelDataLength];

			this.SubscribeToStreams();
		}

		private void SubscribeToStreams()
		{
			var interactionStream = new InteractionStream(this.Kinect, new InteractionClient());

			this.Kinect.AllFramesReady += (o, e) =>
			{
				long skeletonTimestamp = 0;
				long depthTimestamp = 0;
				var accelerometerReading = this.Kinect.AccelerometerGetCurrentReading();

				using (var depthImageFrame = e.OpenDepthImageFrame())
				using (var skeletonFrame = e.OpenSkeletonFrame())
				{
					if (depthImageFrame == null || skeletonFrame == null) return;

					skeletonFrame.CopySkeletonDataTo(this.skeletonData);
					skeletonTimestamp = skeletonFrame.Timestamp;
					this.depthData = depthImageFrame.GetRawPixelData();
					depthTimestamp = depthImageFrame.Timestamp;
				}

				interactionStream.ProcessDepth(depthData, depthTimestamp);
				interactionStream.ProcessSkeleton(skeletonData, accelerometerReading, skeletonTimestamp);
			};

			interactionStream.InteractionFrameReady += InteractionFrameReady;
		}

		private async void InteractionFrameReady(object sender, InteractionFrameReadyEventArgs e)
		{
			using (var interactionFrame = e.OpenInteractionFrame())
			{
				if (interactionFrame != null)
					interactionFrame.CopyInteractionDataTo(this.userInfos);
			}

			var hand = this.userInfos.SelectMany(_ => _.HandPointers.Where(__ => __.HandType == InteractionHandType.Right))
									.FirstOrDefault();

			if (hand == null) return;

			var point = new Point(hand.X * this.kinectRegion.ActualWidth, hand.Y * this.kinectRegion.ActualHeight);

			await Task.Factory.StartNew(() =>
			{
				if(hand.HandEventType == InteractionHandEventType.Grip)
				{
					var elem = this.kinectRegion.InputHitTest(point) as TextBlock;
					if (elem == null) return;

					this.lastTouched = elem;

				}
				else if (hand.HandEventType == InteractionHandEventType.GripRelease)
				{
					this.lastTouched = null;
					return;
				}
				else
				{
					if (this.lastTouched == null) return;

					Canvas.SetLeft(this.lastTouched, point.X - this.lastTouched.ActualWidth / 2); // use center of UIElement -> divide by 2
					Canvas.SetTop(this.lastTouched, point.Y - this.userViewer.ActualHeight - this.lastTouched.ActualHeight / 2); // use center of UIElement -> divide by 2
				}
			}, CancellationToken.None, TaskCreationOptions.None, this.uiScheduler); 
		}

		public KinectSensor Kinect { get; set; }

		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		private void OnPropertyChanged(string propName)
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}
	}
}