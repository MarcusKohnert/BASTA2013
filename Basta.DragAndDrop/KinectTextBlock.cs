using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Kinect.Toolkit.Controls;

namespace Basta.DragAndDrop
{
	public class KinectTextBlock : TextBlock
	{
		private static readonly bool IsInDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());
		private bool isGripped;

		public KinectTextBlock()
		{
			if (!IsInDesignMode)
			{
				KinectRegion.AddHandPointerGripHandler(this, this.OnHandPointerGrip);
				KinectRegion.AddHandPointerGripReleaseHandler(this, this.OnHandPointerGripRelease);
				KinectRegion.AddQueryInteractionStatusHandler(this, this.OnQueryInteractionStatus);
				KinectRegion.AddHandPointerEnterHandler(this, this.OnHandPointerEnter);
				KinectRegion.AddHandPointerLeaveHandler(this, this.OnHandPointerLeave);
			}
		}

		private void OnHandPointerLeave(object sender, HandPointerEventArgs e)
		{
			this.Background = Brushes.Black;
			e.Handled = true;
		}

		private void OnHandPointerEnter(object sender, HandPointerEventArgs e)
		{
			this.Background = Brushes.Gray;
			e.Handled = true;
		}

		private void OnQueryInteractionStatus(object sender, QueryInteractionStatusEventArgs e)
		{
			if (this.Equals(e.HandPointer.Captured))
			{
				e.IsInGripInteraction = this.isGripped;
				e.Handled = true;
			}
		}

		private void OnHandPointerGrip(object sender, HandPointerEventArgs e)
		{
			if (e.HandPointer == null) return;

			this.isGripped = true;
			e.HandPointer.Capture(this);
			e.Handled = true;
		}

		private void OnHandPointerGripRelease(object sender, HandPointerEventArgs e)
		{
			if (e.HandPointer == null) return;

			this.isGripped = false;
			e.HandPointer.Capture(null);
			e.Handled = true;
		}
	}
}