using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Assets.UBindr.Expressions
{
    public abstract class TopDownParser
    {
        protected TopDownParser()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            SymbolList = new List<Symbol>();
            Symbols = new Dictionary<string, Symbol>();
            EndToken = new Token(new Lexer.Lexeme { SymbolName = "(End)", Text = "(End)" }, null, new Symbol("(End)", null) { IsTerminal = true, InfixPriority = 20, PrefixPriority = 20 });
            // ReSharper disable once VirtualMemberCallInConstructor
            AddSymbols();
            CodeLexer = new Lexer(BuildRegexText());
        }

        public List<Symbol> SymbolList { get; set; }
        public Dictionary<string, Symbol> Symbols { get; set; }
        public Lexer CodeLexer { get; set; }
        protected Token EndToken { get; set; }

        public object Evaluate(string code, Scope scope = null)
        {
            var expression = scope != null ? scope.BuildOrGetExpression(code) : BuildExpression(code, null);
            return expression.Evaluate();
        }

        public Expression BuildExpression(string code, Scope scope)
        {
            var expression = new Expression(this, scope);
            var tokens = Tokenize(expression, code);
            if (tokens.Count <= 1)
            {
                throw new InvalidOperationException("No tokens received");
            }

            expression.LastToken = tokens[tokens.Count - 2];
            var context = new Context(this, tokens);
            expression.RootToken = BuildParseTree(context);
            Prepare(null, expression.RootToken);
            return expression;
        }

        public abstract void AddSymbols();

        public Symbol AddIgnoreSymbol(
            string symbolName,
            Regex regex)
        {
            var symbol = Symbols.GetOrCreate(symbolName, () => new Symbol(symbolName, regex));
            symbol.InfixPriority = 0;
            symbol.PrefixPriority = 0;
            symbol.Ignore = true;
            SymbolList.Add(symbol);
            return symbol;
        }

        public Symbol AddUnarySymbol(
            int prefixPriority,
            string symbolName,
            Regex regex,
            Func<Token, object> unaryEvaluator = null)
        {
            var symbol = Symbols.GetOrCreate(symbolName, () => new Symbol(symbolName, regex));
            symbol.PrefixPriority = prefixPriority;
            symbol.UnaryEvaluator = unaryEvaluator;
            SymbolList.Add(symbol);
            return symbol;
        }

        public Symbol AddTerminalSymbol(
            string symbolName,
            Regex regex,
            Func<Token, object> unaryEvaluator)
        {
            var symbol = Symbols.GetOrCreate(symbolName, () => new Symbol(symbolName, regex));
            symbol.UnaryEvaluator = unaryEvaluator;
            symbol.PrefixPriority = 0;
            symbol.IsTerminal = true;
            SymbolList.Add(symbol);
            return symbol;
        }

        public Symbol AddBinarySymbol(int infixPriority, string symbolName, Regex regex, Func<Token, object> binaryEvaluator = null)
        {
            var symbol = Symbols.GetOrCreate(symbolName, () => new Symbol(symbolName, regex));
            symbol.InfixPriority = infixPriority;
            symbol.BinaryEvaluator = binaryEvaluator;
            SymbolList.Add(symbol);
            return symbol;
        }

        public Token BuildParseTree(Context context, int priority = 19)
        {
            var left = context.CurrentToken;
            context.PopToken();

            if (!left.Symbol.IsTerminal)
            {
                if (left.PrefixBuilder != null)
                {
                    left.PrefixBuilder(context, left);
                }
                else
                {
                    left.Inputs.Add(BuildParseTree(context, left.PrefixPriority));
                    if (left.Symbol.AfterPrefix != null)
                    {
                        left.Symbol.AfterPrefix(context, left);
                    }
                }
            }

            while (priority > context.CurrentToken.InfixPriority)
            {
                var t = context.CurrentToken;
                t.IsInfix = true;
                if (t.InfixBuilder != null)
                {
                    t.InfixBuilder(context, t, left);
                }
                else
                {
                    t.Inputs.Add(left);
                    context.PopToken();
                    t.Inputs.Add(BuildParseTree(context, t.InfixPriority + (t.Symbol.RightAssociative ? 1 : 0)));
                }

                left = t;
            }

            return left;
        }

        public string BuildRegexText()
        {
            var sb = new StringBuilder();
            foreach (var tokenMatcher in SymbolList.OrderBy(x => x.BoostedRegexPriority ? 0 : 1))
            {
                if (sb.Length > 0)
                {
                    sb.Append("|");
                }
                sb.AppendLine(tokenMatcher.RegexText);
            }

            sb.AppendLine("|(?<Broken>.+?)");
            return sb.ToString();
        }

        public List<Token> Tokenize(Expression expression, string code)
        {
            var lexemes = Lex(code);
            List<Token> tokens = new List<Token>(lexemes.Count);
            foreach (var lexeme in lexemes)
            {
                var token = BuildToken(expression, lexeme);
                if (!token.Symbol.Ignore)
                {
                    tokens.Add(token);
                }
            }

            tokens.Add(EndToken);
            return tokens;
        }

        public List<Lexer.Lexeme> Lex(string code)
        {
            // What's the regex!?
            return CodeLexer.Lex(code);
        }

        private void Prepare(Token parent, Token child)
        {
            child.ParentToken = parent;
            child.Inputs.ForEach(c => Prepare(child, c));
        }

        public class Scope
        {
            public Scope(TopDownParser topDownParser)
            {
                TopDownParser = topDownParser;
            }

            public TopDownParser TopDownParser { get; set; }
            public Dictionary<string, MemberRoot> MemberRoots = new Dictionary<string, MemberRoot>();
            public Dictionary<string, Expression> ExpressionCache = new Dictionary<string, Expression>();

            public class MemberRoot
            {
                public string Name { get; set; }
                public Func<object> Accessor { get; set; }
                public Type Type { get; set; }
                public bool IsStatic { get; set; }

                public Type MemberType
                {
                    get
                    {
                        if (IsStatic)
                        {
                            return Type;
                        }
                        else
                        {
                            var obj = Accessor();
                            if (obj != null)
                            {
                                return obj.GetType();
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
            }

            public bool DisableExecute { get; set; }
            public bool DisableSet { get; set; }

            public void AddObjectRoot(string name, Func<object> func)
            {
                MemberRoot memberRoot =
                    new MemberRoot
                    {
                        Name = name,
                        Accessor = func,
                        IsStatic = false
                    };
                MemberRoots.SafeAddValue(name, memberRoot, true);
            }

            public void AddStaticRoot(string name, Type type)
            {
                MemberRoot memberRoot =
                    new MemberRoot
                    {
                        Name = name,
                        Type = type,
                        //Accessor = () => { throw new InvalidOperationException(); },
                        Accessor = () => type,
                        IsStatic = true
                    };
                MemberRoots.SafeAddValue(name, memberRoot, true);
            }

            public void RemoveRoot(string name)
            {
                MemberRoots.Remove(name);
            }

            public void Clear()
            {
                MemberRoots.Clear();
            }

            public Expression BuildOrGetExpression(string code)
            {
                return ExpressionCache.GetOrCreate(code, () => TopDownParser.BuildExpression(code, this));
            }
        }

        public class Context
        {
            public Context(TopDownParser topDownParser, List<Token> tokens)
            {
                TopDownParser = topDownParser;
                TokenQueue = new Queue<Token>(tokens);
                PopToken();
            }

            public TopDownParser TopDownParser { get; private set; }
            public Queue<Token> TokenQueue { get; set; }
            public Token CurrentToken { get; set; }

            public void PopToken()
            {
                CurrentToken = TokenQueue.Dequeue();
            }

            public void SkipToken(string tokenText)
            {
                //if (CurrentToken.Symbol.SymbolName != tokenText)
                if (CurrentToken.Lexeme.Text != tokenText)
                {
                    throw new InvalidOperationException(string.Format("Expected {0} but received {1}", tokenText, CurrentToken));
                }
                PopToken();
            }
        }

        private Token BuildToken(Expression expression, Lexer.Lexeme lexeme)
        {
            var symbol = Symbols.SafeGetValue(lexeme.SymbolName);
            if (symbol == null)
            {
                throw new InvalidOperationException(string.Format("Unable to find Symbol for {0}", lexeme));
            }
            return new Token(lexeme, expression, symbol);
        }

        public class Expression
        {
            public Expression(TopDownParser parser, Scope scope)
            {
                Parser = parser;
                Scope = scope;
            }

            public Token RootToken { get; set; }
            public Token LastToken { get; set; }
            public TopDownParser Parser { get; private set; }
            public Scope Scope { get; set; }
            public bool EvaluateLastIdentifierAsToken { get; set; }

            public override string ToString()
            {
                return RootToken.ToString();
            }

            public object Evaluate()
            {
                return RootToken.Evaluate();
            }

            public List<Token> GetTokenPath()
            {
                var tokens = new List<Token>();
                Recurse(tokens, RootToken);
                return tokens;
            }

            void Recurse(List<Token> tokens, TopDownParser.Token token)
            {
                if (token.Symbol.SymbolName == "dot")
                {
                    Recurse(tokens, token.Inputs[0]);
                    Recurse(tokens, token.Inputs[1]);
                }
                else
                {
                    tokens.Add(token);
                }
            }
        }

        public class Token
        {
            // Tokens are the intersection between lexemes and tokens
            public Token(Lexer.Lexeme lexeme, Expression expression, Symbol symbol)
            {
                Lexeme = lexeme;
                Expression = expression;
                Symbol = symbol;
                Symbol.PrepareToken(this);
                Inputs = new List<Token>();
            }

            public string Text
            {
                get { return Lexeme.Text; }
            }

            public Lexer.Lexeme Lexeme { get; private set; }
            public Symbol Symbol { get; private set; }
            public List<Token> Inputs { get; set; }
            public int InfixPriority
            {
                get { return Symbol.InfixPriority; }
            }

            public int PrefixPriority
            {
                get { return Symbol.PrefixPriority; }
            }

            public Action<Context, Token, Token> InfixBuilder
            {
                get { return Symbol.InfixBuilder; }
            }

            public Action<Context, Token> PrefixBuilder
            {
                get { return Symbol.PrefixBuilder; }
            }

            public bool IsInfix { get; set; }
            public Expression Expression { get; set; }
            public Scope Scope
            {
                get { return Expression.Scope; }
            }

            public object DotContext { get; set; }
            public Token ParentToken { get; set; }
            public Func<object> EvaluatorFunc { get; set; }

            public object Evaluate()
            {
                if (EvaluatorFunc != null)
                {
                    return EvaluatorFunc();
                }

                return Symbol.Evaluate(this);
            }

            public override string ToString()
            {
                if (Inputs.Any())
                {
                    return string.Format("({0} {1})", Lexeme.Text, Inputs.SJoin(" "));
                }
                else
                {
                    return Lexeme.Text;
                }
            }
        }

        public class Symbol
        {
            private int? _infixPriority;
            private int? _prefixPriority;
            // Symbols are reused thoughout an unaryEvaluator - every + uses the same symbol
            public Symbol(string symbolName, Regex regex)
            {
                SymbolName = symbolName;
                Regex = regex;
            }

            public string SymbolName { get; private set; }
            public Regex Regex { get; private set; }
            public string RegexText
            {
                get
                {
                    return string.Format("(?<{0}>{1})", SymbolName, Regex);
                }
                set { throw new NotImplementedException(); }
            }

            public override string ToString()
            {
                return SymbolName;
            }

            public Func<Token, object> UnaryEvaluator { get; set; }
            public Func<Token, object> BinaryEvaluator { get; set; }

            public bool IsTerminal { get; set; }
            public bool Ignore { get; set; }
            public bool RightAssociative { get; set; }
            public Action<Context, Token, Token> InfixBuilder { get; set; }
            public Action<Context, Token> PrefixBuilder { get; set; }
            public Action<Context, Token> AfterPrefix { get; set; }
            public Action<Token> TokenPreparer { get; set; }
            public bool BoostedRegexPriority { get; set; }

            public int InfixPriority
            {
                get
                {
                    if (!_infixPriority.HasValue)
                    {
                        throw new InvalidOperationException(string.Format("{0} doesn't have an Infix Priority", ToString()));
                    }

                    return _infixPriority.Value;
                }
                set { _infixPriority = value; }
            }

            public int PrefixPriority
            {
                get
                {
                    if (!_prefixPriority.HasValue)
                    {
                        throw new InvalidOperationException(string.Format("{0} doesn't have an Prefix Priority", ToString()));
                    }

                    return _prefixPriority.Value;
                }
                set { _prefixPriority = value; }
            }

            public Symbol SetPrefixBuilder(Action<Context, Token> prefixBuilder)
            {
                PrefixBuilder = prefixBuilder;
                return this;
            }

            public Symbol SetInfixBuilder(Action<Context, Token, Token> infixBuilder)
            {
                InfixBuilder = infixBuilder;
                return this;
            }

            public Symbol Update(Action<Symbol> action)
            {
                action(this);
                return this;
            }

            public object Evaluate(Token token)
            {
                if (token.IsInfix)
                {
                    if (BinaryEvaluator == null)
                    {
                        throw new InvalidOperationException(string.Format("{0} has no BinaryEvaluator!", ToString()));
                    }
                    return BinaryEvaluator(token);
                }
                else
                {
                    if (UnaryEvaluator == null)
                    {
                        throw new InvalidOperationException(string.Format("{0} has no UnaryEvaluator!", ToString()));
                    }
                    return UnaryEvaluator(token);
                }
            }

            public void PrepareToken(Token token)
            {
                if (TokenPreparer != null)
                {
                    TokenPreparer(token);
                }
            }
        }

        public class Lexer
        {
            public Lexer(string regexText)
            {
                LexerRegex = new Regex(regexText, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Multiline);
                _groupNames = LexerRegex.GetGroupNames();
            }

            private Regex LexerRegex { get; set; }

            private readonly string[] _groupNames; // = LexerRegex.GetGroupNames();

            public string GetSymbolNames(Match lexeme)
            {
                var groupName = _groupNames.LastOrDefault(x => lexeme.Groups[x].Success);
                if (groupName == null)
                {
                    throw new InvalidOperationException(string.Format("Unmatched: {0}", lexeme.Value));
                }

                return groupName;
            }

            public class Lexeme
            {
                public string SymbolName { get; set; }
                public Match Match { get; set; }
                public string Text { get; set; }

                public override string ToString()
                {
                    return string.Format("{0}: {1}", SymbolName, Text);
                }
            }

            public List<Lexeme> Lex(string code)
            {
                Match match = LexerRegex.Match(code);
                List<Lexeme> lexemes = new List<Lexeme>();
                while (match.Success)
                {
                    var symbolName = GetSymbolNames(match);

                    if (symbolName == "Broken" || string.IsNullOrEmpty(symbolName))
                    {
                        throw new InvalidOperationException(string.Format("Unmatched characters {0} at {1}", match.Value, match.Index));
                    }

                    if (symbolName != "Ignore")
                    {
                        lexemes.Add(
                            new Lexeme
                            {
                                SymbolName = symbolName,
                                Match = match,
                                Text = !string.IsNullOrEmpty(symbolName)
                                    ? match.Groups[symbolName].Value
                                    : match.Value
                            });
                    }

                    match = match.NextMatch();
                }

                return lexemes;
            }
        }
    }
}