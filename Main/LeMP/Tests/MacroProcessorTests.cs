using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc;
using S = Loyc.Syntax.CodeSymbols;
using Loyc.Ecs;

/// <summary>Contains tests for the <see cref="LeMP.MacroProcessor"/> and for standard LeMP macros.</summary>
namespace LeMP.Tests
{
	[ContainsMacros]
	public class TestMacros
	{
		[LexicalMacro("splice(args...)", "Expanded args in-place (kinda pointless?) for testing")]
		public static LNode splice(LNode node, IMessageSink sink)
		{
			return node.WithName(S.Splice);
		}
		[LexicalMacro("priorityTest(x, y)", "Change first argument to 'hi'",
			"priorityTest", "priorityTestPCB", Mode = MacroMode.PriorityOverride | MacroMode.Passive)]
		public static LNode priorityTestHi(LNode node, IMessageSink sink)
		{
			if (node.ArgCount >= 1 && !node[0].IsIdNamed("hi"))
				return node.WithArgChanged(0, LNode.Id("hi"));
			return null;
		}
		[LexicalMacro("priorityTest(x, y)", "Swap arg 0 and arg 1", Mode = MacroMode.ProcessChildrenAfter | MacroMode.Passive)]
		public static LNode priorityTest(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2)
				return node.WithArgs(node[1], node[0]);
			return null;
		}
		[LexicalMacro("priorityTestPCB(x, y)", "Swap arg 0 and arg 1", Mode = MacroMode.ProcessChildrenBefore | MacroMode.Passive)]
		public static LNode priorityTestPCB(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2)
				return node.WithArgs(node[1], node[0]);
			return null;
		}
		[LexicalMacro("braceTheRest", "Put the rest of the statements in braces", Mode = MacroMode.MatchIdentifier)]
		public static LNode braceTheRest(LNode node, IMacroContext context)
		{
			context.DropRemainingNodes = true;
			return LNode.Call(S.Braces, LNode.List(context.RemainingNodes));
		}

		[LexicalMacro("replaceTarget(oldTarget, newTarget)", "Register a macro that changes its target from one identifier to another.")]
		public static LNode replaceTarget(LNode outerNode, IMacroContext context1)
		{
			// Test the direct way to register a macro
			context1.RegisterMacro(
				new MacroInfo(null, outerNode[0].Name.Name,
					(node, context2) =>
					{
						return node.WithTarget(outerNode[1]);
					}));
			return LNode.Call(S.Splice);
		}

		[LexicalMacro("overrideTarget(oldTarget, newTarget)", "Register a macro that changes its target from one identifier to another.")]
		public static LNode overrideTarget(LNode outerNode, IMacroContext context1)
		{
			// Test the indirect way to register a macro
			return LNode.Call((Symbol)"#registerMacro", LNode.List(LNode.Literal(
				new MacroInfo(null, outerNode[0].Name.Name,
					(node, context2) =>
					{
						return node.WithTarget(outerNode[1]);
					}) {
						Mode = MacroMode.PriorityOverride // needed by the unit test
					})));
		}

		[LexicalMacro("bob", "= bobby")]
		public static LNode bob(LNode node, IMacroContext context)
		{
			return LNode.Id("bobby");
		}
		[LexicalMacro("tom", "= tommy", Mode = MacroMode.MatchIdentifier)]
		public static LNode tom(LNode node, IMacroContext context)
		{
			return LNode.Id("tommy");
		}
	}

	namespace A
	{
		[ContainsMacros]
		public class AliasTest
		{
			[LexicalMacro("", "", "uppercasemacro", "UpperCaseMacro", Mode = MacroMode.MatchIdentifier)]
			public static LNode uppercasemacro(LNode node, IMacroContext context)
			{
				return node.IsId ? LNode.Id("UPPERCASE") : null;
			}
		}
	}
	namespace B
	{
		[ContainsMacros]
		public class AliasTest
		{
			public static List<MacroInfo> Aliases()
			{
				return MacroInfo.GetMacros(typeof(A.AliasTest), null, (Symbol)"LeMP.Tests.B").ToList();
			}
		}
	}
}
namespace LeMP
{
	/// <summary>A simple version of Compiler that takes a single input and produces 
	/// a StringBuilder. Pre-opens LeMP.Prelude namespaces.</summary>
	public class TestCompiler : Compiler
	{
		public TestCompiler(IMessageSink sink, ICharSource text, params string[] preOpenedNamespaces)
			: base(sink, typeof(LeMP.Prelude.BuiltinMacros), new[] { new InputOutput(text, "TEST") })
		{
			Parallel = false;
			MacroProcessor.AddMacros(typeof(LeMP.Prelude.Les.Macros));
			MacroProcessor.AddMacros(typeof(LeMP.Prelude.Les3.Macros));
			MacroProcessor.AddMacros(typeof(LeMP.Tests.TestMacros));
			MacroProcessor.AddMacros(typeof(LeMP.Tests.A.AliasTest));
			MacroProcessor.AddMacros(typeof(LeMP.Tests.B.AliasTest));
			MacroProcessor.PreOpenedNamespaces.Add((Symbol)"LeMP.Prelude");
			foreach (var ns in preOpenedNamespaces)
				MacroProcessor.PreOpenedNamespaces.Add((Symbol)ns);
		}

