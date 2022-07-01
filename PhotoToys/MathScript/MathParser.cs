using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathScript;

partial class MathParser
{
    public const string ErrorReport = "Please report to the developer because this is not supposed to happen";
    public static IEnumerable<ISimpleToken> GenerateSimpleTokens(string expression, Environment env)
    {
        string ErrorMessage = "";
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
                    yield return new ErrorToken { Message = "Syntax Error: '[' is not currently supported." };
                    //yield return new BracketToken { TokenType = BracketTokenType.OpenIndexBracket };
                    continue;
                case ']':
                    yield return new ErrorToken { Message = "Syntax Error: ']' is not currently supported." };
                    //yield return new BracketToken { TokenType = BracketTokenType.CloseIndexBracket };
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
                    yield return new ErrorToken { Message = ErrorMessage };
                    yield break;
            }
        }
    }
    public static IEnumerable<IToken> GroupTokens(IEnumerator<IToken> TokenEnumerator, Environment env)
    {
        string ErrorMessage = "";
        if (TokenEnumerator.Current is not BracketToken firstToken)
        {
            ErrorMessage = $"Syntax Error & Internal: Expecting token '{TokenEnumerator.Current}' to be a bracket but it isn't. {ErrorReport}";
            Debugger.Break();
            goto TokenError;
        }
        List<IToken> TokenCollection = new ();
        bool ThereIsComma = false;
        while (TokenEnumerator.MoveNext())
        {
            var currentToken = TokenEnumerator.Current;
            if (currentToken is ErrorToken et)
            {
                ErrorMessage = et.Message;
                goto TokenError;
            }
            if (currentToken is BracketToken bracketToken)
            {
                switch (bracketToken.TokenType)
                {
                    case BracketTokenType.OpenBracket:
                    case BracketTokenType.OpenIndexBracket:
                        var tokens = GroupTokens(TokenEnumerator, env).ToArray();
                        if (tokens.Length != 0 && tokens[^1] is ErrorToken et2)
                        {
                            ErrorMessage = et2.Message;
                            goto TokenError;
                        }
                        //yield return ProcessTokens(tokens, env);
                        var parsedGroup = Parse(tokens, env);
                        if (parsedGroup.Count == 1 && parsedGroup[0] is ErrorToken et3) {
                            ErrorMessage = et3.Message;
                            goto TokenError;
                        }
                        TokenCollection.Add(new GrouppedToken { Tokens = parsedGroup });
                        continue;
                    case BracketTokenType.CloseBracket:
                        if (firstToken.TokenType != BracketTokenType.OpenBracket)
                        {
                            ErrorMessage = $@"Syntax Error: Unmatched Bracket (starts with {firstToken.TokenType switch
                            {
                                BracketTokenType.OpenBracket => "'('",
                                BracketTokenType.OpenIndexBracket => "'['",
                                _ => $"<Internal Error: unknown open bracket {ErrorMessage}>"
                            }} but ends with ')' instead of {firstToken.TokenType switch
                            {
                                BracketTokenType.OpenBracket => "')'",
                                BracketTokenType.OpenIndexBracket => "']'",
                                _ => $"<Internal Error: unknown expected closing bracket {ErrorMessage}>"
                            }} )";
                            goto TokenError;
                        }
                        goto Done;
                    case BracketTokenType.CloseIndexBracket:
                        if (firstToken.TokenType != BracketTokenType.OpenIndexBracket)
                        {
                            ErrorMessage = $@"Syntax Error: Unmatched Bracket (starts with {firstToken.TokenType switch
                            {
                                BracketTokenType.OpenBracket => "'('",
                                BracketTokenType.OpenIndexBracket => "'['",
                                _ => $"<Internal Error: unknown open bracket {ErrorMessage}>"
                            }} but ends with ']' instead of {firstToken.TokenType switch
                            {
                                BracketTokenType.OpenBracket => "')'",
                                BracketTokenType.OpenIndexBracket => "']'",
                                _ => $"<Internal Error: unknown expected closing bracket {ErrorMessage}>"
                            }} )";
                            goto TokenError;
                        }
                        goto Done;
                    default:
                        // Weird Bracket
#if DEBUG
                        Debugger.Break();
