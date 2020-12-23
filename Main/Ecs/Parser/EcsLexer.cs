using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using Loyc;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Threading;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
using Loyc.Syntax.Les;
using System.Numerics;

namespace Loyc.Ecs.Parser
{
	using TT = TokenType;

	/// <summary>Lexer for EC# source code (see <see cref="ILexer{Token}"/>).</summary>
	/// <seealso cref="TokensToTree"/>
	public partial class EcsLexer : BaseLexer, ILexer<Token>
	{
		public EcsLexer(string text, IMessageSink sink) : base(new UString(text), "") { ErrorSink = sink; }
		public EcsLexer(ICharSource text, string fileName, IMessageSink sink, int startPosition = 0) : base(text, fileName, startPosition) { ErrorSink = sink; }

		public bool AllowNestedComments = false;
		private bool _isFloat, _parseNeeded, _verbatim;
		// Alternate: hex numbers, verbatim strings
		// UserFlag: bin numbers, double-verbatim
		private NodeStyle _style;
		private int _numberBase;
		private Symbol _typeSuffix;
		private TokenType _type; // predicted type of the current token
		private object _value;
		private int _startPosition;
		// _allowPPAt is used to detect whether a preprocessor directive is allowed
		// at the current input position. When _allowPPAt==_startPosition, it's allowed.
		private int _allowPPAt;

		public void Reset(ICharSource source, string fileName = "", int inputPosition = 0)
		{
			base.Reset(source, fileName, inputPosition, true);
		}

		public new ISourceFile SourceFile { get { return base.SourceFile; } }

		int _indentLevel;
		UString _indent;
		public int IndentLevel { get { return _indentLevel; } }
		public UString IndentString { get { return _indent; } }
		public int SpacesPerTab = 4;

		public Maybe<Token> NextToken()
		{
			int la1;
			if (LA0 == '\t' || LA0 == ' ')
				Spaces();
			else if (LA0 == '.' && InputPosition == _lineStartAt && ((la1 = LA(1)) == '\t' || la1 == ' '))
				DotIndent();
			_startPosition = InputPosition;
			_value = null;
			_style = 0;

			if (InputPosition >= CharSource.Count)
				return Maybe<Token>.NoValue;
			else {
				Token();
				Debug.Assert(InputPosition > _startPosition);
				return new Token((int)_type, _startPosition, InputPosition - _startPosition, _style, _value);
			}
		}

		protected override void Error(int lookaheadIndex, string message)
		{
			// the fast "blitting" code path may not be able to handle errors
			_parseNeeded = true;

			var pos = new SourceRange(SourceFile, InputPosition + lookaheadIndex);
			if (ErrorSink != null)
				ErrorSink.Error(pos, message);
			else
				throw new FormatException(pos + ": " + message);
		}
				
		public void Restart()
		{
			_indentLevel = 0;
			_lineNumber = 0;
			_allowPPAt = _lineStartAt = 0;
		}

		#region Value parsers
		// After the generated lexer code determines the boundaries of the token, 
		// one of these methods extracts the value of the token (e.g. "17L" => (long)17)
		// There are value parsers for identifiers, numbers, and strings; certain
		// parser cores are also accessible as public static methods.

		#region String parsing

		void ParseSQStringValue()
		{
			int len = InputPosition - _startPosition;
			if (!_parseNeeded && len == 3) {
				_value = CG.Cache(CharSource[_startPosition + 1]);
			} else {
				string s = ParseStringCore(_startPosition);
				_value = s;
				if (s.Length == 1)
					_value = CG.Cache(s[0]);
				else if (s.Length == 0)
					Error(_startPosition - InputPosition, "Empty character literal".Localized());
				else
					Error(_startPosition - InputPosition, "Character literal has {0} characters (there should be exactly one)".Localized(s.Length));
			}
		}

		void ParseBQStringValue()
		{
			var value = ParseStringCore(_startPosition);
			_value = GSymbol.Get(value.ToString());
		}

