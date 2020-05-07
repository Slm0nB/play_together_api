using Amazon.S3.Encryption.Internal;
using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;

namespace PlayTogetherApi
{
	public abstract class CountingObservableBase<T>
	{
		private ISubject<T> _internalSubject;
		private int _subscriberCount;

		public IDisposable Subscribe(Action<T> observer)
		{
			if (1 == Interlocked.Increment(ref _subscriberCount))
			{
				_internalSubject = Setup();
			}

			return new CompositeDisposable(
				_internalSubject.Subscribe(observer),
				Disposable.Create(() =>
				{
					if (0 == Interlocked.Decrement(ref _subscriberCount))
					{
						Teardown(_internalSubject);
						_internalSubject = null;
					}
				}
			));
		}

		protected abstract ISubject<T> Setup();

		protected abstract void Teardown(ISubject<T> subject);

		protected ISubject<T> InternalSubject => _internalSubject;

		public int SubscriberCount => _subscriberCount;
	}
}
