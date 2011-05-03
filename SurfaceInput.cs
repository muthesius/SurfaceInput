/**
 * VVVV Plugin to directly access the data of any "contacts" on a Microsoft Surface 1.0
 * 
 * Licensed under the MIT License:
 *
 * Copyright (c) 2010-2011 jens alexander ewald, chris engler, muthesius kunsthochschule kiel
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 * 
 */
 
#region usings
using System;
using System.ComponentModel.Composition;

//using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

using Microsoft.Surface;
using Microsoft.Surface.Core;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;

using VVVV.Core.Logging;
using VVVV.Utils.VMath;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "SurfaceInput",
	            Category = "Input",
	            Version = "0.1",
	            Help = "Gives you the data of Surface Contacts. Only use with Surfacetable or Simulator",
	            Tags = "")]
	#endregion PluginInfo
	
	// DXTextureOutPluginBase,
	public class SurfaceInput : IDisposable, IPluginEvaluate
	{
		#region fields & pins
		[Input("Enable",
		       DefaultValue = 1,
		       Visibility = PinVisibility.Hidden,
		       IsSingle = true)]
		IDiffSpread<bool> bEnable;
		
		[Input("NormalizeValues",
		       DefaultValue = 1,
		       Visibility = PinVisibility.Hidden,
		       IsSingle = true)]
		ISpread<bool> bNormalizeValues;
				
		//************************************* Outputs
		[Output("ID")]
		ISpread<int> IDout;

		// fingers
		[Output("contactCoords")]
		ISpread<Vector2D> contactCoord;

		[Output("contactSize")]
		ISpread<Vector2D> contactSize;
		
		[Output("contactRotation")]
		ISpread<double> contactRotation;
		
//		// Tags
//		[Output("tagCoords")]
//		ISpread<Vector2D> tagCoords;
//		
//		[Output("tagRotation")]
//		ISpread<double> tagRotation;
		
		[Import()]
		ILogger FLogger;
		#endregion fields & pins
		

		#region SurfaceCredentials
		// We need a "Window" ivar for this to work.
		private static Form Window = null;
		public static ContactTarget contactTarget = null;
		
		// no use of the raw image so far
		//private bool bHasImage = false;
		

		public static Mutex imageBufferMutex;
		
		private static void makeWindow() {
			if (Window != null) return; // simply return, if we did this already
			// Create a new Forms Window, hidden!
			Window = new Form();
			Window.FormBorderStyle = FormBorderStyle.None;
			Window.StartPosition = FormStartPosition.Manual;
			Window.Location = new System.Drawing.Point(0,0);
			Window.Size = new System.Drawing.Size(512,512);
			Window.Opacity = 1;
			Window.ShowInTaskbar = false;
			Window.Hide();
		}

		/// <summary>
		/// Initializes the surface input system. This should be called after any window
		/// initialization is done, and should only be called once.
		/// </summary>
		private void InitializeSurfaceInput()
		{
			System.Diagnostics.Debug.Assert(Window.Handle != System.IntPtr.Zero,
			                                "Window initialization must be complete before InitializeSurfaceInput is called");
			if (Window.Handle == System.IntPtr.Zero)
				return;
			//System.Diagnostics.Debug.Assert(contactTarget == null,
			//                                "Surface input already initialized");
			
			if (contactTarget != null)
				return;

			// Create a target for surface input.
			// Use an IntPtr.Zero as the handle, but still have a Window(!!) as an ivar
			// to get contacts globaly.
			contactTarget = new ContactTarget(IntPtr.Zero,EventThreadChoice.OnCurrentThread,true);
			
			// Enable the Surface input on this target:
			//bHasImage = contactTarget.EnableImage(ImageType.Normalized);
			//if (bHasImage) contactTarget.FrameReceived += new EventHandler<FrameReceivedEventArgs>(contactTarget_FrameReceived);
			
			contactTarget.EnableInput();
		}
		
		public static object syncObj = new Object();
		private static bool bImageUpated = false;
		void contactTarget_FrameReceived(object sender, FrameReceivedEventArgs e)
		{
		}

		#endregion SurfaceCredentials
		
		#region Constructor
		/// <summary>
		/// Constructor. Builds a new Surface Input node.
		/// It also creates a window on it's own and hides it.
		/// </summary>
		[ImportingConstructor()]
		public SurfaceInput()
		{
			makeWindow();
			InitializeSurfaceInput();
		}

		public void Dispose() {
			if (Window!=null) Window.Dispose();
		}
		
		#endregion Constructor

		#region Evaluate
		public void Evaluate(int SpreadMax)
		{
			if(!bEnable[0]) return;
			
			// Get the current state of our contacts.
			// Nice, this seems to be a threadsafe read.
			ReadOnlyContactCollection contacts = contactTarget.GetState();
			
			IDout.SliceCount = contactSize.SliceCount = contactRotation.SliceCount
				= contactCoord.SliceCount = contacts.Count;
			
			int i = 0;
			foreach (Contact c in contacts)
			{
				IDout[i] = c.Id;
				
				Vector2D pos = new Vector2D(c.X,c.Y);
				pos.x = (bNormalizeValues[0]) ? pos.x/512-1	 : pos.x;
				pos.y = (bNormalizeValues[0]) ? (2-pos.y/384)-1 : pos.y;
				
				double rotation = 1-c.Orientation/(2*Math.PI);
				
				contactSize[i] = new Vector2D(c.Bounds.Width/1024.0,
					                              c.Bounds.Height/768.0);
				contactCoord[i] = pos;
				contactRotation[i] = rotation;
				i++;
			}
		}
		#endregion Evaluate
	}
}
