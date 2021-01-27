using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Assets.UBindr.Expressions
{
    public class CSharpTopDownParser : TopDownParser
    {
        private Symbol _identifier;

        static CSharpTopDownParser()
        {
            Singleton = new CSharpTopDownParser();
        }

        public static CSharpTopDownParser Singleton { get; set; }

        public override void AddSymbols()
        {
            AddIgnoreSymbol("Whitespace", new Regex(@"\s+"));
            AddIgnoreSymbol("Comment", new Regex(@"/\*.*?\*/"));
            AddTerminalSymbol("Float", new Regex(@"\d+((\.\d +)?f|\.\d+)"), t => float.Parse(t.Lexeme.Text.Replace("f", "").Replace("F", ""), CultureInfo.InvariantCulture));
            AddTerminalSymbol("Int", new Regex(@"\d+"), t => int.Parse(t.Lexeme.Text, CultureInfo.InvariantCulture));
            AddTerminalSymbol("String", new Regex(@""".*?"""), null).Update(x => x.TokenPreparer = StringTokenPreparer);
            AddTerminalSymbol("True", new Regex(@"true(?![a-zA-Z0-9])"), t => true);
            AddTerminalSymbol("False", new Regex(@"false(?![a-zA-Z0-9])"), t => false);
            AddTerminalSymbol("null", new Regex(@"null(?![a-zA-Z0-9])"), t => null);
            _identifier = AddTerminalSymbol("Identifier", new Regex(@"[a-zA-Z_][a-zA-Z_0-9]*"), ExecuteIdentifier);

            //1 	()   []   ->   .   :: 	Function call, scope, array/member access
            AddBinarySymbol(1, "dot", new Regex(@"\.|\?\."), ExecuteDot);
            AddUnarySymbol(1, "Method", new Regex(@"[a-zA-Z_][a-zA-Z_0-9]*(?=\s*\()"), ExecuteMethod).SetPrefixBuilder((c, t) =>
            {
                c.SkipToken("(");
                if (c.CurrentToken.Text != ")")
                {
                    t.Inputs.Add(BuildParseTree(c, 15));
                    while (c.CurrentToken.Text == ",")
                    {
                        c.PopToken();
                        t.Inputs.Add(BuildParseTree(c, 15));
                    }
                }

                c.SkipToken(")");
            }).Update(s => s.BoostedRegexPriority = true);

            AddUnarySymbol(1, "Indexer", new Regex(@"[a-zA-Z_][a-zA-Z_0-9]*(?=\s*\[)"), ExecuteIndexer).SetPrefixBuilder((c, t) =>
            {
                c.SkipToken("[");
                if (c.CurrentToken.Text != "]")
                {
                    t.Inputs.Add(BuildParseTree(c, 15));
                    while (c.CurrentToken.Text == ",")
                    {
                        c.PopToken();
                        t.Inputs.Add(BuildParseTree(c, 15));
                    }
                }

                c.SkipToken("]");
            }).Update(s => s.BoostedRegexPriority = true);

            AddUnarySymbol(1, "OpenParen", new Regex(@"\("), x => x.Inputs[0].Evaluate()).SetPrefixBuilder((c, t) =>
            {
                t.Inputs.Add(BuildParseTree(c, 100));
                c.SkipToken(")");
            });
            AddBinarySymbol(100, "CloseParen", new Regex(@"\)"));

            AddUnarySymbol(1, "OpenBracket", new Regex(@"\[")).SetPrefixBuilder((c, t) =>
            {
                t.Inputs.Add(BuildParseTree(c, 100));
                c.SkipToken("]");
            });
            AddBinarySymbol(100, "CloseBracket", new Regex(@"\]"));

            //2 	!   ~   -   +   *   &   sizeof   type cast   ++   --   	(most) unary operators, sizeof and type casts (right to left)            
            AddUnarySymbol(2, "add", new Regex(@"\+"), IntFloat(a => a, a => a));
            AddUnarySymbol(2, "sub", new Regex(@"\-|−"), IntFloat(a => -a, a => -a));
            AddBinarySymbol(2, "pow", new Regex(@"\*\*"), IntFloat((a, b) => (float)Math.Pow(a, b), (a, b) => (float)Math.Pow(a, b))).Update(x => x.RightAssociative = true);
            AddUnarySymbol(2, "not", new Regex(@"\!"), Bool(a => !a));

            //3 	*   /   % MOD 	Multiplication, division, modulo
            AddBinarySymbol(3, "mul", new Regex(@"\*|×"), IntFloat((a, b) => a * b, (a, b) => a * b));
            AddBinarySymbol(3, "div", new Regex(@"\/|÷"), IntFloat((a, b) => a / b, (a, b) => a / b));
            AddBinarySymbol(3, "mod", new Regex(@"%"), IntFloat((a, b) => a % b, (a, b) => a % b));

            //4 	+   - 	Addition and subtraction
            AddBinarySymbol(4, "add", null, StrIntFloat((a, b) => a + b, (a, b) => a + b, (a, b) => a + b));
            AddBinarySymbol(4, "sub", null, IntFloat((a, b) => a - b, (a, b) => a - b));

            //5 	<<   >> 	Bitwise shift left and right
            //6 	<   <=   >   >= 	Comparisons: less-than and greater-than
            AddBinarySymbol(6, "leq", new Regex("<="), IntFloat((a, b) => a <= b, (a, b) => a <= b));
            AddBinarySymbol(6, "le", new Regex("<"), IntFloat((a, b) => a < b, (a, b) => a < b));
            AddBinarySymbol(6, "geq", new Regex(">="), IntFloat((a, b) => a >= b, (a, b) => a >= b));
            AddBinarySymbol(6, "ge", new Regex(">"), IntFloat((a, b) => a > b, (a, b) => a > b));

            //7 	==   != 	Comparisons: equal and not equal
            AddBinarySymbol(7, "equal", new Regex("=="), StrIntFloat((a, b) => a == b, (a, b) => a == b, (a, b) => a == b, (a, b) => Equals(a, b)));
            AddBinarySymbol(7, "unequal", new Regex("!="), StrIntFloat((a, b) => a != b, (a, b) => a != b, (a, b) => a != b, (a, b) => !Equals(a, b))).Update(s => s.BoostedRegexPriority = true);

            //8 	& 	Bitwise AND
            //9 	^ 	Bitwise exclusive OR (XOR)
            //10 	| 	Bitwise inclusive (normal) OR

            //11 	&& 	Logical AND
            AddBinarySymbol(11, "and", new Regex("&&"), t => (bool)t.Inputs[0].Evaluate() && (bool)t.Inputs[1].Evaluate());

            //12 	|| 	Logical OR
            AddBinarySymbol(12, "or", new Regex(@"\|\|"), t => (bool)t.Inputs[0].Evaluate() || (bool)t.Inputs[1].Evaluate());

            //13 	? : 	Conditional expression (ternary)
            AddBinarySymbol(13, "Ternary", new Regex(@"\?"),
                t =>
                {
                    var res = t.Inputs[0].Evaluate();
                    if (!(res is bool))
                    {
                        throw new InvalidOperationException(string.Format("Expected Bool but received {0}", res.GetType().Name));
                    }

                    var bres = res as bool?;
                    if (bres.Value)
                    {
                        return t.Inputs[1].Evaluate();
                    }
                    else
                    {
                        return t.Inputs[2].Evaluate();
                    }
                }).SetInfixBuilder((c, t, left) =>
            {
                t.Inputs.Add(left);
                c.PopToken();
                t.Inputs.Add(BuildParseTree(c, t.InfixPriority));
                c.SkipToken(":");
                t.Inputs.Add(BuildParseTree(c, t.InfixPriority));
            });
            AddUnarySymbol(13, "Colon", new Regex(@"\:")).Update(s => s.InfixPriority = 100);

            //14 	=   +=   -=   *=   /=   %=   &=   |=   ^=   <<=   >>= 	Assignment operators (right to left)
            AddBinarySymbol(14, "set", new Regex(@"\="), ExecuteAssignment);

            //15 	, 	Comma operator
            AddBinarySymbol(15, "comma", new Regex(@","));
        }

        public static TAttribute GetAttribute<TAttribute>(Expression expression) where TAttribute : Attribute
        {
            try
            {
                expression.EvaluateLastIdentifierAsToken = true;
                var res = expression.Evaluate();
                var last = expression.LastToken;
                if (res != last)
                {
                    return null;
                }

                if (last.ParentToken.DotContext == null)
                {
                    return null;
                }

                var typeWrapper = TypeWrapper.GetTypeWrapper(last.ParentToken.DotContext.GetType());

                TAttribute attribute = null;
                var field = typeWrapper.Type.GetField(last.Text);
                if (field != null)
                {
                    attribute = field.GetCustomAttributes(false).OfType<TAttribute>().FirstOrDefault();
                }

                var property = typeWrapper.Type.GetProperty(last.Text);
                if (property != null)
                {
                    attribute = property.GetCustomAttributes(false).OfType<TAttribute>().FirstOrDefault();
                }

                return attribute;
            }
            catch
            {
                return null;
            }
            finally
            {
                expression.EvaluateLastIdentifierAsToken = false;
            }
        }

        public Type GetPathResultType(string code, Scope scope)
        {
            var expression = scope.BuildOrGetExpression(code);

            if (expression.LastToken.Symbol != _identifier)
            {
                return null;
            }

            try
            {
                expression.EvaluateLastIdentifierAsToken = true;
                var res = expression.Evaluate();
                var token = expression.LastToken;
                if (res != token)
                {
                    return null;
                }

                if (token.ParentToken.DotContext == null)
                {
                    return null;
                }

                return TypeWrapper.GetTypeWrapper(token.ParentToken.DotContext.GetType()).Types[token.Text];
            }
            finally
            {
                expression.EvaluateLastIdentifierAsToken = false;
            }
        }

        public bool GetIsValidSetValueExpression(Expression expression)
        {
            if (expression.LastToken.Symbol != _identifier)
            {
                return false;
            }

            try
            {
                expression.EvaluateLastIdentifierAsToken = true;
                var res = expression.Evaluate();
                var token = expression.LastToken;
                if (res != token)
                {
                    return false;
                }

                if (token.ParentToken.DotContext == null)
                {
                    return false;
                }

                var parentTokenText = token.ParentToken != null ? token.ParentToken.Text : null;
                return parentTokenText == "." && token.ParentToken.Inputs[1] == token;
            }
            finally
            {
                expression.EvaluateLastIdentifierAsToken = false;
            }
        }

        public Token SetValue(Expression expression, object value)
        {
            if (expression.LastToken.Symbol != _identifier)
            {
                throw new InvalidOperationException("SetValue can only be called on an Identifier or an Indexer");
            }

            try
            {
                expression.EvaluateLastIdentifierAsToken = true;
                var res = expression.Evaluate();
                var token = expression.LastToken;
                if (res != token)
                {
                    throw new InvalidOperationException(string.Format("Expected to receive {0} but received {1}", token, res));
                }

                if (token.ParentToken.DotContext == null)
                {
                    throw new InvalidOperationException(string.Format("{0} doesn't have a context, assignment cannot be performed.", token));
                }

                var parentTokenText = token.ParentToken != null ? token.ParentToken.Text : null;
                if (parentTokenText == "." && token.ParentToken.Inputs[1] == token)
                {
                    var context = token.ParentToken.DotContext;
                    var typeWrapper = TypeWrapper.GetTypeWrapper(context.GetType());
                    typeWrapper.Set(context, token.Text, value);
                }
                else
                {
                    throw new InvalidOperationException("Expression is not a valid assignment expression");
                }

                return token;
            }
            finally
            {
                expression.EvaluateLastIdentifierAsToken = false;
            }
        }

        private Regex StringRegex = new Regex(
            @"(?<={)
		          .*?
		         (?=(,\s*\d+\s*)?(:.+?)?})",
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Multiline);

        private void StringTokenPreparer(Token token)
        {
            var tokenText = token.Lexeme.Text.Replace("\"", "");
            if (!token.Lexeme.Text.Contains("{"))
            {
                token.EvaluatorFunc = () => tokenText;
            }

            // Break the string into one expression per {XXX,?:?} block (the XXX) part and when
            // the string is evaluated, evaluate the results and use string.Format to create the string. None of the
            // formatting is done by this class, that's left intact.
            int count = 0;
            var expressions = new List<Expression>();
            var replaced = StringRegex.Replace(tokenText,
                m =>
                {
                    expressions.Add(token.Expression.Parser.BuildExpression(m.Value, token.Expression.Scope));
                    return count++.ToString();
                });

            token.EvaluatorFunc = () => string.Format(replaced, expressions.Select(x => x.Evaluate()).ToArray());
        }

        private object ExecuteMethod(Token token)
        {
            var parentTokenText = token.ParentToken != null ? token.ParentToken.Text : null;
            if (!((parentTokenText == "." || parentTokenText == "?.") && token.ParentToken.Inputs[1] == token))
            {
                throw new InvalidOperationException("Methods can only be used in combination with [.] or [.?] !");
            }
            var context = token.ParentToken.DotContext;
            var type = context.GetType();
            if (context is Type)
            {
                type = (Type)context;
            }
            var typeWrapper = TypeWrapper.GetTypeWrapper(type);
            return typeWrapper.ExecuteMethod(context, token.Text, token.Scope.DisableExecute, token.Inputs.Select(x => x.Evaluate()).ToArray());
        }

        private object ExecuteIndexer(Token token)
        {
            object context;

            if (token.ParentToken == null || token.ParentToken.DotContext == null)
            {
                context = GetScopeValue(token);
            }
            else
            {
                var outerContext = token.ParentToken.DotContext;
                var outerTypeWrapper = TypeWrapper.GetTypeWrapper(outerContext.GetType());

                context = outerTypeWrapper.Get(outerContext, token.Text);
                if (context == null)
                {
                    throw new InvalidOperationException(string.Format("{0} returns null", token.Text));
                }
            }

            var typeWrapper = TypeWrapper.GetTypeWrapper(context.GetType());
            return typeWrapper.ExecuteIndexer(context, token.Text, token.Scope.DisableExecute, token.Inputs.Select(x => x.Evaluate()).ToArray());
        }

        private object ExecuteIdentifier(Token token)
        {
            if (token == token.Expression.LastToken && token.Expression.EvaluateLastIdentifierAsToken)
            {
                return token;
            }

            var parentTokenText = token.ParentToken != null ? token.ParentToken.Text : null;
            if ((parentTokenText == "." || parentTokenText == "?.") && token.ParentToken.Inputs[1] == token)
            {
                var context = token.ParentToken.DotContext;

                var contextType = context is Type ? (Type)context : context.GetType();
                var typeWrapper = TypeWrapper.GetTypeWrapper(contextType);
                return typeWrapper.Get(context, token.Text);
            }
            else
            {
                // a.b.c
                return GetScopeValue(token);
            }
        }

        private static object GetScopeValue(Token token)
        {
            Scope.MemberRoot root = token.Scope.MemberRoots.SafeGetValue(token.Text);
            if (root == null)
            {
                throw new InvalidOperationException($"Unable to find member root for {token.Lexeme}");
            }

            return root.Accessor();
        }

        private object ExecuteDot(Token token)
        {
            token.DotContext = token.Inputs[0].Evaluate();
            if (token.DotContext == null)
            {
                if (token.Text == "?.")
                {
                    return null;
                }
                throw new InvalidOperationException("Context is null");
            }
            return token.Inputs[1].Evaluate();
        }

        private object ExecuteAssignment(Token token)
        {
            var dot = token.Inputs[0];
            if (dot.Text != ".")
            {
                throw new InvalidOperationException("Left part of assignment must be dot!");
            }
            var context = dot.Inputs[0].Evaluate();
            var property = dot.Inputs[1].Text;
            var value = token.Inputs[1].Evaluate();

            TypeWrapper typeWrapper = TypeWrapper.GetTypeWrapper(context.GetType());
            if (!token.Scope.DisableSet && !token.Scope.DisableExecute)
            {
                typeWrapper.Set(context, property, value);
            }
            return value;
        }

        public bool CanConvertToInt(object value)
        {
            if (value == null)
            {
                return false;
            }

            var type = value.GetType();
            return
                type == typeof(byte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(int)
                || type == typeof(uint)
                || type == typeof(long)
                || type == typeof(ulong);
        }

        public bool CanConvertToFloat(object value)
        {
            if (value == null)
            {
                return false;
            }

            var type = value.GetType();
            return
                type == typeof(byte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(int)
                || type == typeof(uint)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal);
        }

        public Func<Token, object> IntFloat(
            Func<int, int> intFunc,
            Func<float, float> floatFunc)
        {
            return t =>
            {
                var a = t.Inputs[0].Evaluate();

                if (CanConvertToInt(a))
                {
                    var ia = Convert.ToInt32(a);
                    return intFunc(ia);
                }

                var fa = Convert.ToSingle(a);
                return floatFunc(fa);
            };
        }

        public Func<Token, object> Bool(
            Func<bool, bool> boolFunc)
        {
            return t =>
            {
                var ba = (bool)t.Inputs[0].Evaluate();

                return boolFunc(ba);
            };
        }

        public Func<Token, object> IntFloat(
            Func<int, int, object> intFunc,
            Func<float, float, object> floatFunc)
        {
            return t =>
            {
                var a = t.Inputs[0].Evaluate();
                var b = t.Inputs[1].Evaluate();

                if (CanConvertToInt(a) && CanConvertToInt(b))
                {
                    var ia = Convert.ToInt32(a);
                    var ib = Convert.ToInt32(b);
                    return intFunc(ia, ib);
                }

                var fa = Convert.ToSingle(a);
                var fb = Convert.ToSingle(b);
                return floatFunc(fa, fb);
            };
        }

        public Func<Token, object> StrIntFloat(
            Func<string, string, object> strFunc,
            Func<int, int, object> intFunc,
            Func<float, float, object> floatFunc,
            Func<object, object, object> fallbackFunc = null)
        {
            return t =>
            {
                var a = t.Inputs[0].Evaluate();
                var b = t.Inputs[1].Evaluate();

                if (a is string || b is string)
                {
                    var ia = Convert.ToString(a);
                    var ib = Convert.ToString(b);
                    return strFunc(ia, ib);
                }

                if (CanConvertToInt(a) && CanConvertToInt(b))
                {
                    var ia = Convert.ToInt32(a);
                    var ib = Convert.ToInt32(b);
                    return intFunc(ia, ib);
                }

                if (CanConvertToFloat(a) && CanConvertToFloat(b) || fallbackFunc == null)
                {
                    var fa = Convert.ToSingle(a);
                    var fb = Convert.ToSingle(b);
                    return floatFunc(fa, fb);
                }

                return fallbackFunc(a, b);
            };
        }
    }
}