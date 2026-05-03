using System.Reactive.Disposables;
using JetBrains.Annotations;

namespace Arcanox.AppCore.Reactive;

/// <summary>
/// A function that will subscribe a subject of type <typeparamref name="T"/>, disposing all
/// subscriptions with the provided <paramref name="disposables"/>.
/// </summary>
public delegate void SubscribeDelegate<in T>(T subject, CompositeDisposable disposables);

/// <summary>
/// A helper class that can manage switchable instances of <typeparamref name="T"/>, where
/// each time a new instance is assigned or materialized, subscriptions are attached to the
/// new instance and the previous instance's subscriptions will be disposed.
/// </summary>
/// <param name="subscribe">
/// A function that will be called for each new instance of <typeparamref name="T"/> to
/// attach subscriptions to the instance.
/// </param>
[PublicAPI]
public sealed class SerialSubscription<T>(SubscribeDelegate<T> subscribe)
where T : class
{
	private volatile CompositeDisposable? disposables;

	/// <summary>
	/// Subscibes to the <paramref name="newSubject"/> using the configured delegate,
	/// and disposes of the subscriptions for the previous instance (if one had been
	/// subscribed before now).
	/// </summary>
	public void Subscribe(T? newSubject)
	{
		var newDisposables = newSubject is null ? null : new CompositeDisposable();
		var previousDisposables = Interlocked.Exchange(ref this.disposables, newDisposables);

		previousDisposables?.Dispose();

		if (newSubject is null)
			return;

		subscribe(newSubject, newDisposables!);
	}

	/// <summary>
	/// Unsubscribes the currently-subscribed instance without subscribing to a new one.
	/// </summary>
	public void Unsubscribe()
	{
		var previousDisposables = Interlocked.Exchange(ref this.disposables, null);

		previousDisposables?.Dispose();
	}
}