#endif
                        ErrorMessage = $"Syntax & Internal Error: Token '{bracketToken}' is not a known bracket. {ErrorReport}";
                        goto TokenError;
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
        ErrorMessage = "Syntax Error: It's the end of the expression, " +
            $"but the bracket '{firstToken}' is not closed properly " +
            $@"(expecting {firstToken.TokenType switch {
                BracketTokenType.OpenBracket => "')'",
                BracketTokenType.OpenIndexBracket => "']'",
                _ => "closing bracket"
            }}) at the end";
        goto TokenError;
    TokenError:
        yield return new ErrorToken { Message = ErrorMessage };
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
    public static List<IToken> ParseFunctionCalls(IEnumerable<IToken> TokenEnumerable, Environment env)
    {
        string ErrorMessage = "";
        var enumerator = TokenEnumerable.GetEnumerator();
        List<IToken> OutputStack = new();
        while (enumerator.MoveNext())
        {
            var currentToken = enumerator.Current;
            if (currentToken is ErrorToken et1)
            {
                ErrorMessage = et1.Message;
                goto TokenError;
            }
            if (currentToken is GrouppedToken GrouppedToken && OutputStack.Count >= 1 && OutputStack[^1] is NameToken FunctionNameToken)
            {
                bool Return23 = true;
                List<IValueToken> Parameters = new();
                if (OutputStack.Count >= 2 && OutputStack[^2] is SystemToken SystemToken && SystemToken.TokenType is SystemTokenType.Dot)
                {
                    if (OutputStack.Count < 3)
                    {
                        ErrorMessage = "Syntax Error: There is no value before dot '.' character";
                        goto TokenError;
                    }
                    var output3 = OutputStack[^3];
                    if (output3 is ErrorToken et)
                    {
                        ErrorMessage = et.Message;
                        goto TokenError;
                    }
                    if (output3 is not IValueToken valtok3)
                    {
#if DEBUG
                        Debugger.Break();
#endif
                        ErrorMessage = $"Syntax Error & Internal Error: The value '{output3}' before dot '.' is unrecognizable. {ErrorReport}";
                        goto TokenError;
                    }
                    Parameters.Add(valtok3);
                    Return23 = false;
                }
                if (GrouppedToken.HasComma)
                    foreach (var value in GrouppedToken.Tokens)
                    {
                        if (value is ErrorToken et)
                        {
                            ErrorMessage = et.Message;
                            goto TokenError;
                        }
                        if (value is not CommaToken)
                        {
                            if (value is not IValueToken valtok)
                            {
#if DEBUG
                                Debugger.Break();
#endif
                                ErrorMessage = $"Syntax Error & Internal Error: The value '{value}' that calls function '{FunctionNameToken.Text}' is not recognizalbe. {ErrorReport}";
                                goto TokenError;
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
        return new List<IToken> { new ErrorToken { Message = ErrorMessage } };
    }
    public static List<IToken> ParseOperators(IEnumerable<IToken> TokenEnumerable, params OperatorTokenType[] Operators)
    {
        string ErrorMessage;
        var enumerator = TokenEnumerable.GetEnumerator();
        List<IToken> OutputStack = new();
        while (enumerator.MoveNext())
        {
            var currentToken = enumerator.Current;
            if (currentToken is ErrorToken et)
            {
                ErrorMessage = et.Message;
                goto TokenError;
            }
            if (currentToken is OperatorToken OperatorToken && Operators.Contains(OperatorToken.TokenType))
            {
                bool ImplicitFirstArgument = false;
                var item1 = OutputStack.Count == 0 ? null : OutputStack[^1];
                if (!enumerator.MoveNext())
                {
                    ErrorMessage = $"Syntax Error: It's the end of the expression. However, the operator '{OperatorToken}' does not see the value to its right";
                    goto TokenError;
                }
                var item2 = enumerator.Current;
                if (item1 is not IValueToken ValueToken1)
                {
                    if (OperatorToken.TokenType.GetImplicitFirstArument() is IValueToken vt)
                    {
                        ImplicitFirstArgument = true;
                        ValueToken1 = vt;
                    } else {
                        if (OutputStack.Count == 0)
                        {
                            ErrorMessage = $"Syntax Error: the operator '{OperatorToken}' does not see anytihng to the left";
                            goto TokenError;
                        }
                        ErrorMessage = $"Syntax Error: the operator '{OperatorToken}' sees the invalid token to the left '{item1}' (The token is not a value)'";
                        goto TokenError;
                    }
                }
                if (item2 is not IValueToken ValueToken2)
                {
                    ErrorMessage = $"Syntax Error: the operator '{OperatorToken}' sees the invalid token to the right '{item2}' (The token is not a value)'";
                    goto TokenError;
                }
                if (!ImplicitFirstArgument)
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
        return new List<IToken> { new ErrorToken { Message = ErrorMessage } };
    }
    public static IList<IToken> Parse(IEnumerable<IToken> TokenEnumerable, Environment env)
    {
        var a = ParseFunctionCalls(TokenEnumerable, env);
        if (a.Count == 1 && a[0] is ErrorToken) return a;
        a = ParseOperators(a, OperatorTokenType.Power);
        if (a.Count == 1 && a[0] is ErrorToken) return a; 
        a = ParseOperators(a, OperatorTokenType.Times, OperatorTokenType.Divide, OperatorTokenType.Mod);
        if (a.Count == 1 && a[0] is ErrorToken) return a; 
        a = ParseOperators(a, OperatorTokenType.Plus, OperatorTokenType.Minus);
        return a;
    }
    public static IValueToken Parse(string Text, Environment env)
    {
        var tokens = GenerateSimpleTokens(Text, env).ToArray();

        var grouppedTokens = GroupTokens(tokens, env).ToArray();
        var parseFunctionCall = Parse(grouppedTokens, env);
        if (parseFunctionCall.Count != 1) return new ErrorToken { Message = "Syntax Error: The token parsed unsuccessfully" };
        if (parseFunctionCall[0] is not IValueToken parsed)
        {
            return new ErrorToken { Message = "Syntax Error: The token parsed unsuccessfully" };
        }
        return parsed;
        }
    public static IEnumerable<IToken> GroupTokens(IEnumerable<IToken> TokenEnumerable, Environment env)
    {
        var openbracket = new IToken[] { new BracketToken { TokenType = BracketTokenType.OpenBracket } };
        var closebracket = new IToken[] { new BracketToken { TokenType = BracketTokenType.CloseBracket } };
        var enumerator = openbracket.Concat(TokenEnumerable).Concat(closebracket).GetEnumerator();
        enumerator.MoveNext();
        return GroupTokens(enumerator, env);
    }
}
