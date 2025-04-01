﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
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
        public async Task TestSimpleReturn_CSAsync(string @operator)
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
        public async Task TestSimpleReturn_VBAsync(string @operator)
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
        public async Task TestCompoundExpression_CSAsync()
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
        public async Task TestCompoundExpression_VBAsync()
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
        public async Task TestCompoundExpression2_CSAsync(string @operator)
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
        public async Task TestCompoundExpression2_VBAsync(string @operator)
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
        public async Task TestCalledAsStaticMethod_CSAsync(string @operator)
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
        public async Task TestCalledAsStaticMethod_VBAsync(string @operator)
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
        public async Task TestVBWithoutParensAsync(string @operator)
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
        public async Task TestSwitchStatement_CSAsync()
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
        public async Task TestSwitchStatement_VBAsync()
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

        [Fact]
        [WorkItem(4946, "https://github.com/dotnet/roslyn-analyzers/issues/4946")]
        public async Task TestSingleNullConditionalAccess_CSAsync()
        {
            var source =
@"using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class C
{
    bool Method(SyntaxNode node)
    {
        return [|node?.Kind()|] == SyntaxKind.None;
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
        return node.IsKind(SyntaxKind.None);
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        [WorkItem(4946, "https://github.com/dotnet/roslyn-analyzers/issues/4946")]
        public async Task TestSingleNullConditionalAccess_VBAsync()
        {
            var source =
@"Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Class C
    Function Method(node As SyntaxNode) As Boolean
        Return [|node?.Kind()|] = SyntaxKind.None
    End Function
End Class
";
            var fixedSource =
@"Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Class C
    Function Method(node As SyntaxNode) As Boolean
        Return node.IsKind(SyntaxKind.None)
    End Function
End Class
";
            await VerifyVB.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        [WorkItem(4946, "https://github.com/dotnet/roslyn-analyzers/issues/4946")]
        public async Task TestSingleNullConditionalAccess_SyntaxToken_CSAsync()
        {
            var source =
@"using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class C
{
    bool Method(SyntaxToken? token)
    {
        return token?.Kind() == SyntaxKind.None;
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        [WorkItem(4946, "https://github.com/dotnet/roslyn-analyzers/issues/4946")]
        public async Task TestSingleNullConditionalAccess_SyntaxToken_VBAsync()
        {
            var source =
@"Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Class C
    Function Method(token As SyntaxToken?) As Boolean
        Return token?.Kind() = SyntaxKind.None
    End Function
End Class
";
            await VerifyVB.VerifyCodeFixAsync(source, source);
        }
    }
}
