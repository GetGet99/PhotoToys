// See https://aka.ms/new-console-template for more information

using MathScript;
using System.Diagnostics;
using Environment = MathScript.Environment;
var x = new OpenCvSharp.Mat(100, 150, OpenCvSharp.MatType.CV_8UC3);
Environment ev = new Environment
{
    Values =
    {
        { "x", new MatToken { Mat = x } }
    }
};
Console.WriteLine($"x = {MatToken.FormatToString(x)}");
//string expr = "(1234.abs() + x +   10 ** 50 + abs(10) ^ 50   + 1234.567.clamp(-1, 1)) * 2/4";
//string expr = "-10.abs() ** 21 / 10 ** 20";
//string expr = "x.SubMat(..,100..).ToImage()";
string expr = "x[..,100..^(25 ^ 1)].ToImage()";
Console.WriteLine($"Starting Expression: {expr}");

var tokens = MathParser.GenerateSimpleTokens(expr, ev).ToArray();

Console.WriteLine("...............");
Console.WriteLine($"Tokens: {string.Join(' ', tokens.AsEnumerable())}");
Console.WriteLine("...............");

var grouppedTokens = MathParser.GroupTokens(tokens, ev).ToArray();
var parseFunctionCall = MathParser.Parse(grouppedTokens, ev);
if (parseFunctionCall is null)
{
    Console.WriteLine($"Parse FunctionCall returned null");
    return;
}
Debug.Assert(parseFunctionCall.Count == 1);
if (parseFunctionCall[0] is not IValueToken parsed)
{
    if (parseFunctionCall[0] is ErrorToken et)
    {
        Console.WriteLine($"Parse FunctionCall Tokens: {et}");
        return;
    }
    Debugger.Break();
    return;
}
Console.WriteLine($"Parse FunctionCall Tokens: {parsed}");
//return;
var eval = parsed.Evaluate();
Console.WriteLine($"Evaluate: {eval}");