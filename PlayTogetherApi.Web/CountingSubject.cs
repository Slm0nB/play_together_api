using Amazon.S3.Encryption.Internal;
using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;

namespace PlayTogetherApi
{
	public class CountingSubject<T> : CountingObservableBase<T>, ISubject<T>
	{
		readonly Action cleanup;

		public CountingSubject(Action cleanup)
		{
			this.cleanup = cleanup;
		}

		protected override ISubject<T> Setup() => new Subject<T>();

		protected override void Teardown() => cleanup?.Invoke();

		public void OnNext(T value) => InternalSubject.OnNext(value);

		public void OnCompleted() => InternalSubject.OnCompleted();

		public void OnError(Exception ex) => InternalSubject.OnError(ex);
	}
}
