// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.CSharpSemanticModelGetDeclaredSymbolAlwaysReturnsNullAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public sealed class SemanticModelGetDeclaredSymbolAlwaysReturnsNullAnalyzerTests
    {
        [Theory]
        [InlineData("GlobalStatementSyntax")]
        [InlineData("IncompleteMemberSyntax")]
        [InlineData("BaseFieldDeclarationSyntax")]
        [InlineData("FieldDeclarationSyntax")]
        [InlineData("EventFieldDeclarationSyntax")]
        public Task Diagnostic(string type)
        {
            var code = $@"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class Test {{
    public void M(SemanticModel semanticModel, {type} syntax) {{
        var x = [|semanticModel.GetDeclaredSymbol(syntax)|];
    }}
}}";

            return VerifyCS.VerifyAnalyzerAsync(code);
        }

        [Theory]
        [InlineData("SyntaxNode")]
        [InlineData("TypeDeclarationSyntax")]
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
    }
}