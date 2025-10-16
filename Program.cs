using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class Test
{
    static void Main()
    {
        // Create a separated list with multiple elements
        var list = SyntaxFactory.SeparatedList<SyntaxNode>(
            new[] {
                SyntaxFactory.IdentifierName("A"),
                SyntaxFactory.IdentifierName("B"),
                SyntaxFactory.IdentifierName("C"),
            });

        try
        {
            // Try to replace a separator that doesn't exist
            var badToken = SyntaxFactory.Token(SyntaxKind.SemicolonToken);
            list.ReplaceSeparator(badToken, SyntaxFactory.Token(SyntaxKind.CommaToken));
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Test 1 - Separator not found:");
            Console.WriteLine($"  Message: '{ex.Message}'");
            Console.WriteLine($"  ParamName: '{ex.ParamName}'");
            Console.WriteLine();
        }

        try
        {
            // Try to replace with wrong RawKind
            var firstSep = list.GetSeparator(0);
            var wrongSep = SyntaxFactory.Token(SyntaxKind.SemicolonToken);
            list.ReplaceSeparator(firstSep, wrongSep);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Test 2 - Wrong RawKind:");
            Console.WriteLine($"  Message: '{ex.Message}'");
            Console.WriteLine($"  ParamName: '{ex.ParamName}'");
            Console.WriteLine();
        }

        try
        {
            // Try to replace with wrong Language (VB token for C# list)
            var firstSep = list.GetSeparator(0);
            var vbComma = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.Token(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CommaToken);
            list.ReplaceSeparator(firstSep, vbComma);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Test 3 - Wrong Language:");
            Console.WriteLine($"  Message: '{ex.Message}'");
            Console.WriteLine($"  ParamName: '{ex.ParamName}'");
        }
    }
}
