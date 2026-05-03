using System.Reactive.Subjects;
using Microsoft.Extensions.Options;

namespace Arcanox.AppCore.Reactive.Internal;

internal class OptionsMonitorSubject<T> : IDisposable
{
	private readonly Subject<T>   subject = new();
	private readonly IDisposable? subscription;

	public OptionsMonitorSubject(IOptionsMonitor<T> optionsMonitor)
	{
		this.subscription = optionsMonitor.OnChange(OnChange);
	}

	public IObservable<T> Observable => this.subject;

	public void Dispose()
	{
		this.subscription?.Dispose();
		this.subject.Dispose();
	}

	private void OnChange(T value)
	{
		this.subject.OnNext(value);
	}
}