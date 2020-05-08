using Amazon.S3.Encryption.Internal;
using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;

namespace PlayTogetherApi
{
	public abstract class CountingObservableBase<T> : IObservable<T>
	{
		private int _subscriberCount;
		protected ISubject<T> InternalSubject { get; private set; }
		public int SubscriberCount => _subscriberCount;

		public IDisposable Subscribe(Action<T> observer)
		{
			if (1 == Interlocked.Increment(ref _subscriberCount))
			{
				InternalSubject = Setup();
			}

			return new CompositeDisposable(
				InternalSubject.Subscribe(observer),
				Disposable.Create(() =>
				{
					if (0 == Interlocked.Decrement(ref _subscriberCount))
					{
						Teardown(InternalSubject);
						InternalSubject = null;
					}
				}
			));
		}

		public IDisposable Subscribe(IObserver<T> observer)
		{
			if (1 == Interlocked.Increment(ref _subscriberCount))
			{
				InternalSubject = Setup();
			}

			return new CompositeDisposable(
				InternalSubject.Subscribe(observer),
				Disposable.Create(() =>
				{
					if (0 == Interlocked.Decrement(ref _subscriberCount))
					{
						Teardown(InternalSubject);
						InternalSubject = null;
					}
				}
			));
		}

		protected abstract ISubject<T> Setup();

		protected abstract void Teardown(ISubject<T> subject);
	}
}
