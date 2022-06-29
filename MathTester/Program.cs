// See https://aka.ms/new-console-template for more information

using MathTester;
using System.Diagnostics;
using Environment = MathTester.Environment;
Environment ev = new Environment
{
    Values =
    {
        { "x", new MatToken { Mat = new OpenCvSharp.Mat(10, 20, OpenCvSharp.MatType.CV_8UC3) } }
    }
};
string expr = "(1234.abs() + x +   10 ** 50 + abs(10) ^ 50   + 1234.567.clamp(-1, 1)) * 2/4";
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
    Debugger.Break();
    return;
}
Console.WriteLine($"Parse FunctionCall Tokens: {parsed}");
Console.WriteLine($"Evaluate: {parsed.Evaluate()}");