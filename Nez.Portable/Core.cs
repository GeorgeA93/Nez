using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Collections;
using Nez.Systems;
using Nez.Console;
using Nez.Tweens;
using Nez.Timers;
using Nez.BitmapFonts;
using Nez.Textures;
using System.Diagnostics;
using System.Runtime.CompilerServices;


[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Nez.ImGui")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Nez.Persistence")]


namespace Nez
{
	public class Core : Game
	{
		public static bool Headless { get; private set; } = false;
		/// <summary>
		/// core emitter. emits only Core level events.
		/// </summary>
		public static Emitter<CoreEvents> Emitter;

		/// <summary>
		/// enables/disables if we should quit the app when escape is pressed
		/// </summary>
		public static bool ExitOnEscapeKeypress = true;

		/// <summary>
		/// enables/disables pausing when focus is lost. No update or render methods will be called if true when not in focus.
		/// </summary>
		public static bool PauseOnFocusLost = true;

		/// <summary>
		/// enables/disables debug rendering
		/// </summary>
		public static bool DebugRenderEnabled = false;

		/// <summary>
		/// when enabled, Update runs the simulation on a fixed-timestep accumulator (FixedTimestep per tick)
		/// decoupled from the render rate. Requires IsFixedTimeStep = false. The headless/server path never sets this.
		/// </summary>
		public static bool UseSubsteppedLoop = false;

		/// <summary>
		/// simulation tick length used by the substepped loop
		/// </summary>
		public static float FixedTimestep = 1f / 60f;

		/// <summary>
		/// maximum simulation ticks the accumulator may hold; time beyond this is discarded (and logged).
		/// 30 ticks = 500ms, parity with MonoGame's fixed-step MaxElapsedTime.
		/// </summary>
		public static int MaxCatchUpTicks = 30;

		/// <summary>
		/// frames-per-second ceiling applied after Present when the substepped loop is active and vsync is off.
		/// 0 disables the limiter. Holds 1ms Windows timer resolution while active — timer resolution is
		/// per-process since Win10 2004, so the limiter cannot rely on another process having raised it.
		/// </summary>
		public static int FrameRateCap
		{
			get => _frameRateCap;
			set
			{
				if (_frameRateCap == value)
					return;

				if (OperatingSystem.IsWindows())
				{
					if (value > 0 && _frameRateCap == 0)
						WinMm.timeBeginPeriod(1);
					else if (value == 0 && _frameRateCap > 0)
						WinMm.timeEndPeriod(1);
				}

				_frameRateCap = value;
			}
		}

		static int _frameRateCap;

		static class WinMm
		{
			[System.Runtime.InteropServices.DllImport("winmm.dll")]
			internal static extern uint timeBeginPeriod(uint period);

			[System.Runtime.InteropServices.DllImport("winmm.dll")]
			internal static extern uint timeEndPeriod(uint period);
		}

		/// <summary>
		/// global access to the graphicsDevice
		/// </summary>
		public new static GraphicsDevice GraphicsDevice;

		/// <summary>
		/// global content manager for loading any assets that should stick around between scenes
		/// </summary>
		public new static NezContentManager Content;
		public static float FPS;

		/// <summary>
		/// default SamplerState used by Materials. Note that this must be set at launch! Changing it after that time will result in only
		/// Materials created after it was set having the new SamplerState
		/// </summary>
		public static SamplerState DefaultSamplerState = new SamplerState
		{
			Filter = TextureFilter.Point
		};

		/// <summary>
		/// default wrapped SamplerState. Determined by the Filter of the defaultSamplerState.
		/// </summary>
		/// <value>The default state of the wraped sampler.</value>
		public static SamplerState DefaultWrappedSamplerState =>
			DefaultSamplerState.Filter == TextureFilter.Point
				? SamplerState.PointWrap
				: SamplerState.LinearWrap;

		/// <summary>
		/// default GameServiceContainer access
		/// </summary>
		/// <value>The services.</value>
		public new static GameServiceContainer Services => ((Game)_instance).Services;

		/// <summary>
		/// provides access to the single Core/Game instance
		/// </summary>
		public static Core Instance => _instance;

		/// <summary>
		/// facilitates easy access to the global Content instance for internal classes
		/// </summary>
		internal static Core _instance;

#if DEBUG
		internal static long drawCalls;
		TimeSpan _frameCounterElapsedTime = TimeSpan.Zero;
		int _frameCounter = 0;
		public string _windowTitle;
#endif

