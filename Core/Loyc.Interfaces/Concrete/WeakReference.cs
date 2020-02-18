using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc
{
	// Not in System namespace, to reduce probability of conflict with .NET 4.5+ DLLs
	#if DotNet4 && !DotNet45
	// WeakReference<T> is not derived from WeakReference in .NET 4.5, but this class
	// does to avoid allocating two separate heap objects. Note that the serialized 
	// form will break when upgrading to .NET 4.5.
	[Serializable]
	public sealed class WeakReference<T> : WeakReference where T : class
	{
		public WeakReference(T target) : base(target) { }
		public WeakReference(T target, bool trackResurrection) : base(target, trackResurrection) { }

		// Shockingly, when .NET 4.5 introduced WeakReference<T> it did not include
		// the Target and IsAlive properties. Therefore I've deleted the Target 
		// property to avoid future compatibility problems. Use the Target() 
		// and IsAlive() extension methods instead.
		//public new T Target { get; set; }

		public void SetTarget(T value) { base.Target = value; }
		public bool TryGetTarget(out T target) { target = (T)base.Target; return target != null; }
	}
	#endif
}