		void ParseStringValue()
		{
			_value = ParseStringCore(_startPosition);
			if (_value.ToString().Length < 16)
				_value = CG.Cache(_value);
		}

		string ParseStringCore(int start)
		{
			Debug.Assert(_verbatim == (CharSource[start] == '@'));
			if (_verbatim)
				start++;
			char q;
			Debug.Assert((q = CharSource.TryGet(start, '\0')) == '"' || q == '\'' || q == '`');
			bool tripleQuoted = (_style & NodeStyle.BaseStyleMask) == NodeStyle.TDQStringLiteral ||
			                    (_style & NodeStyle.BaseStyleMask) == NodeStyle.TQStringLiteral;

			string value;
			if (!_parseNeeded) {
				Debug.Assert(!tripleQuoted);
				value = (string)CharSource.Slice(start + 1, InputPosition - start - 2).ToString();
			} else {
				UString original = CharSource.Slice(start, InputPosition - start);
				value = UnescapeQuotedString(ref original, _verbatim, Error, _indent);
			}
			return value;
		}

		static string UnescapeQuotedString(ref UString source, bool isVerbatim, Action<int, string> onError, UString indentation)
		{
			Debug.Assert(source.Length >= 1);
			if (isVerbatim) {
				bool fail;
				char stringType = (char)source.PopFirst(out fail);
				StringBuilder sb = new StringBuilder();
				int c;
				for (;;) {
					c = source.PopFirst(out fail);
					if (fail) break;
					if (c == stringType) {
						if ((c = source.PopFirst(out fail)) != stringType)
							break;
					}
					sb.Append((char)c);
				}
				return sb.ToString();
			} else {
				// triple-quoted or normal string: let LES lexer handle it
				return Les3Lexer.UnescapeQuotedString(ref source, onError, indentation, true);
			}
		}

		#endregion

		#region Identifier & Symbol parsing (including public ParseIdentifier())

		// id & symbol cache. For Symbols, includes only one of the two @ signs.
		protected Dictionary<UString, object> _idCache = new Dictionary<UString, object>();

		void ParseIdValue(int skipAt, bool isBQString)
		{
			ParseIdOrSymbol(_startPosition + skipAt, isBQString);
		}
		void ParseSymbolValue(bool isBQString)
		{
			ParseIdOrSymbol(_startPosition + 2, isBQString);
		}

		void ParseIdOrSymbol(int start, bool isBQString)
		{
			UString unparsed = CharSource.Slice(start, InputPosition - start);
			UString parsed;
			Debug.Assert(isBQString == (CharSource.TryGet(start, '\0') == '`'));
			Debug.Assert(!_verbatim);
			if (!_idCache.TryGetValue(unparsed, out _value)) {
				if (isBQString)
					parsed = ParseStringCore(start);
				else if (_parseNeeded)
					parsed = ScanNormalIdentifier(unparsed);
				else
					parsed = unparsed;
				_idCache[unparsed.ShedExcessMemory(50)] = _value = GSymbol.Get(parsed.ToString());
			}
		}

		static string ScanNormalIdentifier(UString text)
		{
			var parsed = new StringBuilder();
			char c;
			while ((c = text[0, '\0']) != '\0') {
				if (!ScanUnicodeEscape(ref text, parsed, c)) {
					parsed.Append(c);
					text = text.Slice(1);
				}
			}
			return parsed.ToString();
		}
		static bool ScanUnicodeEscape(ref UString text, StringBuilder parsed, char c)
		{
			// I can't imagine why this exists in C# in the first place. Unicode 
			// escapes inside identifiers are required to be letters or digits,
			// although my lexer doesn't enforce this (EC# needs no such rule.)
			if (c != '\\')
				return false;
			char u = text.TryGet(1, '\0');
			int len = 4;
			if (u == 'u' || u == 'U') {
				if (u == 'U') len = 8;
				if (text.Length < 2 + len)
					return false;

				var digits = text.Substring(2, len);
				int code;
				if (ParseHelpers.TryParseHex(digits, out code) && code <= 0x0010FFFF) {
					if (code >= 0x10000) {
						parsed.Append((char)(0xD800 + ((code - 0x10000) >> 10)));
						parsed.Append((char)(0xDC00 + ((code - 0x10000) & 0x3FF)));
					} else
						parsed.Append((char)code);
					text = text.Substring(2 + len);
					return true;
				}
			}
			return false;
		}









