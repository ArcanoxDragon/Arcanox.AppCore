using Microsoft.Extensions.Primitives;

namespace Arcanox.AppCore.Settings;

internal class SettingsChangeToken : IChangeToken, IDisposable
{
	private readonly CancellationTokenSource cts;
	private readonly CancellationChangeToken innerToken;

	public SettingsChangeToken()
	{
		this.cts = new CancellationTokenSource();
		this.innerToken = new CancellationChangeToken(this.cts.Token);
	}

	public bool HasChanged            => this.innerToken.HasChanged;
	public bool ActiveChangeCallbacks => this.innerToken.ActiveChangeCallbacks;

	public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
		=> this.innerToken.RegisterChangeCallback(callback, state);

	public void NotifyOfChange()
	{
		try
		{
			this.cts.Cancel();
		}
		catch (ObjectDisposedException)
		{
			// Don't care - this just means this token was swapped out for a different one
		}
	}

	public void Dispose()
	{
		this.cts.Dispose();
	}
}