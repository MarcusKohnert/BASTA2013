using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;

namespace StreamViewer
{
	public class BoneDrawer
	{
		#region Constants and Fields

		private KinectSensor kinect;

		/// <summary>
		/// Width of output drawing
		/// </summary>
		private const float RenderWidth = 640.0f;

		/// <summary>
		/// Height of our output drawing
		/// </summary>
		private const float RenderHeight = 480.0f;

		/// <summary>
		/// Thickness of drawn joint lines
		/// </summary>
		private const double JointThickness = 3;

		/// <summary>
		/// Thickness of body center ellipse
		/// </summary>
		private const double BodyCenterThickness = 10;

		/// <summary>
		/// Thickness of clip edge rectangles
		/// </summary>
		private const double ClipBoundsThickness = 10;

		/// <summary>
		/// Brush used to draw skeleton center point
		/// </summary>
		private static readonly Brush centerPointBrush = Brushes.Blue;

		/// <summary>
		/// Brush used for drawing joints that are currently tracked
		/// </summary>
		private static readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

		/// <summary>
		/// Brush used for drawing joints that are currently inferred
		/// </summary>        
		private static readonly Brush inferredJointBrush = Brushes.Yellow;

		/// <summary>
		/// Pen used for drawing bones that are currently tracked
		/// </summary>
		private static readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

		/// <summary>
		/// Pen used for drawing bones that are currently inferred
		/// </summary>        
		private static readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

		#endregion

		public BoneDrawer(KinectSensor kinect)
		{
			this.kinect = kinect;
		}

		internal void Draw(DrawingGroup drawingGroup, Skeleton[] skeletons)
		{
			using (DrawingContext dc = drawingGroup.Open())
			{
				dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

				foreach (var skeleton in skeletons)
				{
					RenderClippedEdges(skeleton, dc);

					if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
					{
						DrawBonesAndJoints(skeleton, dc);
					}
					else if (skeleton.TrackingState == SkeletonTrackingState.PositionOnly)
					{
						dc.DrawEllipse(
							centerPointBrush,
							null,
							SkeletonPointToScreen(skeleton.Position),
							BodyCenterThickness,
							BodyCenterThickness
							);
					}
				}

				drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
			}
		}

		private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
		{
			// Render Torso
			this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
			this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
			this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
			this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
			this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
			this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
			this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

			// Left Arm
			this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
			this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
			this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

			// Right Arm
			this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
			this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
			this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

			// Left Leg
			this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
			this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
			this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

			// Right Leg
			this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
			this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
			this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

			// Render Joints
			foreach (Joint joint in skeleton.Joints)
			{
				Brush drawBrush = null;

				if (joint.TrackingState == JointTrackingState.Tracked)
				{
					drawBrush = trackedJointBrush;
				}
				else if (joint.TrackingState == JointTrackingState.Inferred)
				{
					drawBrush = inferredJointBrush;
				}

				if (drawBrush != null)
				{
					drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
				}
			}
		}

		private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
		{
			Joint joint0 = skeleton.Joints[jointType0];
			Joint joint1 = skeleton.Joints[jointType1];

			// If we can't find either of these joints, exit
			if (joint0.TrackingState == JointTrackingState.NotTracked ||
				joint1.TrackingState == JointTrackingState.NotTracked)
			{
				return;
			}

			// Don't draw if both points are inferred
			if (joint0.TrackingState == JointTrackingState.Inferred &&
				joint1.TrackingState == JointTrackingState.Inferred)
			{
				return;
			}

			// We assume all drawn bones are inferred unless BOTH joints are tracked
			Pen drawPen = inferredBonePen;
			if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
			{
				drawPen = trackedBonePen;
			}

			drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
		}

		private Point SkeletonPointToScreen(SkeletonPoint skeletonPoint)
		{
			var depthPoint = kinect.MapSkeletonPointToDepth(skeletonPoint, DepthImageFormat.Resolution640x480Fps30);
			return new Point(depthPoint.X, depthPoint.Y);
		}

		private void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
		{
			if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
			{
				drawingContext.DrawRectangle(
					Brushes.Red,
					null,
					new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
			}

			if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
			{
				drawingContext.DrawRectangle(
					Brushes.Red,
					null,
					new Rect(0, 0, RenderWidth, ClipBoundsThickness));
			}

			if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
			{
				drawingContext.DrawRectangle(
					Brushes.Red,
					null,
					new Rect(0, 0, ClipBoundsThickness, RenderHeight));
			}

			if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
			{
				drawingContext.DrawRectangle(
					Brushes.Red,
					null,
					new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
			}
		}
	}
}