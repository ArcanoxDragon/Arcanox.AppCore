using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Arcanox.AppCore.Extensions;
using Arcanox.AppCore.Internal.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Arcanox.AppCore.Settings;

internal class SettingsManager<TSettings> : ISettingsManager<TSettings>,
											IConfigureOptions<TSettings>,
											IOptionsChangeTokenSource<TSettings>
where TSettings : class, ISettings, new()
{
	private readonly Lock                    loadSettingsLock = new();
	private readonly string                  settingsTypeName;
	private readonly ILogger                 logger;
	private readonly string                  appDataFolderName;
	private readonly string                  settingsFileName;
	private readonly JsonTypeInfo<TSettings> settingsTypeInfo;

	private TSettings           settings    = new();
	private SettingsChangeToken changeToken = new();
	private bool                isLoaded;

	public SettingsManager(
		IOptions<ApplicationIdentityOptions> applicationIdentityOptions,
		IOptionsFactory<SettingsOptions> settingsOptionsFactory,
		ILogger<SettingsManager<TSettings>>? logger = null)
	{
		this.settingsTypeName = typeof(TSettings).FullName!;
		this.logger = (ILogger?) logger ?? NullLogger.Instance;

		var settingsOptions = settingsOptionsFactory.Create(this.settingsTypeName);

		if (string.IsNullOrEmpty(applicationIdentityOptions.Value.AppDataFolderName))
			throw new ApplicationException($"An AppData folder name was not configured. Make sure {nameof(ServiceCollectionExtensions.ConfigureAppDataFolder)} " +
										   $"was called on the IoC service collection when building.");
		if (string.IsNullOrEmpty(settingsOptions.SettingsFileName))
			throw new ApplicationException($"An invalid settings file name was provided for settings type \"{this.settingsTypeName}\"");
		if (settingsOptions.SettingsTypeInfo is not JsonTypeInfo<TSettings> settingsTypeInfo)
			throw new ApplicationException($"Invalid {nameof(JsonTypeInfo)} provided for \"{this.settingsTypeName}\"");

		this.appDataFolderName = applicationIdentityOptions.Value.AppDataFolderName;
		this.settingsFileName = settingsOptions.SettingsFileName;
		this.settingsTypeInfo = settingsTypeInfo;
	}

	#region IConfigureOptions

	public void Configure(TSettings options)
	{
		// Copy all values from our private instance to the instance being configured
		this.settings.CopyTo(options);
	}

	#endregion

	#region IOptionsChangeTokenSource

	public string Name => Options.DefaultName;

	public IChangeToken GetChangeToken()
		=> this.changeToken;

	#endregion

	#region ISettingsManager

	public void Modify(Action<TSettings> modifyAction)
	{
		EnsureLoaded();
		modifyAction(this.settings);
		Save();
		OnChanged();
	}

	public async Task ModifyAsync(Action<TSettings> modifyAction)
	{
		await EnsureLoadedAsync();
		modifyAction(this.settings);
		await SaveAsync();
		OnChanged();
	}

	#endregion

	#region Private Methods

	private void Save()
	{
		var settingsFilePath = GetSettingsPath(create: true);

		try
		{
			using var fileStream = File.Open(settingsFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);

			JsonSerializer.Serialize(fileStream, this.settings, this.settingsTypeInfo);
		}
		catch (Exception ex)
		{
			this.logger.LogError(ex, "Failed to save application settings to \"{FilePath}\"", settingsFilePath);
		}
	}

	private async ValueTask SaveAsync()
	{
		var settingsFilePath = GetSettingsPath(create: true);

		try
		{
			await using var fileStream = File.Open(settingsFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);

			await JsonSerializer.SerializeAsync(fileStream, this.settings, this.settingsTypeInfo);
		}
		catch (Exception ex)
		{
			this.logger.LogError(ex, "Failed to save application settings to \"{FilePath}\"", settingsFilePath);
		}
	}

	private void OnChanged()
	{
		var previousChangeToken = Interlocked.Exchange(ref this.changeToken, new SettingsChangeToken());
		previousChangeToken.NotifyOfChange();
		previousChangeToken.Dispose();
	}

	private void EnsureLoaded()
	{
		if (this.isLoaded)
			return;

		lock (this.loadSettingsLock)
		{
			// Check again in case someone else loaded while we were waiting for the lock
			if (this.isLoaded)
				return;

			var settingsFilePath = GetSettingsPath();

			if (File.Exists(settingsFilePath))
			{
				try
				{
					using var fileStream = File.Open(settingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
					var settings = JsonSerializer.Deserialize(fileStream, this.settingsTypeInfo);

					this.settings = settings ?? new TSettings();
				}
				catch (Exception ex)
				{
					this.logger.LogError(ex, "Failed to load application settings from \"{FilePath}\"", settingsFilePath);
				}
			}

			this.isLoaded = true;
		}
	}

	private async ValueTask EnsureLoadedAsync()
	{
		if (this.isLoaded)
			return;

		var settingsFilePath = GetSettingsPath();

		if (File.Exists(settingsFilePath))
		{
			try
			{
				await using var fileStream = File.Open(settingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
				var settings = await JsonSerializer.DeserializeAsync(fileStream, this.settingsTypeInfo);

				this.settings = settings ?? new TSettings();
			}
			catch (Exception ex)
			{
				this.logger.LogError(ex, "Failed to load application settings from \"{FilePath}\"", settingsFilePath);
			}
		}

		this.isLoaded = true;
	}

	private string GetSettingsPath(bool create = false)
	{
		var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		var ourAppDataFolder = Path.Join(appDataFolder, this.appDataFolderName);

		if (create)
			Directory.CreateDirectory(ourAppDataFolder);

		return Path.Join(ourAppDataFolder, this.settingsFileName);
	}

	#endregion
}