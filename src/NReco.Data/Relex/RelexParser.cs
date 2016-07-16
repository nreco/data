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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace NReco.Data.Relex
{
	
	/// <summary>
	/// Parses relex expression to <see cref="Query"/> structure.
	/// </summary>
	public class RelexParser
	{
		static readonly string[] nameGroups = new string[] { "and", "or"};
		static readonly string[] delimiterGroups = new string[] { "&&", "||"};
		static readonly QGroupType[] enumGroups = new QGroupType[] { QGroupType.And, QGroupType.Or };
		
		static readonly string[] delimiterConds = new string[] {
			"==", "=",
			"<>", "!=",
			">", ">=",
			"<", "<="};
		static readonly string[] nameConds = new string[] {
			"in", "like" };
			
		static readonly string nullField = "null";
		
		static readonly Conditions[] enumDelimConds = new Conditions[] {
			Conditions.Equal, Conditions.Equal,
			Conditions.Not|Conditions.Equal, Conditions.Not|Conditions.Equal,
			Conditions.GreaterThan, Conditions.GreaterThan|Conditions.Equal,
			Conditions.LessThan, Conditions.LessThan|Conditions.Equal
		};

		static readonly Conditions[] enumNameConds = new Conditions[] {
			Conditions.In, Conditions.Like
		};

		
		static readonly char[] delimiters = new char[] {
			'(', ')', '[', ']', ';', ':', ',', '=', '<', '>', '!', '&', '|', '*', '{', '}'};
		static readonly char charQuote = '"';
		static readonly char[] specialNameChars = new char[] {
			'.', '-', '_' };
		static readonly char[] arrayValuesSeparators = new char[] {
			'\0', ';', ',', '\t' }; // order is important!
		
		static readonly string[] typeNames;
		static readonly string[] arrayTypeNames;

		
		public enum LexemType {
			Unknown,
			Name,
			Delimiter,
			QuotedConstant,
			Constant,
			Stop
		}
		
		static RelexParser() {
			typeNames = Enum.GetNames(typeof(TypeCode));
			arrayTypeNames = new string[typeNames.Length];
			for (int i=0; i<typeNames.Length; i++) {
				typeNames[i] = typeNames[i].ToLower();
				arrayTypeNames[i] = typeNames[i] + "[]";
			}

		}
		
		public RelexParser() {
		}
		
		protected LexemType GetLexemType(string s, int startIdx, out int endIdx) {
			LexemType lexemType = LexemType.Unknown;
			endIdx = startIdx;
			while (endIdx<s.Length) {
				if (Array.IndexOf(delimiters,s[endIdx])>=0) {
					if (lexemType==LexemType.Unknown) {
						endIdx++;
						return LexemType.Delimiter;
					}
					if (lexemType!=LexemType.QuotedConstant)
						return lexemType;
				} else if (Char.IsSeparator(s[endIdx])) {
					if (lexemType!=LexemType.QuotedConstant && lexemType!=LexemType.Unknown)
						return lexemType; // done
				} else if (Char.IsLetter(s[endIdx])) {
					if (lexemType==LexemType.Unknown)
						lexemType=LexemType.Name;
				} else if (Char.IsDigit(s[endIdx])) {
					if (lexemType==LexemType.Unknown)
						lexemType=LexemType.Constant;
				} else if (Array.IndexOf(specialNameChars,s[endIdx])>=0) {
					if (lexemType==LexemType.Unknown)
						lexemType=LexemType.Constant;
					if (lexemType!=LexemType.Name && lexemType!=LexemType.Constant && lexemType!=LexemType.QuotedConstant)
						throw new RelexParseException(
							String.Format("Invalid syntax (position: {0}, expression: {1}", startIdx, s ) );
				} else if (s[endIdx]==charQuote) {
					if (lexemType==LexemType.Unknown)
						lexemType = LexemType.QuotedConstant;
					else {
						if (lexemType==LexemType.QuotedConstant) {
							// check for "" combination
							
							
							if ( ( (endIdx+1)<s.Length && s[endIdx+1]!=charQuote) ) {
								endIdx++;
								return lexemType;
							} else
								if ((endIdx+1)<s.Length) endIdx++; // skip next quote
								
						}
					}
				} else if (Char.IsControl(s[endIdx]) && lexemType!=LexemType.Unknown && lexemType!=LexemType.QuotedConstant)
					return lexemType;
				
				// goto next char
				endIdx++;
			}
			
			if (lexemType==LexemType.Unknown) return LexemType.Stop;
			if (lexemType==LexemType.Constant)
				throw new RelexParseException(
					String.Format("Unterminated constant (position: {0}, expression: {1}", startIdx, s ) );
			return lexemType;
		}

		protected string GetLexem(string s, int startIdx, int endIdx, LexemType lexemType) {
			string lexem = GetLexem(s, startIdx, endIdx);
			if (lexemType!=LexemType.QuotedConstant)
				return lexem;
			// remove first and last chars
			string constant = lexem.Substring(1, lexem.Length-2); 
			// replace "" with "
			return constant.Replace( "\"\"", "\"" );
		}
		
		protected string GetLexem(string s, int startIdx, int endIdx) {
			return s.Substring(startIdx, endIdx-startIdx).Trim();
		}

		
		protected void GetAllDelimiters(string s, int startIdx, out int endIdx) {
			endIdx = startIdx;
			while ((endIdx+1)<s.Length && Array.IndexOf(delimiters, s[endIdx])>=0 )
				endIdx++;
		}
		
		protected bool GetGroupType(LexemType lexemType, string s, int startIdx, ref int endIdx, ref QGroupType groupType) {
			string lexem = GetLexem(s, startIdx, endIdx).ToLower();
			if (lexemType==LexemType.Name) {
				int idx = Array.IndexOf(nameGroups, lexem);
				if (idx<0) return false;
				groupType = enumGroups[idx];
				return true;
			}
			if (lexemType==LexemType.Delimiter) {
				// read all available delimiters...
				GetAllDelimiters(s, endIdx, out endIdx);
				lexem = GetLexem(s, startIdx, endIdx);
				
				int idx = Array.IndexOf(delimiterGroups, lexem);
				if (idx<0) return false;
				groupType = enumGroups[idx];
				return true;
			}
			return false;
		}
		
		protected bool GetCondition(LexemType lexemType, string s, int startIdx, ref int endIdx, ref Conditions conditions) {
			string lexem = GetLexem(s, startIdx, endIdx).ToLower();
			if (lexemType==LexemType.Name) {
				int idx = Array.IndexOf(nameConds, lexem);
				if (idx>=0) {
					conditions = enumNameConds[idx];
					return true;
				}
			}
			
			if (lexemType==LexemType.Delimiter) {
				// read all available delimiters...
				GetAllDelimiters(s, endIdx, out endIdx);
				lexem = GetLexem(s, startIdx, endIdx);

				int idx = Array.IndexOf(delimiterConds, lexem);
				if (idx<0) {
					if (lexem=="!") {
						int newEndIdx;
						Conditions innerConditions = Conditions.Equal;
						
						LexemType newLexemType = GetLexemType(s, endIdx, out newEndIdx);
						if (GetCondition(newLexemType, s, endIdx, ref newEndIdx, ref innerConditions)) {
							endIdx = newEndIdx;
							conditions = innerConditions|Conditions.Not;
							return true;
						}
					}
					return false;
				}
				conditions = enumDelimConds[idx];
				return true;
			}
			return false;
		}
		
		
		public virtual Query Parse(string relEx) {
			int endIdx;
			IQueryValue qValue = ParseInternal(relEx, 0, out endIdx );
			if (!(qValue is Query)) 
				throw new RelexParseException("Invalid expression: result is not a query");
			Query q = (Query)qValue;
			return q;
		}
		
		public virtual QNode ParseCondition(string relExCondition) {
			int endIdx;
			if (String.IsNullOrEmpty(relExCondition))
				return null;
			QNode node = ParseConditionGroup(relExCondition, 0, out endIdx);
			return node;
		}
		
		protected virtual IQueryValue ParseTypedConstant(string typeCodeString, string constant) {
			typeCodeString = typeCodeString.ToLower();
			// sql type
			if (typeCodeString == "sql")
				return new QRawSql(constant);
			// var type
			if (typeCodeString == "var")
				return new QVar(constant);

			// simple type
			int typeNameIdx = Array.IndexOf(typeNames, typeCodeString);
			if (typeNameIdx>=0) {
				TypeCode typeCode = (TypeCode)Enum.Parse(typeof(TypeCode), typeCodeString, true);
				try {
					object typedConstant = Convert.ChangeType(constant, typeCode, CultureInfo.InvariantCulture);
					return new QConst(typedConstant);
				} catch (Exception ex) {
					throw new InvalidCastException(
						 String.Format("Cannot parse typed constant \"{0}\":{1}",constant, typeCodeString),ex);
				}
			}
			// array
			typeNameIdx = Array.IndexOf(arrayTypeNames, typeCodeString);
			if (typeNameIdx>=0) {
				TypeCode typeCode = (TypeCode)Enum.Parse(typeof(TypeCode), typeNames[typeNameIdx], true);
				string[] arrayValues = SplitArrayValues(constant);
				object[] array = new object[arrayValues.Length];
				for (int i=0; i<array.Length; i++)
					array[i] = Convert.ChangeType(arrayValues[i], typeCode, CultureInfo.InvariantCulture);
				return new QConst(array);
			}

			throw new InvalidCastException(
				String.Format("Cannot parse typed constant \"{0}\":{1}",
					constant, typeCodeString) );
		}
		
		protected string[] SplitArrayValues(string str) {
			for (int i=0; i<arrayValuesSeparators.Length; i++)
				if (str.IndexOf(arrayValuesSeparators[i])>=0)
					return str.Split(arrayValuesSeparators[i]);
			return str.Split( arrayValuesSeparators );
		}
		
		
		protected virtual IQueryValue ParseInternal(string input, int startIdx, out int endIdx) {
			LexemType lexemType = GetLexemType(input, startIdx, out endIdx);
			string lexem = GetLexem(input, startIdx, endIdx);
						
			if (lexemType==LexemType.Constant)
				return (QConst)lexem;
			
			if (lexemType==LexemType.QuotedConstant) {
				// remove first and last chars
				string constant = lexem.Substring(1, lexem.Length-2); 
				// replace "" with "
				constant = constant.Replace( "\"\"", "\"" );
				// typed?
				int newEndIdx;
				if ( GetLexemType(input, endIdx, out newEndIdx)==LexemType.Delimiter &&
					 GetLexem(input, endIdx, newEndIdx)==":" ) {
					int typeEndIdx;
					if (GetLexemType(input, newEndIdx, out typeEndIdx)==LexemType.Name) {
						string typeCodeString = GetLexem(input, newEndIdx, typeEndIdx);
						endIdx = typeEndIdx;
						// read [] at the end if specified
						if (GetLexemType(input, endIdx, out newEndIdx)==LexemType.Delimiter &&
							GetLexem(input, endIdx, newEndIdx)=="[")
							if (GetLexemType(input, newEndIdx, out typeEndIdx)==LexemType.Delimiter &&
								GetLexem(input, newEndIdx, typeEndIdx)=="]") {
								endIdx = typeEndIdx;
								typeCodeString += "[]";
							}
						
						return ParseTypedConstant(typeCodeString, constant);
					}
				}
				
				return (QConst)constant;
			}
			
			if (lexemType==LexemType.Name) {
				int nextEndIdx;
				
				// query
				string tableName = lexem;
				QNode rootCondition = null;
				QField[] fields = null;
                QSort[] sort = null;
				
				LexemType nextLexemType = GetLexemType(input, endIdx, out nextEndIdx);
				string nextLexem = GetLexem(input, endIdx, nextEndIdx);
				if (nextLexemType==LexemType.Delimiter && nextLexem=="(") {
					// compose conditions
					rootCondition = ParseConditionGroup(input, nextEndIdx, out endIdx);
					// read ')'
					nextLexemType = GetLexemType(input, endIdx, out nextEndIdx);
					if (nextLexemType!=LexemType.Delimiter || GetLexem(input, endIdx,nextEndIdx)!=")")
						throw new RelexParseException(
							String.Format("Invalid syntax (position: {0}, expression: {1})", endIdx, input ) );
					
					// read next lexem
					nextLexemType = GetLexemType(input, nextEndIdx, out endIdx);
					nextLexem = GetLexem(input, nextEndIdx, endIdx);
					nextEndIdx = endIdx;
				}
				
				if (nextLexemType==LexemType.Delimiter && nextLexem=="[") {
					nextLexemType = GetLexemType(input, nextEndIdx, out endIdx);
					nextLexem = GetLexem(input, nextEndIdx, endIdx, nextLexemType);
					nextEndIdx = endIdx;
					
					var fieldsList = new List<QField>();
                    var sortList = new List<QSort>();
					var curFldBuilder = new StringBuilder();
					curFldBuilder.Append(nextLexem);
                    bool sortPart = false;
					Action pushFld = () => {
						if (curFldBuilder.Length > 0) {
							if (sortPart) {
								sortList.Add(curFldBuilder.ToString());
							} else {
								fieldsList.Add(curFldBuilder.ToString());
							}
							curFldBuilder.Clear();
						}
					};
                    do {
						LexemType prevLexemType = nextLexemType;
						nextLexemType = GetLexemType(input, endIdx, out nextEndIdx);
						nextLexem = GetLexem(input, endIdx, nextEndIdx, nextLexemType);
						endIdx = nextEndIdx;
						if (nextLexemType == LexemType.Delimiter && nextLexem == "]") {
							pushFld();
							break;
						}
						if (nextLexemType == LexemType.Stop) {
							throw new RelexParseException(
								String.Format("Invalid syntax - unclosed bracket (position: {0}, expression: {1})", endIdx, input));
						}
                        // handle sort separator
                        if (nextLexemType == LexemType.Delimiter && nextLexem == ";") {
							pushFld();
							sortPart = true;
						} else if (nextLexemType == LexemType.Delimiter && nextLexem == ",") {
							pushFld();
						} else {
							if (prevLexemType != LexemType.Delimiter && 
									(nextLexem.Equals(QSort.Asc,StringComparison.OrdinalIgnoreCase) ||
									 nextLexem.Equals(QSort.Desc, StringComparison.OrdinalIgnoreCase))) {
								curFldBuilder.Append(' ');
							}
							curFldBuilder.Append(nextLexem);
                        }
                    } while (true);
					if (fieldsList.Count != 1 || fieldsList[0] != "*")
						fields = fieldsList.ToArray();
					if (sortList.Count > 0)
						sort = sortList.ToArray();
				} else {
					return (QField)lexem;
				}
				endIdx = nextEndIdx;

				Query q = new Query( tableName, rootCondition);
				
				// limits?
				nextLexemType = GetLexemType(input, endIdx, out nextEndIdx);
				nextLexem = GetLexem(input, endIdx, nextEndIdx);
				if (nextLexemType==LexemType.Delimiter && nextLexem=="{") {
					// read start record
					endIdx = nextEndIdx;
					nextLexemType = GetLexemType(input, endIdx, out nextEndIdx);
					nextLexem = GetLexem(input, endIdx, nextEndIdx);
					if (nextLexemType!=LexemType.Constant)
						throw new RelexParseException(
							String.Format("Invalid syntax (position: {0}, expression: {1})", endIdx, input ) );
					q.RecordOffset = Int32.Parse(nextLexem);
					// read comma
					endIdx = nextEndIdx;
					nextLexemType = GetLexemType(input, endIdx, out nextEndIdx);
					nextLexem = GetLexem(input, endIdx, nextEndIdx);
					if (nextLexemType!=LexemType.Delimiter || nextLexem!=",")
						throw new RelexParseException(
							String.Format("Invalid syntax (position: {0}, expression: {1})", endIdx, input ) );
						
					// read record count
					endIdx = nextEndIdx;
					nextLexemType = GetLexemType(input, endIdx, out nextEndIdx);
					nextLexem = GetLexem(input, endIdx, nextEndIdx);
					if (nextLexemType!=LexemType.Constant)
						throw new RelexParseException(
							String.Format("Invalid syntax (position: {0}, expression: {1})", endIdx, input ) );
					q.RecordCount = Int32.Parse(nextLexem);				
					
					// read close part '}'
					endIdx = nextEndIdx;
					nextLexemType = GetLexemType(input, endIdx, out nextEndIdx);
					nextLexem = GetLexem(input, endIdx, nextEndIdx);
					if (nextLexemType!=LexemType.Delimiter || nextLexem!="}")
						throw new RelexParseException(
							String.Format("Invalid syntax (position: {0}, expression: {1})", endIdx, input ) );

					endIdx = nextEndIdx;
				}
								
				q.Select(fields);
                if (sort != null)
					q.OrderBy(sort);
				return q;
			}
			
			throw new RelexParseException(
				String.Format("Invalid syntax (position: {0}, expression: {1})", endIdx, input ) );
		}

		protected string ParseNodeName(string input, int startIdx, out int endIdx) {
			string nodeName = null;
			// check for node name - starts with '<'
			LexemType lexemType = GetLexemType(input, startIdx, out endIdx);
			string lexem = GetLexem(input, startIdx, endIdx);
			if (lexemType==LexemType.Delimiter && lexem=="<") {
				startIdx = endIdx;
				lexemType = GetLexemType(input, startIdx, out endIdx);
				if (lexemType!=LexemType.Name && lexemType!=LexemType.Constant && lexemType!=LexemType.QuotedConstant)
					throw new RelexParseException(
						String.Format("Invalid syntax - node name expected (position: {0}, expression: {1})", startIdx, input ) );
				nodeName = GetLexem(input, startIdx, endIdx);
				startIdx = endIdx;
				// read closing delimiter '>'
				lexemType = GetLexemType(input, startIdx, out endIdx);
				lexem = GetLexem(input, startIdx, endIdx);
				if (lexemType!=LexemType.Delimiter || lexem!=">")
					throw new RelexParseException(
						String.Format("Invalid syntax (position: {0}, expression: {1})", startIdx, input ) );
			} else {
				endIdx = startIdx; 
			}

			return nodeName;
		}
		
		protected QNode ParseConditionGroup(string input, int startIdx, out int endIdx) {
			int nextEndIdx;
			LexemType lexemType = GetLexemType(input, startIdx, out nextEndIdx);
			string lexem = GetLexem(input, startIdx, nextEndIdx);
			
			QNode node;
			if (lexemType==LexemType.Delimiter && lexem=="(") {
				string nodeName = ParseNodeName(input, nextEndIdx, out endIdx);
				nextEndIdx = endIdx;

				// check for empty group
				lexemType = GetLexemType(input, nextEndIdx, out endIdx);
				if (lexemType==LexemType.Delimiter && GetLexem(input,nextEndIdx,endIdx)==")") {
					node = null;
					// push back
					endIdx = nextEndIdx;
				} else
					node = ParseConditionGroup(input, nextEndIdx, out endIdx);

				if (nodeName!=null) {
					if (node==null)
						node = new QGroupNode(QGroupType.And);
					if (node is QNode)
						((QNode)node).Name = nodeName;
				}

				// read ')'
				lexemType = GetLexemType(input, endIdx, out nextEndIdx);
				if (lexemType!=LexemType.Delimiter || GetLexem(input,endIdx,nextEndIdx)!=")")
					throw new RelexParseException(
						String.Format("Invalid syntax (position: {0}, expression: {1})", endIdx, input ) );
				endIdx = nextEndIdx;
			} else {
				node = ParseCondition(input, startIdx, out nextEndIdx);
				endIdx = nextEndIdx;
			}
			
			// check for group
			lexemType = GetLexemType(input, endIdx, out nextEndIdx);
			QGroupType groupType = QGroupType.And;
			if (GetGroupType(lexemType, input, endIdx, ref nextEndIdx, ref groupType))
				return ComposeGroupNode(node, ParseConditionGroup(input, nextEndIdx, out endIdx), groupType);

			return node;		
		}

		protected QGroupNode ComposeGroupNode(QNode node1, QNode node2, QGroupType groupType) {
			QGroupNode group1 = node1 as QGroupNode, group2 = node2 as QGroupNode;
			if (group1 != null && group1.GroupType != groupType)
				group1 = null;
			if (group2 != null && group2.GroupType != groupType)
				group2 = null;

			// don't corrupt named groups
			if (group1 != null && group1.Name != null || group2 != null && group2.Name != null)
				group1 = group2 = null;

			if (group1 == null) {
				if (group2 == null) {
					QGroupNode group = new QGroupNode(groupType);
					group.Nodes.Add(node1);
					group.Nodes.Add(node2);
					return group;				
				} else {
					group2.Nodes.Insert(0, node1);
					return group2;				
				}
			} else {
				if (group2 == null)
					group1.Nodes.Add(node2);
				else
					foreach (QNode qn in group2.Nodes)
						group1.Nodes.Add(qn);
				return group1;
			}
		}
		
		protected QNode ParseCondition(string input, int startIdx, out int endIdx) {
			
			IQueryValue leftValue = ParseInternal(input, startIdx, out endIdx); 
			
			int nextEndIdx;
			Conditions conditions = Conditions.Equal;

			LexemType nextLexemType = GetLexemType(input, endIdx, out nextEndIdx);
			if (!GetCondition(nextLexemType, input, endIdx, ref nextEndIdx, ref conditions))
				throw new RelexParseException(
					String.Format("Invalid syntax (position: {0}, expression: {1})", startIdx, input ) );

			IQueryValue rightValue = ParseInternal(input, nextEndIdx, out endIdx);
			QNode node;
			if (IsNullValue(rightValue)) {
				if ( (conditions & Conditions.Equal)!=0 )
					node = new QConditionNode( leftValue, Conditions.Null | (conditions & ~Conditions.Equal), null);
				else
					throw new RelexParseException(
						String.Format("Invalid syntax - such condition cannot be used with 'null' (position: {0}, expression: {1})", startIdx, input ) );
			} else
				node = new QConditionNode( leftValue, conditions, rightValue);
			
			return node;
		}
		
		protected bool IsNullValue(IQueryValue value) {
			return ((value is QField) && ((QField)value).Name.ToLower()==nullField);
		}
		

	}

	/// <summary>
	/// </summary>
	public class RelexParseException : Exception {
		public RelexParseException() : base() {}
		public RelexParseException(string message) : base(message) {}
		public RelexParseException(string message, Exception innerException) : base(message, innerException) {}
	}
}

