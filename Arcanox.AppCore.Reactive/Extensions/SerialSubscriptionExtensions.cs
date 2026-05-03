using System.Reactive.Disposables;
using JetBrains.Annotations;

namespace Arcanox.AppCore.Reactive.Extensions;

[PublicAPI]
public static class SerialSubscriptionExtensions
{
	/// <summary>
	/// Automatically subscribes each instance of <typeparamref name="T"/> that this <see cref="IObservable{T}"/>
	/// produces using the provided <see cref="SerialSubscription{T}"/>. If the observable is completed, or if the
	/// returned <see cref="IDisposable"/> is disposed, any subscribed subject will be unsubscribed automatically.
	/// </summary>
	public static IDisposable SubscribeWith<T>(this IObservable<T?> observable, SerialSubscription<T> subscription)
	where T : class
	{
		IDisposable subscriptionDisposable = null!;

		subscriptionDisposable = observable.Subscribe(
			onNext: subscription.Subscribe,
			onError: _ => { },
			onCompleted: UnsubscribeAndDispose);

		return Disposable.Create(UnsubscribeAndDispose);

		void UnsubscribeAndDispose()
		{
			// ReSharper disable once AccessToModifiedClosure
			subscriptionDisposable.Dispose();
			subscription.Unsubscribe();
		}
	}
}