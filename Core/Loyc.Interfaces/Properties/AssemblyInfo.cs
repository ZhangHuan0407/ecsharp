#region Using directives

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#endregion

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Loyc.Interfaces")]
[assembly: AssemblyDescription("A library of interfaces used in the Loyc Core libraries. "+
	"Also contains a small number of essential structs, attribute types, enums, and delegates.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("David Piepgrass")]
[assembly: AssemblyProduct("Loyc.Interfaces")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// This sets the default COM visibility of types in the assembly to invisible.
// If you need to expose a type to COM, use [ComVisible(true)] on that type.
[assembly: ComVisible(false)]

[assembly: CLSCompliant(false)]

//[assembly: InternalsVisibleTo("LoycCore.Tests")] is not allowed in a signed assembly
[assembly: InternalsVisibleTo("LoycCore.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100adec5c8e52098b94dc60b34ac0916d307eec23b2c285a0beeb7168174fc1f6a71dcae43c88904e2907a12f66861de8d8f130c4f7b57cff0aea92ed06b50d96c63cea2ee19ec5d35a2946ddef3f35f0fbd3ec3a358b46fd05c82837c49d91694c1926935dc83e2a28c1ff077e4d8a5f679f1edb1c8a692aa2913d753ea05f4fba")]
