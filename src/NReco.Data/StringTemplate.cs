#region License
/*
 * NReco Data library (http://www.nrecosite.com/)
 * Copyright 2016 Vitaliy Fedorchenko
 * Distributed under the MIT license
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NReco.Data {

	/// <summary>
	/// Conditional string template parser.
	/// </summary>
	/// <remarks>
	/// StringTemplate replaces all tokens started with '@' (token name should have only aphanumeric or '_-' chars). 
	/// Token processing can be avoided by specifying @@ before any alphanumeric charachter, for example: <code>@@Test</code> 
	/// (result: <code>@Test</code>). Special symbols used for formatting syntax ('@', '[', ']',';', '{', '}') can be escaped in the following way:
	/// <list>
	/// <item>\; or ;; = ;</item>
	/// <item>\] or ]] = ]</item>
	/// <item>\[ = [</item>
	/// <item>\{ or {{ = {</item>  
	/// <item>\} or }} = }</item>  	 
	/// <item>\@ = @</item>  
	/// <item>\\ = \</item> 
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// var strTpl = new StringTemplate("@Name[Hello, {0}; Hi all]!");
	/// Console.Write( strTpl.FormatTemplate( new Dictionary&lt;string,object&gt;() { {"Name", "John"} } ) );  // Hi, John!
	/// Console.Write( strTpl.FormatTemplate( new Dictionary&lt;string,object&gt;() ) );  // Hi, all!
	/// </code>
	/// </example>
	public class StringTemplate {
		protected string Template;

		protected char[] ExtraNameChars = new [] {'_','-'};

		/// <summary>
		/// Get or set max recursion level of token replacement (for cases when token value contains token definitions). 
		/// </summary>
		/// <remarks>Default recursion level is 1 (this means that tokens inside token values are not replaced)</remarks>
		public int RecursionLevel { get; set; }

		/// <summary>
		/// Get or set flag that determines replacement behaviour when token is not defined (true by default)
		/// </summary>
		public bool ReplaceMissedTokens { get;set; }

		/// <summary>
		/// Determines whether to replace nested tokens (like <code>@token1[ @token2={0} ]</code> ). 
		/// </summary>
		public bool ReplaceNestedTokens { get; set; } = false;

		public StringTemplate(string tpl) {
			Template = tpl;
			RecursionLevel = 1;
			ReplaceMissedTokens = true;
		}

		public StringTemplate(string tpl, int recursionLevel) {
			Template = tpl;
			RecursionLevel = recursionLevel;
			ReplaceMissedTokens = true;
		}

		/// <summary>
		/// Replaces the format items in a specified string with the string representations of corresponding objects in a specified dictionary.
		/// </summary>
		public string FormatTemplate(IDictionary<string,object> props) {
			return FormatTemplate((token) => {
				return props.ContainsKey(token) ? new TokenResult(props[token]) : TokenResult.NotDefined;
			});
		}

		/// <summary>
		/// Replaces the format items in a specified string with the string representations of corresponding objects returned by value handler.
		/// </summary>
		public string FormatTemplate(Func<string,TokenResult> valueHandler) {
			var sb = new StringBuilder();
			string tpl = Template;
			for (int i = 0; i < RecursionLevel; i++) {
				sb.Clear();
				var replacedCount = ReplaceTokens(tpl, valueHandler, sb);
				tpl = sb.ToString();
				if (replacedCount == 0)
					break;
			}
			return tpl;
		}

		protected int ReplaceTokens(string tpl, Func<string,TokenResult> valueHandler, StringBuilder sb) {
			int pos = 0;
			int matchedTokensCount = 0;
			while (pos < tpl.Length) {
				var c = tpl[pos];
				if (c == '@') {
					int endPos;
					var name = ReadName(tpl, pos + 1, out endPos);
					if (name != null) {
						var tokenValue = resolveTokenValue(name, nested:false, endPos, out endPos);
						if (tokenValue!=null) {
							sb.Append(tokenValue);
							pos = endPos;
							continue;
						}
					} else {
						// check for @@ combination
						if ((pos + 1) < tpl.Length && tpl[pos+1]=='@') {
							pos++;
						}
					}
				}
				sb.Append(c);
				pos++;
			}
			return matchedTokensCount;

			string resolveTokenValue(string name, bool nested, int startPos, out int endPos) {
				TokenResult tokenRes;
				try {
					tokenRes = valueHandler(name);
				} catch (Exception ex) {
					throw new Exception(String.Format("Evaluation of token {0} at position {1} failed: {2}",
						name, pos, ex.Message), ex);
				}
				if (ReplaceMissedTokens || tokenRes.Defined) {
					object callRes = tokenRes.Value;
					string[] formatOptions;
					string tokenValue = String.Empty;
					try {
						formatOptions = ReadFormatOptions(tpl, nested, startPos, out endPos, resolveTokenValue);
					} catch (Exception ex) {
						throw new Exception(String.Format("Parse error (format options of token {0}) at {1}: {2}",
							name, pos, ex.Message), ex);
					}
					if (tokenRes.Applicable) {
						var fmtNotEmpty = formatOptions != null && formatOptions.Length > 0 ? formatOptions[0] : "{0}";
						var fmtEmpty = formatOptions != null && formatOptions.Length > 1 ? formatOptions[1] : "";
						try {
							tokenValue = callRes != null && Convert.ToString(callRes) != String.Empty ?
									FormatToken(fmtNotEmpty, callRes) : FormatToken(fmtEmpty, callRes);
						} catch (Exception ex) {
							throw new Exception(String.Format("Format of token {0} at position {1} failed: {2}",
								name, pos, ex.Message), ex);
						}
					}
					matchedTokensCount++;
					if (nested) {
						// this is nested token and resolved value will be parsed
						// by String.Format in the 'parent' token.
						// If nested token format returns '{' or '}' they should be escaped 
						tokenValue = escapeFormatBrackets(tokenValue);
					}
					return tokenValue;
				}
				endPos = startPos;
				return null;
			}

		}

		protected virtual string FormatToken(string fmt, object firstArg) {
			return String.Format(fmt, firstArg);
		}

		bool isDoubleChar(char c, string s, int pos) {
			return (s[pos] == c) && (pos + 1) < s.Length && s[pos + 1] == c;
		}

		private delegate string ResolveTokenValue(string name, bool nested, int startPos, out int endPos);

		bool isBackslashEscapedChar(char c) {
			switch (c) {
				case ';':
				case ']':
				case '[':
				case '\\':
				case '@':
				case '{':
				case '}':
					return true;
			}
			return false;
		}

		string escapeFormatBrackets(string s) {
			var sb = new StringBuilder(s.Length + 10);
			char c;
			for (var i = 0; i < s.Length; i++) {
				c = s[i];
				if (c=='{' || c=='}') {
					sb.Append(c); // double
				}
				sb.Append(c);
			}
			return sb.ToString();
		}

		private string[] ReadFormatOptions(string s, bool nested, int start, out int newStart, ResolveTokenValue resolveNestedTokenValue ) {
			newStart = start;
			if (start >= s.Length || s[start] != '[')
				return null;
			start++;
			var opts = new List<string>();
			var pSb = new StringBuilder();
			while (start < s.Length) {
				var isEscapedChar =
					((s[start] == '\\') && (start + 1) < s.Length && isBackslashEscapedChar(s[start + 1]))
					||
					isDoubleChar(';', s, start)
					||
					(isDoubleChar(']', s, start) && !nested)  // double-escape doesn't work inside nested b/c this can be closing braket of outer token
					||
					(ReplaceNestedTokens && isDoubleChar('@', s, start));
				if (isEscapedChar) {
					// process escaped special char
					var escapedChar = s[start + 1];
					pSb.Append(escapedChar);
					if (escapedChar == '{' || escapedChar == '}') {
						// if this is escaped curved bracket let's keep it as escaped
						// for String.Format (doubled)
						pSb.Append(escapedChar);
					}
					start++;
				} else if (s[start] == ']') {
					break;
				} else if (s[start] == ';') {
					opts.Add(pSb.ToString());
					pSb.Clear();
				} else if (ReplaceNestedTokens && s[start]=='@') {
					// nested token
					var nestedTokenName = ReadName(s, start + 1, out var endPos);
					if (nestedTokenName!=null) {
						var nestedTokenValue = resolveNestedTokenValue(nestedTokenName, nested:true, endPos, out endPos);
						if (nestedTokenValue!=null) {
							pSb.Append(nestedTokenValue);
							start = endPos;
							continue;
						}
					}
					pSb.Append(s[start]); // not a nested token, handle as a usual char
				} else {
					pSb.Append(s[start]);
				}
				start++;
			}
			opts.Add(pSb.ToString());
			if (start>=s.Length || s[start] != ']')
				throw new FormatException("Invalid format options (no closing ']')");
			if (opts.Count > 2)
				throw new FormatException("Too many format options");
			newStart = start + 1;
			return opts.ToArray();
		}

		protected string ReadName(string s, int start, out int newStart) {
			newStart = start;
			// should start with letter
			if (start >= s.Length || (!Char.IsLetter(s[start]) && s[start]!='(') )
				return null;
			if (s[start] == '(') {
				// rest of the name: any chars except ')'
				while (start < s.Length && s[start]!=')')
					start++;
				if (start >= s.Length)
					return null; // no closing bracket
				var name = s.Substring(newStart + 1, start - newStart - 1);
				newStart = start + 1; // for closing bracket
				return name;
			} else {
				// rest of the name: letters or digits or '_' or '-'
				while (start < s.Length && (Char.IsLetterOrDigit(s[start]) || Array.IndexOf(ExtraNameChars, s[start]) >= 0))
					start++;

				var name = s.Substring(newStart, start - newStart);
				newStart = start;
				return name;
			}
		}

		/// <summary>
		/// Represents token evaluation result.
		/// </summary>
		public sealed class TokenResult {

			/// <summary>
			/// Token value
			/// </summary>
			public object Value { get; private set; }

			/// <summary>
			/// Determines if token is defined.
			/// </summary>
			public bool Defined { get; private set; }
			
			/// <summary>
			/// Determines if token result is applicable.
			/// </summary>
			/// <remarks>If token is not applicable neither "has value" nor "empty value" is used (token replaced to empty string)</remarks>
			public bool Applicable { get; private set; }

			public static readonly TokenResult NotDefined = new TokenResult(null) { Defined = false };
			public static readonly TokenResult NotApplicable = new TokenResult(null) { Defined = true, Applicable = false };

			public TokenResult(object val) {
				Value = val;
				Defined = true;
				Applicable = true;
			}
		}

	}
}
