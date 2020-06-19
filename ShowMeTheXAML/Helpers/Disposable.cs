using System;
using System.Collections.Generic;
using System.Text;

namespace ShowMeTheXAML.Helpers
{
	internal static class Disposable
	{
		public static IDisposable Create(Action onDispose) => new DisposableAction(onDispose);

		public static IDisposable Create(Action onCreate, Action onDispose)
		{
			onCreate?.Invoke();
			return new DisposableAction(onDispose);
		}

		private class DisposableAction : IDisposable
		{
			private readonly Action _action;

			public DisposableAction(Action action)
			{
				_action = action;
			}

			public void Dispose()
			{
				_action?.Invoke();
			}
		}
	}
}
