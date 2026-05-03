namespace Arcanox.AppCore.Settings;

public interface ISettingsManager<out TSettings>
where TSettings : class
{
	/// <summary>
	/// Modifies the persisted instance of <typeparamref name="TSettings"/>, saves
	/// the changes to the settings file, and notifies all consumers that the instance
	/// has been changed.
	/// </summary>
	void Modify(Action<TSettings> modifyAction);

	/// <inheritdoc cref="Modify"/>
	Task ModifyAsync(Action<TSettings> modifyAction);
}