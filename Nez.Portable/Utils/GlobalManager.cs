namespace Nez
{
	public enum GlobalManagerUpdateMode
	{
		/// <summary>
		/// updated once per simulation tick, inside the substepped loop (gameplay time)
		/// </summary>
		Tick,

		/// <summary>
		/// updated exactly once per rendered frame, before the tick loop (presentation time)
		/// </summary>
		Frame
	}

	public class GlobalManager
	{
		/// <summary>
		/// controls whether this manager updates per simulation tick or per rendered frame when
		/// Core.UseSubsteppedLoop is enabled. With the substepped loop disabled the two are equivalent.
		/// </summary>
		public virtual GlobalManagerUpdateMode UpdateMode => GlobalManagerUpdateMode.Tick;

		/// <summary>
		/// true if the GlobalManager is enabled. Changes in state result in OnEnabled/OnDisable being called.
		/// </summary>
		/// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
		public bool Enabled
		{
			get => _enabled;
			set => SetEnabled(value);
		}

		/// <summary>
		/// enables/disables this GlobalManager
		/// </summary>
		/// <returns>The enabled.</returns>
		/// <param name="isEnabled">If set to <c>true</c> is enabled.</param>
		public void SetEnabled(bool isEnabled)
		{
			if (_enabled != isEnabled)
			{
				_enabled = isEnabled;

				if (_enabled)
					OnEnabled();
				else
					OnDisabled();
			}
		}

		bool _enabled;


		#region GlobalManager Lifecycle

		/// <summary>
		/// called when this GlobalManager is enabled
		/// </summary>
		public virtual void OnEnabled()
		{
		}

		/// <summary>
		/// called when the this GlobalManager is disabled
		/// </summary>
		public virtual void OnDisabled()
		{
		}

		/// <summary>
		/// called each frame before Scene.update
		/// </summary>
		public virtual void Update()
		{
		}

		#endregion
	}
}