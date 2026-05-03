using System.Reactive;
using System.Reactive.Linq;
using JetBrains.Annotations;
using ReactiveUI;

namespace Arcanox.AppCore.Reactive.Extensions;

[PublicAPI]
public static class ObservableExtensions
{
	extension<TParam, TResult>(IReactiveCommand<TParam, TResult> command)
	{
		/// <summary>
		/// Adds an exception handler to this <see cref="IReactiveCommand{TParam,TResult}"/> that will
		/// automatically re-subscribe after the <see cref="IHandleObservableErrors.ThrownExceptions"/>
		/// observable ends (i.e. after an exception occurs) so that subsequent exceptions are handled
		/// in the same manner.
		/// </summary>
		/// <param name="subscribeToExceptions">
		/// A function that will subscribe a handler to the <see cref="IHandleObservableErrors.ThrownExceptions"/>
		/// observable when the command is first subscribed, in addition to re-subscribing any time an exception
		/// occurs.
		/// </param>
		public IObservable<TResult> HandleExceptions(Func<IObservable<Exception?>, IDisposable> subscribeToExceptions)
			=> Observable.Using(
				() => subscribeToExceptions(command.ThrownExceptions),
				_ => command.Catch(Observable.Empty<TResult>())
			);

		/// <summary>
		/// Adds an exception handler to this <see cref="IReactiveCommand{TParam,TResult}"/> that will
		/// be invoked any time an exception is thrown in the command. The handler will automatically
		/// be re-subscribed after each exception so that subsequent exceptions are also handled (and
		/// not just the first one).
		/// </summary>
		/// <param name="onException">
		/// A function that will be invoked each time an exception is thrown in the command. The function
		/// should return a <see cref="IObservable{T}"/> of type <see cref="Unit"/> that will act as a
		/// "continuation" of the exception observable.
		/// </param>
		public IObservable<TResult> HandleExceptionsWith(Func<Exception, IObservable<Unit>> onException)
			=> command.HandleExceptions(exceptions => exceptions.WhereNotNull().SelectMany(onException).Subscribe());

		/// <inheritdoc cref="HandleExceptionsWith{TParam,TResult}(IReactiveCommand{TParam,TResult},Func{Exception,IObservable{Unit}})"/>
		/// <param name="onException">
		/// A function that will be invoked each time an exception is thrown in the command.
		/// </param>
		public IObservable<TResult> HandleExceptionsWith(Action<Exception> onException)
			=> command.HandleExceptions(exceptions => exceptions.WhereNotNull().SelectMany(exception => {
				onException(exception);
				return Observable.Return(Unit.Default);
			}).Subscribe());

		/// <inheritdoc cref="HandleExceptionsWith{TParam,TResult}(IReactiveCommand{TParam,TResult},Func{Exception,IObservable{Unit}})"/>
		/// <param name="onException">
		/// An asynchronous function that will be invoked each time an exception is thrown in the command.
		/// </param>
		public IObservable<TResult> HandleExceptionsWith(Func<Exception, Task> onException)
			=> command.HandleExceptions(exceptions => exceptions.WhereNotNull().SelectMany(exception => {
				return Observable.FromAsync(() => onException(exception));
			}).Subscribe());
	}
}