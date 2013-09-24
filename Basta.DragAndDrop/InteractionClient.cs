using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect.Toolkit.Interaction;

namespace Basta.DragAndDrop
{
	public class InteractionClient : IInteractionClient
	{
		public InteractionInfo GetInteractionInfoAtLocation(int skeletonTrackingId,
												InteractionHandType handType, double x, double y)
		{
			return new InteractionInfo 
			{
				
			};
		}
	}
}