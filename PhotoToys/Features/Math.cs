using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.IO;
using Windows.Storage;
using System.Threading;
namespace PhotoToys.Features;
class AdvancedManipulation : Category
{

    public override string Name { get; } = nameof(AdvancedManipulation).ToReadableName();
    public override string Description { get; } = "Apply advanced manipulation features";
    public override Feature[] Features { get; } = new Feature[]
    {
        new Normalize()
    };
}
class Normalize : Feature
{
    public override string Name { get; } = nameof(Normalize);
    public override string Description => "Normalize the value of image (Equvilent to x / max(x) * 255)";
    enum ManipulationMode
    {
        Normalize
    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameterDefinition(),
            },
            OnExecute: x =>
            {
                
            }
        );
    }
}
/*
class Math : Feature
{
    static StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
    static Math()
    {
        async void main()
        {
            JSFile = await InstallationFolder.GetFileAsync("LoadPyodide.js");
            PythonFile = await InstallationFolder.GetFileAsync("MathWrapper.py");
        }
        main();
    }
    static StorageFile? JSFile;
    static StorageFile? PythonFile;
    CoreWebView2? Browser;
    public override string Name => nameof(Math);
    public override string Description => "Apply Mathematics to customize your image look";
    protected override UIElement CreateUI()
    {
        void CreateWebView2Thread()
        {
            var thread = new Thread((ThreadStart)delegate
            {
                WV2::Microsoft.Web.WebView2.WinForms.WebView2 webView21 = new WV2::Microsoft.Web.WebView2.WinForms.WebView2();

                var ecwTask = webView21.EnsureCoreWebView2Async(null);
                ecwTask.ContinueWith(_ => Browser = webView21.CoreWebView2);
                System.Windows.Forms.Application.Run();
            });
            thread.SetApartmentState(ApartmentState.STA);
        }


        CreateWebView2Thread();
        UIElement? element = null;
        return element = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new StringTextBoxParameter(
                    Name: "Math Expression (use \"x\" as the matrix)",
                    Placeholder: "Math Expression",
                    LiveUpdate: false,
                    IsReady: async x =>
                    {
                        return await Task.Run(() => !string.IsNullOrEmpty(x));
                    }
                ).Assign(out var scriptParam)
            },
            OnExecute: async delegate
            {
                if (PythonFile == null) return;
                try
                {

                    await Browser?.ExecuteScriptAsync($"pyodide.runPython(`{File.ReadAllText(PythonFile.Path).Replace("\\", "\\\\")}`)");
                    await Browser?.ExecuteScriptAsync("pyodide.runPython('StaticCounter.reset(); x = MathActionRecorder()')");
                    await Browser?.ExecuteScriptAsync($"pyodide.runPython(`{scriptParam.Result}`)");
                    var output = await Browser?.ExecuteScriptAsync($"pyodide.runPython(`x.showHistory()`)");
                    if (element != null)
                        await new ContentDialog
                        {
                            Title = "Result",
                            Content = output,
                            XamlRoot = element.XamlRoot,
                            PrimaryButtonText = "Okay"
                        }.ShowAsync();
                    Browser?.OpenDevToolsWindow();
                }
                catch (Exception e) { 

                }
            }
        );
    }
}
    interface IToken
    {

    }
    interface ISimpleToken : IToken
    {

    }
    interface ICompoundToken : IToken
    {

    }
    public enum SystemTokenType
    {
        Dot,
        Range,
        Comma
    }
    public enum OperatorTokenType
    {
        OpenBracket,
        CloseBracket,
        OpenIndexBracket,
        CloseIndexBracket,
        Function,
        Power,
        Times,
        Divide,
        Mod,
        Plus,
        Minus,
    }
    public enum BracketTokenType
    {
        OpenBracket,
        CloseBracket,
        OpenIndexBracket,
        CloseIndexBracket
    }
    public int Precedence(OperatorTokenType type)
        => type switch
        {
            // Reference: https://en.wikipedia.org/wiki/Order_of_operations
            OperatorTokenType.OpenBracket => 0,
            OperatorTokenType.CloseBracket => 0,
            OperatorTokenType.OpenIndexBracket => 0,
            OperatorTokenType.CloseIndexBracket => 0,
            OperatorTokenType.Function => 0,
            OperatorTokenType.Power => 1,
            OperatorTokenType.Times => 3,
            OperatorTokenType.Divide => 3,
            OperatorTokenType.Mod => 3,
            OperatorTokenType.Plus => 4,
            OperatorTokenType.Minus => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    struct SystemToken : ISimpleToken
    {
        public SystemTokenType TokenType;
    }
    struct OperatorToken : ISimpleToken
    {
        public OperatorTokenType TokenType;
        public Func<object, object>? Function;
    }
    struct BracketToken : ISimpleToken
    {
        public BracketTokenType TokenType;
    }
    struct CommaToken : ISimpleToken
    {
        
    }
    struct NameToken : ISimpleToken
    {
        public string Text;
    }
    struct GrouppedToken : ICompoundToken
    {
        public IEnumerable<IToken?> Tokens;
    }
    struct IndexGrouppedToken : ICompoundToken
    {
        public IEnumerable<IToken?> Tokens;
    }
    struct NumberToken : ISimpleToken
    {
        public double Number;
    }
    interface IOperation : IToken
    {

    }
    class Operation : IOperation
    {

    }
    struct UnpharsedOperation : IOperation
    {
        public UnpharsedOperation(string Text) => this.Text = Text;
        public string Text { get; set; }
    }
    interface IValue
    {

    }
    interface ICallable : IValue
    {

    }
    struct Environment
    {
        public IValue? FindName(string Name)
        {

        }
    }
    static IEnumerable<ISimpleToken?> GenerateSimpleTokens(string expression)
    {
        var expressionCount = expression.Length;
        for (int i = 0; i < expressionCount; i++)
        {
            char c = expression[expressionCount];
            switch (c)
            {
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
                case '.':
                    if (i + 1 < expressionCount && expression[expressionCount + 1] == '.')
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
                    yield return new OperatorToken { TokenType = OperatorTokenType.Minus };
                    continue;
                case '*':
                    if (i + 1 < expressionCount && expression[expressionCount + 1] == '*')
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
                        while (i < expressionCount && char.IsLetter(expression[expressionCount]))
                            s += expression[expressionCount++];
                        yield return new NameToken { Text = s };
                        continue;
                    }
                    if (char.IsDigit(c))
                    {
                        double num;
                        string s = "";
                        while (i < expressionCount && char.IsDigit(expression[expressionCount]))
                            s += expression[expressionCount++];
                        if (i < expressionCount && expression[expressionCount] == '.')
                        {
                            var nextchar = expression[expressionCount];
                            if (char.IsDigit(nextchar))
                                while (i < expressionCount && char.IsDigit(expression[expressionCount]))
                                    s += expression[expressionCount++];
                            else if (char.IsLetter(nextchar))
                            {
                                if (double.TryParse(s, out num))
                                {
                                    yield return new NumberToken { Number = num };
                                    yield return new SystemToken { TokenType = SystemTokenType.Dot };
                                    continue;
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
    static IEnumerable<IToken?> GroupTokensAndReturnWhereDone(IEnumerator<IToken?> TokenEnumerator, Environment env)
    {
        if (TokenEnumerator.Current is not BracketToken firstToken) goto TokenError;
        List<IToken> TokenCollection = new List<IToken>();
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
                        var tokens = GroupTokensAndReturnWhereDone(TokenEnumerator, env).ToArray();
                        if (tokens[^1] == null) goto TokenError;
                        yield return ProcessTokens(tokens, env);
                        break;
                    case BracketTokenType.CloseBracket:
                        if (firstToken.TokenType != BracketTokenType.OpenBracket) goto TokenError;
                        foreach (var t in TokenCollection) yield return t;
                        TokenCollection.Clear();
                        yield break;
                    case BracketTokenType.CloseIndexBracket:
                        if (firstToken.TokenType != BracketTokenType.OpenIndexBracket) goto TokenError;
                        foreach (var t in TokenCollection) yield return t;
                        TokenCollection.Clear();
                        yield break;
                    default:
                        // Weird Bracket
                        Debugger.Break();
                        throw new ArgumentOutOfRangeException(nameof(TokenEnumerator));
                }
            }
            if (currentToken is CommaToken)
            {
                yield return new GrouppedToken { Tokens = TokenCollection.ToArray() };
                yield return currentToken;
                TokenCollection.Clear();
            } else TokenCollection.Add(currentToken);
        }
    TokenError:
        yield return null;
        yield break;
    }
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