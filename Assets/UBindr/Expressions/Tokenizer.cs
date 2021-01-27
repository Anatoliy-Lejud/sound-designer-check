using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Assets.UBindr.Expressions
{
    public class Tokenizer
    {
        public Tokenizer(params TokenType[] tokenTypes)
        {
            TokenTypes = tokenTypes;
        }

        public TokenType[] TokenTypes { get; private set; }

        public List<Token> Tokenize(string text)
        {
            return Tokenize(this, text);
        }

        protected static List<Token> Tokenize(Tokenizer tokenizer, string text)
        {
            var tokens = new List<Token>();
            int pos = 0;
            while (pos < text.Length)
            {
                Token token = null;
                foreach (TokenType tokenRule in tokenizer.TokenTypes)
                {
                    var match = tokenRule.Match(text, pos);
                    if (match.Success && match.Index == pos)
                    {
                        token = new Token(tokenRule, match, tokenRule.GetValue(match));
                        break;
                    }
                }

                if (token == null)
                {
                    throw new InvalidOperationException(string.Format("Unmatched token {0} at {1}", text[pos], pos));
                }

                if (token.Match.Length == 0)
                {
                    throw new InvalidOperationException(string.Format("Bad token rule {0}, returns zero length matches!", token.TokenType.Code));
                }

                pos = token.Match.Index + token.Match.Length;
                if (!token.TokenType.Skip)
                {
                    tokens.Add(token);
                }
            }

            return tokens;
        }

        public class Token
        {
            public Token(TokenType tokenType, Match match, string text)
            {
                TokenType = tokenType;
                Text = text;
                Match = match;
            }

            public TokenType TokenType { get; private set; }
            public string Text { get; private set; }
            public Match Match { get; private set; }

            public override string ToString()
            {
                return string.Format("{0}: [{1}]", TokenType.Code, Text);
            }
        }

        public class TokenType
        {
            public TokenType(
                string code,
                Regex regex,
                bool skip = false,
                string targetGroup = null)
            {
                Code = code;
                Regex = regex;
                Skip = skip;
                TargetGroup = targetGroup;
            }

            public override string ToString()
            {
                return string.Format("{0} {1}", Code, Regex);
            }

            public string Code { get; private set; }
            public Regex Regex { get; private set; }
            public bool Skip { get; private set; }
            public string TargetGroup { get; private set; }

            public Match Match(string text, int pos)
            {
                return Regex.Match(text, pos);
            }

            public string GetValue(Match match)
            {
                if (TargetGroup != null)
                {
                    return match.Groups[TargetGroup].Value;
                }
                else
                {
                    return match.Value;
                }
            }
        }

        //public class RecurseTokenType : TokenType
        //{
        //    private Tokenizer _innerTokenizer;

        //    public RecurseTokenType(
        //        string code,
        //        Regex regex,
        //        Func<Tokenizer> tokenizerFunc) : base(code, regex, false)
        //    {
        //        TokenizerFunc = tokenizerFunc;
        //    }

        //    public Func<Tokenizer> TokenizerFunc { get; private set; }

        //    public Tokenizer InnerTokenizer
        //    {
        //        get { return _innerTokenizer ?? (_innerTokenizer = TokenizerFunc()); }
        //    }
        //}
    }
}