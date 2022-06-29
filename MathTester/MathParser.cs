using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathTester;

partial class MathParser
{
    public static IEnumerable<ISimpleToken?> GenerateSimpleTokens(string expression, Environment env)
    {
        var expressionCount = expression.Length;
        char? NextChar(int currentCharLocation)
        {
            if ((currentCharLocation + 1) >= expressionCount)
                return null;
            else return expression[currentCharLocation + 1];
        }
        for (int i = 0; i < expressionCount; i++)
        {
            char c = expression[i];
            switch (c)
            {
                case ' ':
                    continue;
                case '(':
                    yield return new BracketToken { TokenType = BracketTokenType.OpenBracket };
                    continue;
                case ')':
                    yield return new BracketToken { TokenType = BracketTokenType.CloseBracket };
                    continue;
                case '[':
                    yield return new BracketToken { TokenType = BracketTokenType.OpenIndexBracket };
                    continue;
                case ']':
                    yield return new BracketToken { TokenType = BracketTokenType.CloseIndexBracket };
                    continue;
                case ',':
                    yield return new CommaToken();
                    continue;
                case '.':
                    if (i + 1 < expressionCount && NextChar(i) is '.')
                    {
                        i++;
                        yield return new SystemToken { TokenType = SystemTokenType.Range };
                    }
                    else
                        yield return new SystemToken { TokenType = SystemTokenType.Dot };
                    continue;
                case ':':
                    yield return new SystemToken { TokenType = SystemTokenType.Range };
                    continue;
                case '+':
                    yield return new OperatorToken { TokenType = OperatorTokenType.Plus };
                    continue;
                case '-':
                    if (NextChar(i) is char nextChar && char.IsDigit(nextChar)) goto default;
                    yield return new OperatorToken { TokenType = OperatorTokenType.Minus };
                    continue;
                case '*':
                    if (i + 1 < expressionCount && NextChar(i) is '*')
                    {
                        i++;
                        yield return new OperatorToken { TokenType = OperatorTokenType.Power };
                    }
                    else
                        yield return new OperatorToken { TokenType = OperatorTokenType.Times };
                    continue;
                case '^':
                    yield return new OperatorToken { TokenType = OperatorTokenType.Power };
                    break;
                case '/':
                    yield return new OperatorToken { TokenType = OperatorTokenType.Divide };
                    continue;
                default:
                    if (char.IsLetter(c))
                    {
                        string s = "";
                        while (i < expressionCount)
                            if (char.IsLetter(expression[i])) s += expression[i++];
                            else { i--; break; }
                        yield return new NameToken { Text = s, Environment = env };
                        continue;
                    }
                    if (c is '-' || char.IsDigit(c))
                    {
                        double num;
                        string s = "";
                        if (c is '-')
                        {
                            s += "-";
                            i++;
                        }
                        while (i < expressionCount)
                            if (char.IsDigit(expression[i]))
                                s += expression[i++];
                            else { i--; break; }
                        if (i < expressionCount && NextChar(i) is '.')
                        {
                            i++;
                            s += expression[i];
                            if (NextChar(i).HasValue)
                            {
                                i++;
                                if (char.IsDigit(expression[i]))
                                {
                                    while (i < expressionCount)
                                        if (char.IsDigit(expression[i])) s += expression[i++];
                                        else { i--; break; }
                                }
                                else if (char.IsLetter(expression[i]))
                                {
                                    i--;
                                    if (double.TryParse(s, out num))
                                    {
                                        yield return new NumberToken { Number = num };
                                        yield return new SystemToken { TokenType = SystemTokenType.Dot };
                                        continue;
                                    }
                                }
                            }
                        }
                        if (double.TryParse(s, out num))
                        {
                            yield return new NumberToken { Number = num };
                            continue;
                        }
                        else goto TokenError;
                    }
                TokenError:
                    yield return null;
                    yield break;
            }
        }
    }
    public static IEnumerable<IToken?> GroupTokens(IEnumerator<IToken?> TokenEnumerator, Environment env)
    {
        if (TokenEnumerator.Current is not BracketToken firstToken) goto TokenError;
        List<IToken> TokenCollection = new List<IToken>();
        bool ThereIsComma = false;
        while (TokenEnumerator.MoveNext())
        {
            var currentToken = TokenEnumerator.Current;
            if (currentToken == null) goto TokenError;
            if (currentToken is BracketToken bracketToken)
            {
                switch (bracketToken.TokenType)
                {
                    case BracketTokenType.OpenBracket:
                    case BracketTokenType.OpenIndexBracket:
                        var tokens = GroupTokens(TokenEnumerator, env).ToArray();
                        if (tokens.Length != 0 && tokens[^1] == null) goto TokenError;
                        //yield return ProcessTokens(tokens, env);
                        var parsedGroup = Parse(tokens, env);
                        if (parsedGroup is null) goto TokenError;
                        TokenCollection.Add(new GrouppedToken { Tokens = parsedGroup });
                        continue;
                    case BracketTokenType.CloseBracket:
                        if (firstToken.TokenType != BracketTokenType.OpenBracket) goto TokenError;
                        goto Done;
                    case BracketTokenType.CloseIndexBracket:
                        if (firstToken.TokenType != BracketTokenType.OpenIndexBracket) goto TokenError;
                        goto Done;
                    default:
                        // Weird Bracket
                        Debugger.Break();
                        throw new ArgumentOutOfRangeException(nameof(TokenEnumerator));
                }
            }
            if (currentToken is CommaToken)
            {
                ThereIsComma = true;
                yield return new GrouppedToken { Tokens = TokenCollection.ToArray() };
                yield return currentToken;
                TokenCollection.Clear();
            }
            else TokenCollection.Add(currentToken);
        }
    TokenError:
        yield return null;
        yield break;
    Done:
        if (ThereIsComma)
        {
            var token = new GrouppedToken { Tokens = TokenCollection.ToArray() };
            TokenCollection.Clear();
            yield return token;
            yield break;
        }
        foreach (var t in TokenCollection) yield return t;
        TokenCollection.Clear();
        yield break;
    }
    public static List<IToken>? ParseFunctionCalls(IEnumerable<IToken?> TokenEnumerable, Environment env)
    {
        var enumerator = TokenEnumerable.GetEnumerator();
        List<IToken> OutputStack = new();
        while (enumerator.MoveNext())
        {
            var currentToken = enumerator.Current;
            if (currentToken is null) goto TokenError;
            if (currentToken is GrouppedToken GrouppedToken && OutputStack.Count >= 1 && OutputStack[^1] is NameToken FunctionNameToken)
            {
                bool Return23 = true;
                List<IValueToken> Parameters = new();
                if (OutputStack.Count >= 2 && OutputStack[^2] is SystemToken SystemToken && SystemToken.TokenType is SystemTokenType.Dot)
                {
                    if (OutputStack.Count < 3) goto TokenError;
                    var output3 = OutputStack[^3];
                    if (output3 is null) goto TokenError;
                    if (output3 is not IValueToken valtok3)
                    {
                        Debugger.Break();
                        return null;
                    }
                    Parameters.Add(valtok3);
                    Return23 = false;
                }
                if (GrouppedToken.HasComma)
                    foreach (var value in GrouppedToken.Tokens)
                    {
                        if (value is null) goto TokenError;
                        if (value is not CommaToken)
                        {
                            if (value is not IValueToken valtok)
                            {
                                Debugger.Break();
                                return null;
                            }
                            Parameters.Add(valtok);
                        }
                    }
                else if (!GrouppedToken.IsEmpty)
                    Parameters.Add(GrouppedToken);
                OutputStack.RemoveAt(OutputStack.Count - 1); // current - 1 (NameToken FunctionNameToken)
                if (!Return23)
                {
                    OutputStack.RemoveAt(OutputStack.Count - 1); // current - 2 (Dot Token)
                    OutputStack.RemoveAt(OutputStack.Count - 1); // current - 3 (Some Value)
                }
                OutputStack.Add(new ParsedFunctionToken { FunctionName = FunctionNameToken, Parameters = Parameters.ToArray() });
                Parameters.Clear();
            } else
            {
                OutputStack.Add(currentToken);
            }
        }
        return OutputStack;
    TokenError:
        OutputStack.Clear();
        return null;
    }
    public static List<IToken>? ParseOperators(IEnumerable<IToken?> TokenEnumerable, Environment env, params OperatorTokenType[] Operators)
    {
        var enumerator = TokenEnumerable.GetEnumerator();
        List<IToken> OutputStack = new();
        while (enumerator.MoveNext())
        {
            var currentToken = enumerator.Current;
            if (currentToken is null) goto TokenError;
            if (currentToken is OperatorToken OperatorToken && Operators.Contains(OperatorToken.TokenType))
            {
                var item1 = OutputStack[^1];
                if (!enumerator.MoveNext()) goto TokenError;
                var item2 = enumerator.Current;
                if (item1 is not IValueToken ValueToken1) goto TokenError;
                if (item2 is not IValueToken ValueToken2) goto TokenError;
                OutputStack.RemoveAt(OutputStack.Count - 1); // item1
                OutputStack.Add(new ParsedOperatorToken { Operator = OperatorToken, Value1 = ValueToken1, Value2 = ValueToken2 });
            }
            else
            {
                OutputStack.Add(currentToken);
            }
        }
        return OutputStack;
    TokenError:
        OutputStack.Clear();
        return null;
    }
    public static IList<IToken?>? Parse(IEnumerable<IToken?> TokenEnumerable, Environment env)
    {
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        var a = ParseFunctionCalls(TokenEnumerable, env);
        if (a is null) return null;
        a = ParseOperators(a, env, OperatorTokenType.Power);
        if (a is null) return null;
        a = ParseOperators(a, env, OperatorTokenType.Times, OperatorTokenType.Divide, OperatorTokenType.Mod);
        if (a is null) return null;
        a = ParseOperators(a, env, OperatorTokenType.Plus, OperatorTokenType.Minus);
        return a;
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
    }
    public static IEnumerable<IToken?> GroupTokens(IEnumerable<IToken?> TokenEnumerable, Environment env)
    {
        var openbracket = new IToken[] { new BracketToken { TokenType = BracketTokenType.OpenBracket } };
        var closebracket = new IToken[] { new BracketToken { TokenType = BracketTokenType.CloseBracket } };
        var enumerator = openbracket.Concat(TokenEnumerable).Concat(closebracket).GetEnumerator();
        enumerator.MoveNext();
        return GroupTokens(enumerator, env);
    }
    /*

    static IEnumerable<IToken?> GroupTokens(IEnumerable<IToken?> Tokens, Environment env)
    {
        var TokenEnumerator = Tokens.GetEnumerator();
        var tokens = GroupTokensAndReturnWhereDone(TokenEnumerator, env);
        return tokens;
        //    while (TokenEnumerator.MoveNext())
        //    {
        //        var currenttoken = TokenEnumerator.Current;
        //        if (currenttoken == null) yield return null;
        //        if (currenttoken is BracketToken bracket)
        //        {
        //            switch (bracket.TokenType)
        //            {
        //                case BracketTokenType.OpenBracket:
        //                case BracketTokenType.OpenIndexBracket:
        //                    var tokens = GroupTokensAndReturnWhereDone(TokenEnumerator).ToArray();
        //                    if (tokens[^1] == null) goto TokenError;
        //                    if (bracket.TokenType == BracketTokenType.OpenBracket)
        //                        yield return new GrouppedToken { Tokens = tokens };
        //                    else
        //                        yield return new IndexGrouppedToken { Tokens = tokens };
        //                    continue;
        //                default:
        //                    goto TokenError;
        //            }
        //        }
        //        if (currenttoken is CommaToken) goto TokenError; // There should be no top level comma
        //    }

    }
    static IToken? ProcessTokens(IList<IToken?> GrouppedTokens, Environment env)
    {
        //var Tokens = GroupTokens(UngrouppedTokens).ToArray();
        for (var i = 0; i < GrouppedTokens.Count; i++)
        {
            if (GrouppedTokens[i] is NameToken NameToken)
            {
                var val = env.FindName(NameToken.Text);
                if (val == null) return null;

            }
        }
    }
    // Parse Math expression into tree 
    static IOperation Parse(List<IOperation> operationInOrder)
    {
        if (operationInOrder[0] is UnpharsedOperation unpharsedOperation)
        {

        }
        int openbracketindex;
        if ((openbracketindex = s.IndexOf('(')) != -1)
        {
            int closebracketindex;
            if ((closebracketindex = s.LastIndexOf(')')) != -1)
            {
                if (openbracketindex > closebracketindex)
                    return null; // Something's wrong with bracket

            }
        }
    }
    static async Task<IOperation?> Parse(string s)
    {
        return await Task.Run(delegate
        {
            List<Operation> operation = new List<Operation>();
            int openbracketindex;
            if ((openbracketindex = s.IndexOf('(')) != -1)
            {
                int closebracketindex;
                if ((closebracketindex = s.LastIndexOf(')')) != -1)
                {
                    if (openbracketindex > closebracketindex)
                        return null; // Something's wrong with bracket

                }
            }
            else
            {
                if (!s.Contains(')'))
                    return null; // Error: Unmatched bracket
                //while (s.Contains(''))
            }

            return new Operation();
        });
    }
    */
}
