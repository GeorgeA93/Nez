using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Nez
{
	public class VignettePostProcessor : PostProcessor
	{
		[Range(0.001f, 10f, 0.001f)]
		public float Power
		{
			get => _power;
			set
			{
				if (_power != value)
				{
					_power = value;

					if (Effect != null)
						_powerParam.SetValue(_power);
				}
			}
		}

		[Range(0.001f, 10f, 0.001f)]
		public float Radius
		{
			get => _radius;
			set
			{
				if (_radius != value)
				{
					_radius = value;

					if (Effect != null)
						_radiusParam.SetValue(_radius);
				}
			}
		}

		public Color VignetteColor
		{
			get => _vignetteColor;
			set
			{
				if (_vignetteColor != value)
				{
					_vignetteColor = value;

					if (Effect != null)
						_colorParam.SetValue(_vignetteColor.ToVector4());
				}
			}
		}

		float _power = 1f;
		float _radius = 1.25f;
		Color _vignetteColor = Color.Black;
		EffectParameter _powerParam;
		EffectParameter _radiusParam;
		EffectParameter _colorParam;
		EffectParameter _screenSizeParam;

		public VignettePostProcessor(int executionOrder) : base(executionOrder)
		{
		}

		public override void OnAddedToScene(Scene scene)
		{
			base.OnAddedToScene(scene);

			Effect = scene.Content.LoadEffect<Effect>("vignette", EffectResource.VignetteBytes);

			_powerParam = Effect.Parameters["_power"];
			_radiusParam = Effect.Parameters["_radius"];
			_colorParam = Effect.Parameters["_vignetteColor"];
			_screenSizeParam = Effect.Parameters["_screenSize"];
			_powerParam.SetValue(_power);
			_radiusParam.SetValue(_radius);
			_colorParam.SetValue(_vignetteColor.ToVector4());
			_screenSizeParam?.SetValue(new Vector2(Screen.Width, Screen.Height));
		}

		public override void Process(RenderTarget2D source, RenderTarget2D destination)
		{
			// Update screen size for dithering calculation
			_screenSizeParam?.SetValue(new Vector2(source.Width, source.Height));
			base.Process(source, destination);
		}

		public override void Unload()
		{
			_scene.Content.UnloadEffect(Effect);
			base.Unload();
		}
	}
}