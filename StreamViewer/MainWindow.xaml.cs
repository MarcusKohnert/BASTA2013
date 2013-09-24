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

namespace StreamViewer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private KinectSensor kinect;
		private TaskScheduler uiScheduler;

		private const int RedIndex = 2;
		private const int GreenIndex = 1;
		private const int BlueIndex = 0;
		// color divisors for tinting depth pixels 
		private static readonly int[] IntensityShiftByPlayerR = { 1, 2, 0, 2, 0, 0, 2, 0 };
		private static readonly int[] IntensityShiftByPlayerG = { 1, 2, 2, 0, 2, 0, 0, 1 };
		private static readonly int[] IntensityShiftByPlayerB = { 1, 0, 2, 2, 0, 2, 0, 2 };

		public MainWindow()
		{
			this.uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
			InitializeComponent();
			this.KinectColorImage = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);
			this.KinectDepthImage = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);
			this.KinectSkeletonImage = new DrawingImage(this.drawingGroup);

			this.DataContext = this;
			Task.Factory.StartNew(() => this.SubscribeToKinect());
		}

		public WriteableBitmap KinectColorImage { get; private set; }
		public WriteableBitmap KinectDepthImage { get; private set; }

		private BoneDrawer boneDrawer;
		private DrawingGroup drawingGroup = new DrawingGroup();
		public DrawingImage KinectSkeletonImage { get; private set; }

		private async void SubscribeToKinect()
		{
			this.kinect = await KinectConnector.KinectConnection.GetStartedKinectAsync();

			this.kinect.AllFramesReady += AllFramesReady;
			Task.Factory.StartNew(() => this.boneDrawer = new BoneDrawer(this.kinect), CancellationToken.None, TaskCreationOptions.None, this.uiScheduler);
		}

		private void AllFramesReady(object sender, AllFramesReadyEventArgs args)
		{
			var colorData = new byte[this.kinect.ColorStream.FramePixelDataLength];
			var depthData = new short[this.kinect.DepthStream.FramePixelDataLength];
			var skeletons = new Skeleton[this.kinect.SkeletonStream.FrameSkeletonArrayLength];

			using(var colorFrame = args.OpenColorImageFrame())
			{
				if(colorFrame != null)
					colorFrame.CopyPixelDataTo(colorData);
			}

			using(var depthFrame = args.OpenDepthImageFrame())
			{
				if(depthFrame != null)
					depthFrame.CopyPixelDataTo(depthData);
			}

			using (var skeletonFrame = args.OpenSkeletonFrame())
			{
				if (skeletonFrame != null)
					skeletonFrame.CopySkeletonDataTo(skeletons);
			}

			var depthBits = this.ConvertDepthFrame(depthData);

			Task.Factory.StartNew(() => 
			{
				this.KinectColorImage.WritePixels(new Int32Rect(0, 0, 640, 480), colorData, 640 * 4, 0);
				this.KinectDepthImage.WritePixels(new Int32Rect(0, 0, 640, 480), depthBits, 640 * 4, 0);
				this.boneDrawer.Draw(this.drawingGroup, skeletons);
				this.OnPropertyChanged("KinectColorImage");
				this.OnPropertyChanged("KinectDepthImage");
			}, CancellationToken.None, TaskCreationOptions.None, this.uiScheduler);
		}

		private byte[] ConvertDepthFrame(short[] depthFrame)
		{
			var tooNearDepth = 50;
			var tooFarDepth = 200;
			var unknownDepth = 250;
			var depthBits = new byte[640 * 480 * 4];

			for (int i16 = 0, i32 = 0; i16 < depthFrame.Length && i32 < depthBits.Length; i16++, i32 += 4)
			{
				int player = depthFrame[i16] & Microsoft.Kinect.DepthImageFrame.PlayerIndexBitmask;
				int realDepth = depthFrame[i16] >> Microsoft.Kinect.DepthImageFrame.PlayerIndexBitmaskWidth;

				// transform 13-bit depth information into an 8-bit intensity appropriate // for display (we disregard information in most significant bit) 
				byte intensity = (byte)(~(realDepth >> 4));

				if (player == 0 && realDepth == 0)
				{
					// white this.depthFrame32[i32 + RedIndex] = 255;
					depthBits[i32 + GreenIndex] = 255;
					depthBits[i32 + BlueIndex] = 255;
				}
				else if (player == 0 && realDepth == tooFarDepth)
				{
					// dark purple this.depthFrame32[i32 + RedIndex] = 66;
					depthBits[i32 + GreenIndex] = 0;
					depthBits[i32 + BlueIndex] = 66;
				}
				else if (player == 0 && realDepth == unknownDepth)
				{
					// dark brown this.depthFrame32[i32 + RedIndex] = 66;
					depthBits[i32 + GreenIndex] = 66;
					depthBits[i32 + BlueIndex] = 33;
				}
				else
				{
					// tint the intensity by dividing by per-player values 
					depthBits[i32 + RedIndex] = (byte)(intensity >> IntensityShiftByPlayerR[player]);
					depthBits[i32 + GreenIndex] = (byte)(intensity >> IntensityShiftByPlayerG[player]);
					depthBits[i32 + BlueIndex] = (byte)(intensity >> IntensityShiftByPlayerB[player]);
				}
			}

			return depthBits;
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };

		private void OnPropertyChanged(string propName)
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propName)); 
		}
	}
}