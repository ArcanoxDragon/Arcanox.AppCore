using System.Text.Json.Serialization.Metadata;

namespace Arcanox.AppCore.Internal.Options;

internal sealed class SettingsOptions
{
	public string?       SettingsFileName    { get; set; }
	public JsonTypeInfo? SettingsTypeInfo { get; set; }
}