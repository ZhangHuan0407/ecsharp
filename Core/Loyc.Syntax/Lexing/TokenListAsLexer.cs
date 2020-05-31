using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax.Lexing
{
	/// <summary>Adapter: converts <c>IEnumerable(Token)</c> to the <see cref="ILexer{Token}"/> interface.</summary>
	/// <remarks>
	/// The LineNumber property is computed on-demand by the <see cref="ISourceFile"/> provided.
	/// <para/>
	/// TODO: IndentLevel, IndentString do not work.
	/// </remarks>
	public class TokenListAsLexer : ILexer<Token>
	{
		public TokenListAsLexer(IEnumerable<Token> tokenList, ISourceFile sourceFile) : this(tokenList.GetEnumerator(), sourceFile) { }
		public TokenListAsLexer(IEnumerator<Token> tokenList, ISourceFile sourceFile) { _e = tokenList; _sourceFile = sourceFile; }

		IEnumerator<Token> _e;
		ISourceFile _sourceFile;
		Token _current;
		public Loyc.Syntax.ISourceFile SourceFile
		{
			get { return _sourceFile; }
		}

		public Maybe<Token> NextToken()
		{
			if (MoveNext())
				return _current;
			else
				return Maybe<Token>.NoValue;
		}

		public Loyc.IMessageSink ErrorSink { get; set; }

		public int IndentLevel { get { return 0; } } // TODO
		public UString IndentString
		{
			get { throw new NotImplementedException(); } // TODO
		}

		public int LineNumber
		{
			get { return _sourceFile.IndexToLine(_current.EndIndex).Line; }
		}
		public int InputPosition { get { return _current.EndIndex; } }

		public bool MoveNext()
		{
			if (_e.MoveNext()) {
				_current = _e.Current;
				return true;
			}
			return false;
		}
		public Token Current
		{
			get { return _current; }
		}
		object System.Collections.IEnumerator.Current
		{
			get { return _e.Current; }
		}
		void IDisposable.Dispose() { _e.Dispose(); }
		void IEnumerator.Reset() { _e.Reset(); }

		public string FileName
		{
			get { return SourceFile.FileName; }
		}
		public ILineColumnFile IndexToLine(int index)
		{
			return SourceFile.IndexToLine(index);
		}
	}
}
