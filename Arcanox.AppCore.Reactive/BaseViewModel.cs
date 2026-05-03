using System.Reactive.Concurrency;
using ReactiveUI;

namespace Arcanox.AppCore.Reactive;

public abstract class BaseViewModel : ReactiveObject, IActivatableViewModel
{
	protected static IScheduler MainThreadScheduler => RxSchedulers.MainThreadScheduler;
	protected static IScheduler TaskPoolScheduler   => RxSchedulers.TaskpoolScheduler;

	public ViewModelActivator Activator { get; } = new();
}