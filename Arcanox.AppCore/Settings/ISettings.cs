using JetBrains.Annotations;

namespace Arcanox.AppCore.Settings;

[PublicAPI]
public interface ISettings
{
	/// <summary>
	/// Copies all settings properties from this instance to the provided <paramref name="other"/> instance.
	/// </summary>
	void CopyTo(ISettings other);
}