		//private bool FindCurrentIdInKeywordTrie(Trie t, string source, int start, ref Symbol value, ref TokenType type)
		//{
		//    Debug.Assert(InputPosition >= start);
		//    for (int i = start, stop = InputPosition; i < stop; i++) {
		//        char input = source[i];
		//        int input_i = input - t.CharOffs;
		//        if (t.Child == null || (uint)input_i >= t.Child.Length) {
		//            if (input == '\'' && t.Value != null) {
		//                // Detected keyword followed by single quote. This requires 
		//                // the lexer to backtrack so that, for example, case'x' is 
		//                // treated as two tokens instead of the one token it 
		//                // initially appears to be.
		//                InputPosition = i;
		//                break;
		//            }
		//            return false;
		//        }
		//        if ((t = t.Child[input - t.CharOffs]) == null)
		//            return false;
		//    }
		//    if (t.Value != null) {
		//        value = t.Value;
		//        type = t.TokenType;
		//        return true;
		//    }
		//    return false;
		//}


		#endregion

		#region Number parsing

		static Symbol _sub = GSymbol.Get("-");
		static Symbol _F = GSymbol.Get("F");
		static Symbol _D = GSymbol.Get("D");
		static Symbol _M = GSymbol.Get("M");
		static Symbol _U = GSymbol.Get("U");
		static Symbol _L = GSymbol.Get("L");
		static Symbol _UL = GSymbol.Get("UL");
		static Symbol _Z = GSymbol.Get("Z");

		void ParseNumberValue()
		{
			int start = _startPosition;
			if (_numberBase != 10)
				start += 2;
			int stop = InputPosition;
			if (_typeSuffix != null)
				stop -= _typeSuffix.Name.Length;

			UString digits = CharSource.Slice(start, stop - start);
			string error;
			if ((_value = ParseNumberCore(digits, false, _numberBase, _isFloat, _typeSuffix, out error)) == null)
				_value = 0;
			else if (_value == CodeSymbols.Sub) {
				InputPosition = _startPosition + 1;
				_type = TT.Sub;
			}
			if (error != null)
				Error(_startPosition - InputPosition, error);
		}

		/////////////////////////////////////////////////////////////////////////////////
		// The following number-parsing methods were taken from LES2 (they were removed
		// from LES2 because of the switchover to uninterpreted literals)
		/////////////////////////////////////////////////////////////////////////////////

		/// <summary>Parses the digits of a literal (integer or floating-point),
		/// not including the radix prefix (0x, 0b) or type suffix (F, D, L, etc.)</summary>
		/// <param name="source">Digits of the number (not including radix prefix or type suffix)</param>
		/// <param name="isFloat">Whether the number is floating-point</param>
		/// <param name="numberBase">Radix. Must be 2 (binary), 10 (decimal) or 16 (hexadecimal).</param>
		/// <param name="typeSuffix">Type suffix: F, D, M, U, L, UL, or null.</param>
		/// <param name="error">Set to an error message in case of error.</param>
		/// <returns>Boxed value of the literal, null if total failure (result 
		/// is not null in case of overflow), or <see cref="CodeSymbols.Sub"/> (-)
		/// if isNegative is true but the type suffix is unsigned or the number 
		/// is larger than long.MaxValue.</returns>
		protected static object ParseNumberCore(UString source, bool isNegative, int numberBase, bool isFloat, Symbol typeSuffix, out string error)
		{
			error = null;
			if (!isFloat)
			{
				return ParseIntegerValue(source, isNegative, numberBase, typeSuffix, ref error);
			}
			else
			{
				if (numberBase == 10)
					return ParseNormalFloat(source, isNegative, typeSuffix, ref error);
				else
					return ParseSpecialFloatValue(source, isNegative, numberBase, typeSuffix, ref error);
			}
		}

