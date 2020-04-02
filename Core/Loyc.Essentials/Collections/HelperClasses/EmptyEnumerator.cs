// This file is part of the Loyc project. Licence: LGPL
using System;
using System.Collections;
using System.Collections.Generic;

namespace Loyc.Collections
{
	/// <summary>Helper class: an empty enumerator.</summary>
	[Serializable]
	public class EmptyEnumerator<T> : IEnumerator<T>, IEnumerator
	{
		public static readonly IEnumerator<T> Value = new EmptyEnumerator<T>();

		public T Current { get { return default(T); } }
		object IEnumerator.Current { get { return this.Current; } }
		public void Dispose() { }
		public bool MoveNext() { return false; }
		public void Reset() { }
	}
}