		Scene _scene;
		Scene _nextScene;
		internal SceneTransition _sceneTransition;
		public SceneTransition SceneTransition => _sceneTransition;

		/// <summary>
		/// used to coalesce GraphicsDeviceReset events. Counted down with real frame time (not the
		/// TimeScale-scaled TimerManager) so the event still fires while the game is paused.
		/// </summary>
		float _graphicsDeviceChangeCountdown = -1f;

		/// <summary>
		/// unconsumed time carried between frames by the substepped loop
		/// </summary>
		float _tickAccumulator;

		long _nextPresentTimestamp;

		// globally accessible systems
		FastList<GlobalManager> _globalManagers = new FastList<GlobalManager>();
		CoroutineManager _coroutineManager = new CoroutineManager();
		TimerManager _timerManager = new TimerManager();

		public Point DefaultResolution;


		/// <summary>
		/// The currently active Scene. Note that if set, the Scene will not actually change until the end of the Update
		/// </summary>
		public static Scene Scene
		{
			get => _instance._scene;
			set
			{
				Insist.IsNotNull(value, "Scene cannot be null!");

				// handle our initial Scene. If we have no Scene and one is assigned directly wire it up
				if (_instance._scene == null)
				{
					_instance._scene = value;
					_instance.OnSceneChanged();
					_instance._scene.Begin();
				}
				else
				{
					_instance._nextScene = value;
				}
			}
		}


		public Core(int width = 1280, int height = 720, bool isFullScreen = false, string windowTitle = "Nez", string contentDirectory = "Content", bool hardwareModeSwitch = true)
		{
#if DEBUG
			_windowTitle = windowTitle;
#endif

			_instance = this;
			Emitter = new Emitter<CoreEvents>(new CoreEventsComparer());

			if (!Headless)
			{
				var graphicsManager = new GraphicsDeviceManager(this)
				{
					PreferredBackBufferWidth = width,
					PreferredBackBufferHeight = height,
					IsFullScreen = isFullScreen,
					SynchronizeWithVerticalRetrace = true,
#if MONOGAME_38
					HardwareModeSwitch = hardwareModeSwitch,
					PreferHalfPixelOffset = true
#endif
				};
				graphicsManager.DeviceReset += OnGraphicsDeviceReset;
				graphicsManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;

				Screen.Initialize(graphicsManager);
				Window.ClientSizeChanged += OnGraphicsDeviceReset;
				Window.OrientationChanged += OnOrientationChanged;

				base.Content.RootDirectory = contentDirectory;
				Content = new NezGlobalContentManager(Services, base.Content.RootDirectory);
				IsMouseVisible = true;
				IsFixedTimeStep = false;
			}
			else
			{
				Services.AddService(typeof(IGraphicsDeviceService), new DummyGraphicsDeviceService());

				IsFixedTimeStep = true;
			}

			DefaultResolution = new Point(width, height);

			// setup systems
			RegisterGlobalManager(_coroutineManager);
			RegisterGlobalManager(new TweenManager());
			RegisterGlobalManager(_timerManager);
			RegisterGlobalManager(new RenderTarget());
		}

		public static void UserHeadlessMode()
		{
			Sdl2.SDL_setenv("SDL_VIDEODRIVER", "dummy", overwrite: 0);
			Headless = true;
		}

		void OnOrientationChanged(object sender, EventArgs e)
		{
			Emitter.Emit(CoreEvents.OrientationChanged);
		}

		/// <summary>
		/// this gets called whenever the screen size changes
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnGraphicsDeviceReset(object sender, EventArgs e)
		{
			// we coalese these to avoid spamming events
			_graphicsDeviceChangeCountdown = 0.05f;
		}

		void UpdateGraphicsDeviceResetCoalescing(float frameDt)
		{
			if (_graphicsDeviceChangeCountdown < 0f)
				return;

			_graphicsDeviceChangeCountdown -= frameDt;
			if (_graphicsDeviceChangeCountdown < 0f)
				Emitter.Emit(CoreEvents.GraphicsDeviceReset);
		}


		#region Passthroughs to Game

		public new static void Exit()
		{
			((Game)_instance).Exit();
		}

		#endregion


		#region Game overides

		protected override void Initialize()
		{
			if (!Headless)
			{
				base.Initialize();

				// prep the default Graphics system
				GraphicsDevice = base.GraphicsDevice;
				var font = Content.Load<BitmapFont>("nez://Nez.Content.NezDefaultBMFont.xnb");
				Graphics.Instance = new Graphics(font);
			}
		}

