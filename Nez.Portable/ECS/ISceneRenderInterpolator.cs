namespace Nez
{
	/// <summary>
	/// hook for render interpolation under Core.UseSubsteppedLoop. OnTickEnd is called after each completed
	/// simulation tick (all movement done); Apply/Restore bracket the whole render in Core.Draw — Apply moves
	/// state to the interpolated pose for Time.RenderAlpha, Restore must put back the exact tick values before
	/// the next tick runs.
	/// </summary>
	public interface ISceneRenderInterpolator
	{
		void OnTickEnd();
		void Apply(float alpha);
		void Restore();
	}
}
