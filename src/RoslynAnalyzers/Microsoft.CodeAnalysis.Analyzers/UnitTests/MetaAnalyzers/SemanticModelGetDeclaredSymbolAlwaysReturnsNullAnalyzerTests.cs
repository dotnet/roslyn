﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.CSharpSemanticModelGetDeclaredSymbolAlwaysReturnsNullAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public sealed class SemanticModelGetDeclaredSymbolAlwaysReturnsNullAnalyzerTests
    {
        [Theory]
        [InlineData("AccessorListSyntax")]
        [InlineData("AwaitExpressionSyntax")]
        [InlineData("ForStatementSyntax")]
        [InlineData("GenericNameSyntax")]
        [InlineData("ParameterListSyntax")]
        [InlineData("LockStatementSyntax")]
        public Task Diagnostic(string type)
        {
            var code = $@"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class Test {{
    public void M(SemanticModel semanticModel, {type} syntax) {{
        var x = {{|#0:semanticModel.GetDeclaredSymbol(syntax)|}};
    }}
}}";

            return new VerifyCS.Test
            {
                TestCode = code,
                ExpectedDiagnostics = { new DiagnosticResult(CSharpSemanticModelGetDeclaredSymbolAlwaysReturnsNullAnalyzer.DiagnosticDescriptor).WithLocation(0).WithArguments(type) }
            }.RunAsync();
        }

        [Theory]
        [InlineData("BaseFieldDeclarationSyntax")]
        [InlineData("FieldDeclarationSyntax")]
        [InlineData("EventFieldDeclarationSyntax")]
        public Task Field_Diagnostic(string type)
        {
            var code = $@"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class Test {{
    public void M(SemanticModel semanticModel, {type} syntax) {{
        var x = {{|#0:semanticModel.GetDeclaredSymbol(syntax)|}};
    }}
}}";

            return new VerifyCS.Test
            {
                TestCode = code,
                ExpectedDiagnostics = { new DiagnosticResult(CSharpSemanticModelGetDeclaredSymbolAlwaysReturnsNullAnalyzer.FieldDiagnosticDescriptor).WithLocation(0).WithArguments(type) }
            }.RunAsync();
        }

        [Theory]
        [InlineData("SyntaxNode")]
        [InlineData("TypeDeclarationSyntax")]
        [InlineData("ClassDeclarationSyntax")]
        [InlineData("EnumMemberDeclarationSyntax")]
        [InlineData("NamespaceDeclarationSyntax")]
        public Task NoDiagnostic(string type)
        {
            var code = $@"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class Test {{
    public void M(SemanticModel semanticModel, {type} syntax) {{
        var x = semanticModel.GetDeclaredSymbol(syntax);
    }}
}}";

            return VerifyCS.VerifyAnalyzerAsync(code);
        }

        [Fact]
        public Task NoDiagnosticForCompilationError()
        {
            const string code = @"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class Test {
    public void M(SemanticModel semanticModel) {
        var x = semanticModel.{|CS7036:GetDeclaredSymbol|}();
    }
}";

            return VerifyCS.VerifyAnalyzerAsync(code);
        }

        [Fact, WorkItem(7061, "https://github.com/dotnet/roslyn-analyzers/issues/7061")]
        public Task LocalFunctionStatement_NoDiagnostic()
        {
            const string code = """
                       using Microsoft.CodeAnalysis;
                       using Microsoft.CodeAnalysis.CSharp.Syntax;

                       public class Test
                       {
                           public void M(SemanticModel semanticModel, LocalFunctionStatementSyntax syntax)
                           {
                               var x = semanticModel.GetDeclaredSymbol(syntax);
                           }
                       }
                       """;

            return VerifyCS.VerifyAnalyzerAsync(code);
        }
    }
}