		public StringBuilder Output;
		public VList<LNode> Results;

		protected override void WriteOutput(InputOutput io)
		{
			Results = io.Output;
			Output = new StringBuilder();
			io.OutPrinter.Print(Results, Output, Sink, null, io.OutOptions);
		}

		#region static Test(), StripExtraWhitespace() methods

		public static void Test(string input, string output, IMessageSink sink, int maxExpand = 0xFFFF, params string[] preOpenedNamespaces)
		{
			var c = new TestCompiler(sink, new UString(input), preOpenedNamespaces);
			c.MaxExpansions = maxExpand;
			c.MacroProcessor.AbortTimeout = TimeSpan.Zero; // never timeout (avoids spawning a new thread)
			c.Run();
			Assert.AreEqual(StripExtraWhitespace(output), StripExtraWhitespace(c.Output.ToString()));
		}

		static readonly string[] CommentPrefix = new[] { "//" };
		/// <summary>Strips whitespace and single-line comments from a string.
		/// Helps test whether two blocks of code are "sufficiently equal".</summary>
		public static string StripExtraWhitespace(string a, string[] commentPrefixes = null)
		{
			commentPrefixes = commentPrefixes ?? CommentPrefix;
			StringBuilder sb = new StringBuilder();
			char prev_c = '\0';
			for (int i = 0; i < a.Length; i++)
			{
				char c = a[i];

				var slice = a.Slice(i);
				for (int cp = 0; cp < commentPrefixes.Length; cp++)
				{
					if (slice.StartsWith(commentPrefixes[cp]))
					{
						do ++i; while (i < a.Length && (c = a[i]) != '\n' && c != '\r');
						break;
					}
				}

				if (c == '\n' || c == '\r' || c == '\t')
					c = ' ';
				if (c == ' ' && (!MaybeId(prev_c) || !MaybeId(a.TryGet(i + 1, '\0'))))
					continue;

				sb.Append(c);
				prev_c = c;
			}
			return sb.ToString();
		}
		static bool MaybeId(char c) { return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'); }

		#endregion
	}

	[TestFixture]
	public class MacroProcessorTests
	{
		[Test]
		public void TrivialTest()
		{
			Test("no macros.apply here;",   // LES input
				"no(macros.apply, here);"); // C# output
			Test("while (@true) {};",       // LES input
				"while ((true)) {}");       // C# output
		}

		[Test]
		public void ExpandLimit()
		{
			Test("{ x::Foo; y::int; }",
				"{ Foo x; int y; }", 2);
			Test("{ x::Foo; y::int; }",
				"{ Foo x; @int y; }", 1);
			Test("@[static] fn Main()::void { var x::int = `default` int; }",
				"[@static] @void Main() { var(x::@int = @default(@int)); }", 1);
			Test("@[static] fn Main()::void { var x::int = `default` int; }",
				"static void Main() { @int x = @default(@int); }", 2);
			Test("@[static] fn Main()::void { var x::int = `default` int; }",
				"static void Main() { int x = default(@int); }", 3);
			Test("@[static] fn Main()::void { var x::int = `default` int; }",
				"static void Main() { int x = default(int); }", 4);
		}

		[Test]
		public void JustSpliceTest()
		{
			Test("the(#splice(nonmacro, inserts, stuff, in-place), here);",
				"the (nonmacro, inserts, stuff, @in-place, here);");
			Test("A(#splice()); B(#splice(x), #splice()); C(#splice(), #splice(y))",
				"A(); B(x); C(y);");
			Test("A(#splice(x), #splice(y, z)); B(#splice(x, y), #splice(z))",
				"A(x, y, z); B(x, y, z);");
		}