		static object ParseBigIntegerValue(UString source, bool isNegative, int numberBase, ref string error)
		{
			BigInteger bigIntResult;
			bool overflow = !ParseHelpers.TryParseUInt(ref source, out bigIntResult, numberBase, ParseNumberFlag.SkipUnderscores);
			if (!source.IsEmpty)
			{
				// I'm not sure if this can ever happen
				error = Localize.Localized("Syntax error in integer literal");
			}

			// Overflow means that an out-of-memory exception has occurred.
			// This should be a rare sight indeed, though it's not impossible.
			if (overflow)
				error = Localize.Localized("Overflow in big integer literal (could not parse beyond {0}).", bigIntResult);

			// Optionally negate the result.
			if (isNegative)
				bigIntResult = -bigIntResult;

			return bigIntResult;
		}

		static object ParseIntegerValue(UString source, bool isNegative, int numberBase, Symbol typeSuffix, ref string error)
		{
			if (source.IsEmpty)
			{
				error = Localize.Localized("Syntax error in integer literal");
				return CG.Cache(0);
			}

			bool overflow;
			if (typeSuffix == _Z)
			{
				// Fast path for BigInteger values.
				return ParseBigIntegerValue(source, isNegative, numberBase, ref error);
			}

			// Create a copy of the input, in case we need to re-parse it as
			// a BigInteger.
			var srcCopy = source;

			// Parse the integer
			ulong unsigned;
			overflow = !ParseHelpers.TryParseUInt(ref source, out unsigned, numberBase, ParseNumberFlag.SkipUnderscores);
			if (!source.IsEmpty)
			{
				// I'm not sure if this can ever happen
				error = Localize.Localized("Syntax error in integer literal");
			}

			// If no suffix, automatically choose int, uint, long, ulong or BigInteger.
			if (typeSuffix == null)
			{
				if (overflow)
				{
					// If we tried to parse a plain integer literal (no suffix)
					// as a ulong, but failed due to overflow, then we'll parse
					// it as a BigInteger instead.
					return ParseBigIntegerValue(srcCopy, isNegative, numberBase, ref error);
				}
				else if (isNegative && -(long)unsigned > 0)
				{
					// We parsed a literal whose absolute value fits in a ulong,
					// but which cannot be represented as a long. Return a
					// BigInteger literal instead.
					return -new BigInteger(unsigned);
				}
				else if (unsigned > long.MaxValue)
					typeSuffix = _UL;
				else if (unsigned > uint.MaxValue)
					typeSuffix = _L;
				else if (unsigned > int.MaxValue)
					typeSuffix = isNegative ? _L : _U;
			}

			if (isNegative && (typeSuffix == _U || typeSuffix == _UL))
			{
				// Oops, an unsigned number can't be negative, so treat 
				// '-' as a separate token and let the number be reparsed.
				return CodeSymbols.Sub;
			}

			// Create boxed integer of the appropriate type 
			object value;
			if (typeSuffix == _UL)
			{
				value = unsigned;
				typeSuffix = null;
			}
			else if (typeSuffix == _U)
			{
				overflow = overflow || (uint)unsigned != unsigned;
				value = (uint)unsigned;
				typeSuffix = null;
			}
			else if (typeSuffix == _L)
			{
				if (isNegative)
				{
					overflow = overflow || -(long)unsigned > 0;
					value = -(long)unsigned;
				}
				else
				{
					overflow = overflow || (long)unsigned < 0;
					value = (long)unsigned;
				}
				typeSuffix = null;
			}
			else
			{
				value = isNegative ? -(int)unsigned : (int)unsigned;
			}

