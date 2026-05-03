using System.Text.Json.Serialization.Metadata;
using Arcanox.AppCore.Internal.Options;
using Arcanox.AppCore.Settings;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Arcanox.AppCore.Extensions;

[PublicAPI]
public static class ServiceCollectionExtensions
{
	extension(IServiceCollection services)
	{
		/// <summary>
		/// Adds an alias for a service implementation that is already registered for another service type,
		/// allowing the alias to be used to resolve the existing implementation instance (e.g. for scoped
		/// or singleton services where a new instance should not be created).
		/// </summary>
		/// <typeparam name="TService">The service type for which the implementation is already registered.</typeparam>
		/// <typeparam name="TAlias">The alias via which the implementation may also be resolved.</typeparam>
		public IServiceCollection AddAlias<TService, TAlias>()
		where TService : class, TAlias
		where TAlias : class
		{
			services.TryAddEnumerable(ServiceDescriptor.Transient<TAlias, TService>(provider => provider.GetRequiredService<TService>()));
			return services;
		}

		/// <summary>
		/// Configures the name of the folder that the application will use inside the current user's AppData folder
		/// (or profile directory, on non-Windows platforms).
		/// </summary>
		public IServiceCollection ConfigureAppDataFolder(string appDataFolderName)
		{
			services.Configure<ApplicationIdentityOptions>(options => {
				options.AppDataFolderName = appDataFolderName;
			});
			return services;
		}

		#region Settings

		/// <summary>
		/// Adds an implementation of <see cref="ISettingsManager{TSettings}"/> that can be used to modify and save an instance of
		/// <typeparamref name="TSettings"/> to a JSON settings file in the application's AppData directory. The stored settings
		/// will be loaded automatically when the application is next launched, and can be accessed via the usual
		/// <see cref="IOptions{TOptions}"/> interfaces (with <typeparamref name="TSettings"/> as the options type).
		/// </summary>
		/// <typeparam name="TSettings">The class that will be serialized to and deserialized from the settings file.</typeparam>
		/// <param name="settingsFileName">The name of the settings file (which will be created in the application's AppData folder).</param>
		/// <param name="settingsTypeInfo">
		/// A <see cref="JsonTypeInfo{T}"/> for the <typeparamref name="TSettings"/> type that will be used to serialize
		/// and deserialize the settings class in a reflection-free manner (for compatibility with trimmed/AOT compilation).
		/// </param>
		public IServiceCollection AddSettingsManager<TSettings>(string settingsFileName, JsonTypeInfo<TSettings> settingsTypeInfo)
		where TSettings : class, ISettings<TSettings>, new()
		{
			var settingsTypeName = typeof(TSettings).FullName!;

			services.Configure<SettingsOptions>(settingsTypeName, options => {
				options.SettingsFileName = settingsFileName;
				options.SettingsTypeInfo = settingsTypeInfo;
			});
			services.AddSingleton<SettingsManager<TSettings>>();
			services.AddAlias<SettingsManager<TSettings>, ISettingsManager<TSettings>>();
			services.AddAlias<SettingsManager<TSettings>, IOptionsChangeTokenSource<TSettings>>();
			services.AddAlias<SettingsManager<TSettings>, IConfigureOptions<TSettings>>();
			return services;
		}

		#endregion
	}
}