		[Test]
		public void NoLexicalMacrosTest()
		{
			Test("#noLexicalMacros(blocks macros, e.g. break, return Foo);",
				"blocks(macros); e.g.@break; @return(Foo);");
		}

		[Test]
		public void ImportsTest()
		{
			Test("import_macros LeMP.Tests; x();",
				"x();");
			Test("import x.y;",
				"using x.y;");
			Test("splice(x); { import LeMP.Tests; splice(x); }; splice(x);",
				"splice(x); { using LeMP.Tests; x; } splice(x);");
			Test("import_macros LeMP.Tests; A(splice(x), splice(y, z)); B(splice(x, y), splice(z))",
				"A(x, y, z); B(x, y, z);");
			Test("{{ import LeMP.Tests; splice(x); }}; splice(x);",
				 "{{ using LeMP.Tests; x; }} splice(x);");
		}

		[Test]
		public void PriorityTest()
		{
			Test("import_macros LeMP.Tests; priorityTest(0, 1);",
										   "priorityTest(1, hi);");
			Test("{ import_macros LeMP.Tests; foo0(); priorityTest(0, 2); foo(); }",
				 "{                           foo0(); priorityTest(2, hi); foo(); }");
			Test("{ import_macros LeMP.Tests; priorityTest(0, x::int = 3); foo(); }",
				 "{                           priorityTest(int x = 3, hi); foo(); }");
			Test("{ import_macros LeMP.Tests; priorityTestPCB(0, x::int = 4); foo2(); }",
				 "{                           priorityTestPCB(int x = 4, hi); foo2(); }");
		}

		[Test]
		public void SpliceTheBrace()
		{
			Test("import_macros LeMP.Tests; f(x); braceTheRest; g(y); h(z);",
				 "f(x); { g(y); h(z); }");
			// Test that MacroProcessorTask properly includes stuff outside 
			// the #splice in the RemainingNodes list.
			Test("import_macros LeMP.Tests; f(x); #splice(braceTheRest; g(y)); h(z);",
				 "f(x); { g(y); h(z); }");
			Test("import_macros LeMP.Tests; f(x);  splice(braceTheRest; g(y)); h(z);",
				 "f(x); { g(y); h(z); }");
			Test("import_macros LeMP.Tests; splice(f(x); braceTheRest); g(y); h(z);",
				 "f(x); { g(y); h(z); }");
		}

		[Test]
		public void RegisterMacros()
		{
			Test("import_macros LeMP.Tests; replaceTarget(John, Steve); John(John(1));",
				 "Steve(Steve(1));");
			// Make sure that scoping works
			Test("{ import_macros LeMP.Tests; Hi(1); replaceTarget(Hi, Hello); { Hi(2); overrideTarget(Hi, Bye); Hi(3); }; Hi(4); }",
				 "{ Hi(1); { Hello(2); Bye(3); } Hello(4); }");
		}

		[Test]
		public void MatchIdentifierTest()
		{
			Test("import_macros LeMP.Tests; bob(); tom();", "bobby; tommy;");
			Test("import_macros LeMP.Tests; bob; tom;", "bob; tommy;");
		}

		[Test]
		public void MacroAliasedAcrossNamespaces()
		{
			Test("#importMacros(LeMP.Tests.A); uppercasemacro;", "UPPERCASE;");
			Test("#importMacros(LeMP.Tests.A); UpperCaseMacro;", "UPPERCASE;");
			Test("#importMacros(LeMP.Tests.B); uppercasemacro;", "UPPERCASE;");
			Test("#importMacros(LeMP.Tests.B); UpperCaseMacro;", "UPPERCASE;");
			Test("#importMacros(LeMP.Tests.A); import_macros LeMP.Tests.B; uppercasemacro;", "UPPERCASE;");
			Test("#importMacros(LeMP.Tests.A); import_macros LeMP.Tests.B; UpperCaseMacro;", "UPPERCASE;");
		}

		SeverityMessageFilter _sink = new SeverityMessageFilter(ConsoleMessageSink.Value, Severity.DebugDetail);

		private void Test(string input, string output, int maxExpand = 0xFFFF)
		{
			using (LNode.SetPrinter(EcsLanguageService.WithPlainCSharpPrinter))
				TestCompiler.Test(input, output, _sink, maxExpand, "LeMP.Prelude.Les");
		}
	}
}
