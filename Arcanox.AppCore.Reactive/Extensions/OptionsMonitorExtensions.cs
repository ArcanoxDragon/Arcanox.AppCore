using System.Reactive.Linq;
using Arcanox.AppCore.Reactive.Internal;
using Microsoft.Extensions.Options;

namespace Arcanox.AppCore.Reactive.Extensions;

public static class OptionsMonitorExtensions
{
	extension<T>(IOptionsMonitor<T> optionsMonitor)
	{
		/// <summary>
		/// Returns an <see cref="IObservable{T}"/> that will produce a new value
		/// any time the <see cref="IOptionsMonitor{TOptions}"/>'s options change.
		/// The value produced will be a snapshot of the options after the change.
		/// </summary>
		public IObservable<T> WhenChanged()
			=> Observable.Using(
					() => new OptionsMonitorSubject<T>(optionsMonitor),
					subject => subject.Observable)
				.StartWith(optionsMonitor.CurrentValue);
	}
}