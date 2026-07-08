using System.Runtime.CompilerServices;


namespace Nez
{
	/// <summary>
	/// provides frame timing information
	/// </summary>
	public static class Time
	{
		/// <summary>
		/// total time the game has been running
		/// </summary>
		public static float TotalTime;

		/// <summary>
		/// delta time from the previous frame to the current, scaled by timeScale
		/// </summary>
		public static float DeltaTime;

		/// <summary>
		/// unscaled version of deltaTime. Not affected by timeScale
		/// </summary>
		public static float UnscaledDeltaTime;

		/// <summary>
		/// secondary deltaTime for use when you need to scale two different deltas simultaneously
		/// </summary>
		public static float AltDeltaTime;

		/// <summary>
		/// total time since the Scene was loaded
		/// </summary>
		public static float TimeSinceSceneLoad;

		/// <summary>
		/// time scale of deltaTime
		/// </summary>
		public static float TimeScale = 1f;

		/// <summary>
		/// time scale of altDeltaTime
		/// </summary>
		public static float AltTimeScale = 1f;

		/// <summary>
		/// total number of simulation ticks that have passed. Under the substepped loop this advances per tick,
		/// not per rendered frame — never key draw-side logic to it.
		/// </summary>
		public static uint FrameCount;

		/// <summary>
		/// real time elapsed since the previous rendered frame. Unlike DeltaTime this tracks the render rate,
		/// not the simulation tick. Use for purely visual, draw-path animation.
		/// </summary>
		public static float RenderDeltaTime;

		/// <summary>
		/// fraction [0,1) of the way from the last completed simulation tick to the next one at render time.
		/// Only meaningful when Core.UseSubsteppedLoop is enabled; 0 otherwise.
		/// </summary>
		public static float RenderAlpha;

		/// <summary>
		/// Maximum value that DeltaTime can be. This can be useful to prevent physics from breaking when dragging
		/// the game window or if your game hitches.
		/// </summary>
		public static float MaxDeltaTime = float.MaxValue;

		internal static void Update(float dt)
		{
			if(dt > MaxDeltaTime)
				dt = MaxDeltaTime;
			TotalTime += dt;
			DeltaTime = dt * TimeScale;
			AltDeltaTime = dt * AltTimeScale;
			UnscaledDeltaTime = dt;
			TimeSinceSceneLoad += dt;
			FrameCount++;
		}


		internal static void SceneChanged()
		{
			TimeSinceSceneLoad = 0f;
		}


		/// <summary>
		/// Allows to check in intervals. Should only be used with interval values above deltaTime,
		/// otherwise it will always return true.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CheckEvery(float interval)
		{
			// we subtract deltaTime since timeSinceSceneLoad already includes this update ticks deltaTime
			return (int) (TimeSinceSceneLoad / interval) > (int) ((TimeSinceSceneLoad - DeltaTime) / interval);
		}
	}
}