			if (overflow)
				error = Localize.Localized("Overflow in integer literal (the number is 0x{0:X} after binary truncation).", value);
			if (typeSuffix == null)
				return value;
			else
				return new CustomLiteral(value, typeSuffix);
		}

		static object ParseNormalFloat(UString source, bool isNegative, Symbol typeSuffix, ref string error)
		{
			string token = (string)source;
			token = token.Replace("_", "");
			if (typeSuffix == _F)
			{
				float f;
				if (float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
					return isNegative ? -f : f;
			}
			else if (typeSuffix == _M)
			{
				decimal m;
				if (decimal.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out m))
					return isNegative ? -m : m;
			}
			else
			{
				double d;
				if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
				{
					if (isNegative)
						d = -d;
					if (typeSuffix == null || typeSuffix == _D)
						return d;
					else
						return new CustomLiteral(d, typeSuffix);
				}
			}
			error = Localize.Localized("Syntax error in float literal");
			return null;
		}

		private static object ParseSpecialFloatValue(UString source, bool isNegative, int radix, Symbol typeSuffix, ref string error)
		{
			if (typeSuffix == _F)
			{
				float result = ParseHelpers.TryParseFloat(ref source, radix, ParseNumberFlag.SkipUnderscores);
				if (float.IsNaN(result))
					error = Localize.Localized("Syntax error in '{0}' literal", "float");
				else if (float.IsInfinity(result))
					error = Localize.Localized("Overflow in '{0}' literal", "float");
				if (isNegative)
					result = -result;
				return result;
			}
			else
			{
				string type = "double";
				if (typeSuffix == _M)
				{
					error = "Support for hex and binary literals of type decimal is not implemented. Converting from double instead.";
					type = "decimal";
				}

				double result = ParseHelpers.TryParseDouble(ref source, radix, ParseNumberFlag.SkipUnderscores);
				if (double.IsNaN(result))
					error = Localize.Localized("Syntax error in '{0}' literal", type);
				else if (double.IsInfinity(result))
					error = Localize.Localized("Overflow in '{0}' literal", type);
				if (isNegative)
					result = -result;
				if (typeSuffix == _M)
					return (decimal)result;
				if (typeSuffix == null || typeSuffix == _D)
					return result;
				else
					return new CustomLiteral(result, typeSuffix);
			}
		}

		#endregion

		#endregion

		// Due to the way generics are implemented, repeating the implementation 
		// of this base-class method might improve performance (TODO: verify this idea)
		new protected int LA(int i)
		{
			bool fail;
			char result = CharSource.TryGet(InputPosition + i, out fail);
			return fail ? -1 : result;
		}

		int MeasureIndent(UString indent)
		{
			return MeasureIndent(indent, SpacesPerTab);
		}
		public static int MeasureIndent(UString indent, int spacesPerTab)
		{
			int amount = 0;
			for (int i = 0; i < indent.Length; i++)
			{
				char ch = indent[i];
				if (ch == '\t') {
					amount += spacesPerTab;
					amount -= amount % spacesPerTab;
				} else if (ch == '.' && i + 1 < indent.Length) {
					amount += spacesPerTab;
					amount -= amount % spacesPerTab;
					i++;
				} else
					amount++;
			}
			return amount;
		}

		Maybe<Token> _current;

		void IDisposable.Dispose() {}
		Token IEnumerator<Token>.Current { get { return _current.Value; } }
		object System.Collections.IEnumerator.Current { get { return _current; } }
		void System.Collections.IEnumerator.Reset() { throw new NotSupportedException(); }
		bool System.Collections.IEnumerator.MoveNext()
		{
			_current = NextToken();
			return _current.HasValue;
		}
	}

}