		protected override void Update(GameTime gameTime)
		{
			if (!Headless)
			{

				if (PauseOnFocusLost && !IsActive)
				{
					SuppressDraw();
					return;
				}
			}

			UpdateGraphicsDeviceResetCoalescing((float)gameTime.ElapsedGameTime.TotalSeconds);

			if (UseSubsteppedLoop)
			{
				SubsteppedUpdate((float)gameTime.ElapsedGameTime.TotalSeconds);
			}
			else
			{
				Time.RenderDeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

				// update all our systems and global managers
				Time.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
				Input.Update();

				if (ExitOnEscapeKeypress &&
					(Input.IsKeyDown(Keys.Escape) || Input.GamePads[0].IsButtonReleased(Buttons.Back)))
				{
					base.Exit();
					return;
				}

				if (_scene != null)
				{
					for (var i = _globalManagers.Length - 1; i >= 0; i--)
					{
						if (_globalManagers.Buffer[i].Enabled)
							_globalManagers.Buffer[i].Update();
					}

					UpdateSceneAndHandleSceneChange();
				}

				if (!Headless)
				{
					EndDebugUpdate();
				}
			}

#if FNA
			// MonoGame only updates old-school XNA Components in Update which we dont care about. FNA's core FrameworkDispatcher needs
			// Update called though so we do so here.
			FrameworkDispatcher.Update();
#endif
		}

		void SubsteppedUpdate(float frameDt)
		{
			Time.RenderDeltaTime = frameDt;

			var maxAccumulator = FixedTimestep * MaxCatchUpTicks;
			_tickAccumulator += frameDt;
			if (_tickAccumulator > maxAccumulator)
			{
				Debug.Warn("substepped loop dropped {0:0.0}ms of sim time (frame dt {1:0.0}ms)",
					(_tickAccumulator - maxAccumulator) * 1000f, frameDt * 1000f);
				_tickAccumulator = maxAccumulator;
			}

			// frame-mode managers run exactly once per rendered frame, BEFORE the tick loop: ImGui builds its
			// draw list and intercepts input here, which must precede any scene tick consuming that input
			if (_scene != null)
			{
				for (var i = _globalManagers.Length - 1; i >= 0; i--)
				{
					var manager = _globalManagers.Buffer[i];
					if (manager.Enabled && manager.UpdateMode == GlobalManagerUpdateMode.Frame)
						manager.Update();
				}
			}

			while (_tickAccumulator >= FixedTimestep)
			{
				_tickAccumulator -= FixedTimestep;

				Time.Update(FixedTimestep);
				Input.Update();

				if (ExitOnEscapeKeypress &&
					(Input.IsKeyDown(Keys.Escape) || Input.GamePads[0].IsButtonReleased(Buttons.Back)))
				{
					base.Exit();
					return;
				}

				if (_scene != null)
				{
					for (var i = _globalManagers.Length - 1; i >= 0; i--)
					{
						var manager = _globalManagers.Buffer[i];
						if (manager.Enabled && manager.UpdateMode == GlobalManagerUpdateMode.Tick)
							manager.Update();
					}

					var sceneChanged = UpdateSceneAndHandleSceneChange();
					if (sceneChanged)
					{
						// scene load + GC can take hundreds of ms; starting the new scene with a catch-up
						// burst would replay that stall as sim ticks
						_tickAccumulator = 0f;
						break;
					}

					// snapshot AFTER all movement for this tick
					_scene.Interpolator?.OnTickEnd();
				}

				OnTickUpdate();

				if (!Headless)
				{
					EndDebugUpdate();
				}
			}

			Time.RenderAlpha = _tickAccumulator / FixedTimestep;
		}

		bool UpdateSceneAndHandleSceneChange()
		{
			// read carefully:
			// - we do not update the Scene while a SceneTransition is happening
			// 		- unless it is SceneTransition that doesn't change Scenes (no reason not to update)
			//		- or it is a SceneTransition that has already switched to the new Scene (the new Scene needs to do its thing)
			if (_sceneTransition == null ||
				(_sceneTransition != null &&
				 (!_sceneTransition._loadsNewScene || _sceneTransition._isNewSceneLoaded)))
			{
				_scene.Update();
			}

			if (_nextScene != null)
			{
				_scene.End();

				_scene = _nextScene;
				_nextScene = null;
				OnSceneChanged();

				_scene.Begin();
				return true;
			}

			return false;
		}

