// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.PreferIsKindAnalyzer,
    Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.Fixers.CSharpPreferIsKindFix>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.PreferIsKindAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.CodeFixes.BasicPreferIsKindFix>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class PreferIsKindAnalyzerTests
    {
        [Theory]
        [InlineData("==")]
        [InlineData("!=")]
        public async Task TestSimpleReturn_CS(string @operator)
        {
            var source =
$@"using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class C
{{
    bool Method(SyntaxNode node)
    {{
        return [|node.Kind()|] {@operator} SyntaxKind.None;
    }}
}}
";

            var prefix = @operator switch
            {
                "==" => "",
                "!=" => "!",
                _ => throw new InvalidOperationException(),
            };

            var fixedSource =
$@"using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class C
{{
    bool Method(SyntaxNode node)
    {{
        return {prefix}node.IsKind(SyntaxKind.None);
    }}
}}
";

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Theory]
        [InlineData("=")]
        [InlineData("<>")]
        public async Task TestSimpleReturn_VB(string @operator)
        {
            var source =
$@"Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Class C
    Function Method(node As SyntaxNode) As Boolean
        Return [|node.Kind()|] {@operator} SyntaxKind.None
    End Function
End Class
";

            var prefix = @operator switch
            {
                "=" => "",
                "<>" => "Not ",
                _ => throw new InvalidOperationException(),
            };

            var fixedSource =
$@"Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Class C
    Function Method(node As SyntaxNode) As Boolean
        Return {prefix}node.IsKind(SyntaxKind.None)
    End Function
End Class
";

            await VerifyVB.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task TestCompoundExpression_CS()
        {
            var source =
@"using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class C
{
    bool Method(SyntaxNode node)
    {
        return [|node.Kind()|] != SyntaxKind.None && [|node.Kind()|] != SyntaxKind.TrueKeyword;
    }
}
";
            var fixedSource =
@"using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class C
{
    bool Method(SyntaxNode node)
    {
        return !node.IsKind(SyntaxKind.None) && !node.IsKind(SyntaxKind.TrueKeyword);
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task TestCompoundExpression_VB()
        {
            var source =
@"Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Class C
    Function Method(node As SyntaxNode) As Boolean
        Return [|node.Kind()|] <> SyntaxKind.None AndAlso [|node.Kind()|] <> SyntaxKind.TrueKeyword
    End Function
End Class
";
            var fixedSource =
@"Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Class C
    Function Method(node As SyntaxNode) As Boolean
        Return Not node.IsKind(SyntaxKind.None) AndAlso Not node.IsKind(SyntaxKind.TrueKeyword)
    End Function
End Class
";

            await VerifyVB.VerifyCodeFixAsync(source, fixedSource);
        }

        [Theory]
        [InlineData("==")]
        [InlineData("!=")]
        public async Task TestCompoundExpression2_CS(string @operator)
        {
            var source =
$@"using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class C
{{
    bool Method(SyntaxNode node)
    {{
        return [|node.Kind()|] {@operator} SyntaxKind.None &&
            [|node.Kind()|] {@operator} SyntaxKind.TrueKeyword;
    }}
}}
";

            var prefix = @operator switch
            {
                "==" => "",
                "!=" => "!",
                _ => throw new InvalidOperationException(),
            };

            var fixedSource =
$@"using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class C
{{
    bool Method(SyntaxNode node)
    {{
        return {prefix}node.IsKind(SyntaxKind.None) &&
            {prefix}node.IsKind(SyntaxKind.TrueKeyword);
    }}
}}
";

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Theory]
        [InlineData("=")]
        [InlineData("<>")]
        public async Task TestCompoundExpression2_VB(string @operator)
        {
            var source =
$@"Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Class C
    Function Method(node As SyntaxNode) As Boolean
        Return [|node.Kind()|] {@operator} SyntaxKind.None AndAlso
            [|node.Kind()|] {@operator} SyntaxKind.TrueKeyword
    End Function
End Class
";

            var prefix = @operator switch
            {
                "=" => "",
                "<>" => "Not ",
                _ => throw new InvalidOperationException(),
            };

            var fixedSource =
$@"Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Class C
    Function Method(node As SyntaxNode) As Boolean
        Return {prefix}node.IsKind(SyntaxKind.None) AndAlso
            {prefix}node.IsKind(SyntaxKind.TrueKeyword)
    End Function
End Class
";

            await VerifyVB.VerifyCodeFixAsync(source, fixedSource);
        }

        [Theory]
        [InlineData("==")]
        [InlineData("!=")]
        public async Task TestCalledAsStaticMethod_CS(string @operator)
        {
            var source =
$@"using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class C
{{
    bool Method(SyntaxNode node)
    {{
        return [|Microsoft.CodeAnalysis.CSharp.CSharpExtensions.Kind(node)|] {@operator} SyntaxKind.None;
    }}
}}
";

            var prefix = @operator switch
            {
                "==" => "",
                "!=" => "!",
                _ => throw new InvalidOperationException(),
            };

            var fixedSource =
$@"using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class C
{{
    bool Method(SyntaxNode node)
    {{
        return {prefix}Microsoft.CodeAnalysis.CSharp.CSharpExtensions.{{|#0:IsKind|}}(node, SyntaxKind.None);
    }}
}}
";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedState =
                {
                    Sources = { fixedSource },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(8,63): error CS0117: 'CSharpExtensions' does not contain a definition for 'IsKind'
                        DiagnosticResult.CompilerError("CS0117").WithLocation(0).WithArguments("Microsoft.CodeAnalysis.CSharp.CSharpExtensions", "IsKind"),
                    },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("=")]
        [InlineData("<>")]
        public async Task TestCalledAsStaticMethod_VB(string @operator)
        {
            var source =
$@"Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Class C
    Function Method(node As SyntaxNode) As Boolean
        Return [|Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions.Kind(node)|] {@operator} SyntaxKind.None
    End Function
End Class
";

            var prefix = @operator switch
            {
                "=" => "",
                "<>" => "Not ",
                _ => throw new InvalidOperationException(),
            };

            var fixedSource =
$@"Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Class C
    Function Method(node As SyntaxNode) As Boolean
        Return {prefix}{{|#0:Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions.IsKind|}}(node, SyntaxKind.None)
    End Function
End Class
";

            await new VerifyVB.Test
            {
                TestCode = source,
                FixedState =
                {
                    Sources = { fixedSource },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.vb(6) : error BC30456: 'IsKind' is not a member of 'VisualBasicExtensions'.
                        DiagnosticResult.CompilerError("BC30456").WithLocation(0).WithArguments("IsKind", "Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions"),
                    },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("=")]
        [InlineData("<>")]
        public async Task TestVBWithoutParens(string @operator)
        {
            var source =
$@"Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Class C
    Function Method(node As SyntaxNode) As Boolean
        Return [|node.Kind|] {@operator} SyntaxKind.None
    End Function
End Class
";

            var prefix = @operator switch
            {
                "=" => "",
                "<>" => "Not ",
                _ => throw new InvalidOperationException(),
            };

            var fixedSource =
$@"Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Class C
    Function Method(node As SyntaxNode) As Boolean
        Return {prefix}node.IsKind(SyntaxKind.None)
    End Function
End Class
";

            await VerifyVB.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task TestSwitchStatement_CS()
        {
            var source =
@"using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class C
{
    void Method(SyntaxNode node)
    {
        switch (node.Kind())
        {
            case SyntaxKind.NewKeyword:
                break;

            case SyntaxKind.None:
                break;
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task TestSwitchStatement_VB()
        {
            var source =
@"Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Class C
    Sub Method(node As SyntaxNode)
        Select Case node.Kind()
            Case SyntaxKind.NewKeyword
                Return
            Case Else
                Return
        End Select
    End Sub
End Class
";

            await VerifyVB.VerifyCodeFixAsync(source, source);
        }
    }
}
