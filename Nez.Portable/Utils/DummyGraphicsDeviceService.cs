using System;
using Microsoft.Xna.Framework.Graphics;

namespace Nez {
	public class DummyGraphicsDeviceService : IGraphicsDeviceService {
		public GraphicsDevice GraphicsDevice { get; }
#pragma warning disable 0067
		public event EventHandler<EventArgs> DeviceCreated;
		public event EventHandler<EventArgs> DeviceDisposing;
		public event EventHandler<EventArgs> DeviceReset;
		public event EventHandler<EventArgs> DeviceResetting;
#pragma warning restore 0067
	}
}