		/// <summary>
		/// called at the end of every simulation tick when the substepped loop is active. Input edges
		/// (IsKeyPressed etc.) are only valid inside a tick — override this instead of checking them after
		/// base.Update, where multi-tick frames have already consumed them and zero-tick frames re-fire them.
		/// </summary>
		protected virtual void OnTickUpdate()
		{
		}

		protected override void Draw(GameTime gameTime)
		{
			if (Headless) { return; }

			if (PauseOnFocusLost && !IsActive)
				return;

			StartDebugDraw(gameTime.ElapsedGameTime);

			// fetched at draw time, never cached — the scene can change between frames
			var interpolator = _scene?.Interpolator;
			interpolator?.Apply(Time.RenderAlpha);

			if (_sceneTransition != null)
				_sceneTransition.PreRender(Graphics.Instance.Batcher);

			// special handling of SceneTransition if we have one. We either render the SceneTransition or the Scene
			if (_sceneTransition != null)
			{
				if (_scene != null && _sceneTransition.WantsPreviousSceneRender &&
					!_sceneTransition.HasPreviousSceneRender)
				{
					_scene.Render();
					_scene.PostRender(_sceneTransition.PreviousSceneRender);
					StartCoroutine(_sceneTransition.OnBeginTransition());
				}
				else if (_scene != null && _sceneTransition._isNewSceneLoaded)
				{
					_scene.Render();
					_scene.PostRender();
				}

				_sceneTransition.Render(Graphics.Instance.Batcher);
			}
			else if (_scene != null)
			{
				_scene.Render();

#if DEBUG
				if (DebugRenderEnabled)
					Debug.Render();
#endif

				// render as usual if we dont have an active SceneTransition
				_scene.PostRender();
			}

			// transforms must be back at their exact tick values before the next tick runs
			interpolator?.Restore();

			EndDebugDraw();
		}

		protected override void EndDraw()
		{
			// base.EndDraw is exactly Platform.Present — nothing else happens after it in the frame
			base.EndDraw();

			var cap = _frameRateCap;
			if (!UseSubsteppedLoop || cap <= 0)
				return;

			var ticksPerFrame = Stopwatch.Frequency / cap;
			var now = Stopwatch.GetTimestamp();
			if (_nextPresentTimestamp < now - ticksPerFrame || _nextPresentTimestamp == 0)
				_nextPresentTimestamp = now;

			// Sleep(1) wakes in ~1.5-2ms even at 1ms timer resolution, so sleep only while there is
			// comfortably more than that left and spin the remainder
			var sleepGuardTicks = Stopwatch.Frequency / 400;
			while (true)
			{
				now = Stopwatch.GetTimestamp();
				var remaining = _nextPresentTimestamp - now;
				if (remaining <= 0)
					break;

				if (remaining > sleepGuardTicks)
					System.Threading.Thread.Sleep(1);
				else
					System.Threading.Thread.SpinWait(32);
			}

			_nextPresentTimestamp += ticksPerFrame;
		}

		protected override void OnExiting(object sender, ExitingEventArgs args)
		{
			base.OnExiting(sender, args);

			if (OperatingSystem.IsWindows() && _frameRateCap > 0)
				WinMm.timeEndPeriod(1);

			Emitter.Emit(CoreEvents.Exiting);
		}

		#endregion

		#region Debug Injection

		[Conditional("DEBUG")]
		void EndDebugUpdate()
		{
#if DEBUG
			DebugConsole.Instance.Update();
			drawCalls = 0;
#endif
		}

		[Conditional("DEBUG")]
		void StartDebugDraw(TimeSpan elapsedGameTime)
		{
#if DEBUG
			// fps counter
			_frameCounter++;
			_frameCounterElapsedTime += elapsedGameTime;
			if (_frameCounterElapsedTime >= TimeSpan.FromSeconds(1))
			{
				var totalMemory = (GC.GetTotalMemory(false) / 1048576f).ToString("F");
				FPS = _frameCounter;
				Window.Title = string.Format("{0} {1} fps - {2} MB", _windowTitle, _frameCounter, totalMemory);
				_frameCounter = 0;
				_frameCounterElapsedTime -= TimeSpan.FromSeconds(1);
			}
#endif
		}

		[Conditional("DEBUG")]
		void EndDebugDraw()
		{
#if DEBUG
			DebugConsole.Instance.Render();
#if !FNA
			drawCalls = GraphicsDevice.Metrics.DrawCount;
#endif
#endif
		}

		#endregion

		/// <summary>
		/// Called after a Scene ends, before the next Scene begins
		/// </summary>
		void OnSceneChanged()
		{
			Emitter.Emit(CoreEvents.SceneChanged);
			Time.SceneChanged();
			GC.Collect();
		}

		/// <summary>
		/// temporarily runs SceneTransition allowing one Scene to transition to another smoothly with custom effects.
		/// </summary>
		/// <param name="sceneTransition">Scene transition.</param>
		public static T StartSceneTransition<T>(T sceneTransition) where T : SceneTransition
		{
			Insist.IsNull(_instance._sceneTransition,
				"You cannot start a new SceneTransition until the previous one has completed");
			_instance._sceneTransition = sceneTransition;
			return sceneTransition;
		}


		#region Global Managers

		/// <summary>
		/// adds a global manager object that will have its update method called each frame before Scene.update is called
		/// </summary>
		/// <returns>The global manager.</returns>
		/// <param name="manager">Manager.</param>
		public static void RegisterGlobalManager(GlobalManager manager)
		{
			_instance._globalManagers.Add(manager);
			manager.Enabled = true;
		}

		/// <summary>
		/// removes the global manager object
		/// </summary>
		/// <returns>The global manager.</returns>
		/// <param name="manager">Manager.</param>
		public static void UnregisterGlobalManager(GlobalManager manager)
		{
			_instance._globalManagers.Remove(manager);
			manager.Enabled = false;
		}

		/// <summary>
		/// gets the global manager of type T
		/// </summary>
		/// <returns>The global manager.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static T GetGlobalManager<T>() where T : GlobalManager
		{
			for (var i = 0; i < _instance._globalManagers.Length; i++)
			{
				if (_instance._globalManagers.Buffer[i] is T)
					return _instance._globalManagers.Buffer[i] as T;
			}

			return null;
		}

		#endregion


		#region Systems access

		/// <summary>
		/// starts a coroutine. Coroutines can yeild ints/floats to delay for seconds or yeild to other calls to startCoroutine.
		/// Yielding null will make the coroutine get ticked the next frame.
		/// </summary>
		/// <returns>The coroutine.</returns>
		/// <param name="enumerator">Enumerator.</param>
		public static ICoroutine StartCoroutine(IEnumerator enumerator)
		{
			return _instance._coroutineManager.StartCoroutine(enumerator);
		}

		/// <summary>
		/// schedules a one-time or repeating timer that will call the passed in Action
		/// </summary>
		/// <param name="timeInSeconds">Time in seconds.</param>
		/// <param name="repeats">If set to <c>true</c> repeats.</param>
		/// <param name="context">Context.</param>
		/// <param name="onTime">On time.</param>
		public static ITimer Schedule(float timeInSeconds, bool repeats, object context, Action<ITimer> onTime)
		{
			return _instance._timerManager.Schedule(timeInSeconds, repeats, context, onTime);
		}

		/// <summary>
		/// schedules a one-time timer that will call the passed in Action after timeInSeconds
		/// </summary>
		/// <param name="timeInSeconds">Time in seconds.</param>
		/// <param name="context">Context.</param>
		/// <param name="onTime">On time.</param>
		public static ITimer Schedule(float timeInSeconds, object context, Action<ITimer> onTime)
		{
			return _instance._timerManager.Schedule(timeInSeconds, false, context, onTime);
		}

		/// <summary>
		/// schedules a one-time or repeating timer that will call the passed in Action
		/// </summary>
		/// <param name="timeInSeconds">Time in seconds.</param>
		/// <param name="repeats">If set to <c>true</c> repeats.</param>
		/// <param name="onTime">On time.</param>
		public static ITimer Schedule(float timeInSeconds, bool repeats, Action<ITimer> onTime)
		{
			return _instance._timerManager.Schedule(timeInSeconds, repeats, null, onTime);
		}

		/// <summary>
		/// schedules a one-time timer that will call the passed in Action after timeInSeconds
		/// </summary>
		/// <param name="timeInSeconds">Time in seconds.</param>
		/// <param name="onTime">On time.</param>
		public static ITimer Schedule(float timeInSeconds, Action<ITimer> onTime)
		{
			return _instance._timerManager.Schedule(timeInSeconds, false, null, onTime);
		}

		#endregion
	}
}