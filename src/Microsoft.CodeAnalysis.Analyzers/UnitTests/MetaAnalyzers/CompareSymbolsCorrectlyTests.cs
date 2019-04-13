// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpSecurityCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.CompareSymbolsCorrectlyAnalyzer,
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers.CompareSymbolsCorrectlyFix>;
using VerifyVB = Test.Utilities.VisualBasicSecurityCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.CompareSymbolsCorrectlyAnalyzer,
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers.CompareSymbolsCorrectlyFix>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class CompareSymbolsCorrectlyTests
    {
        [Theory]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public async Task CompareTwoSymbolsEquals_CSharp(string symbolType)
        {
            var source = $@"
using Microsoft.CodeAnalysis;
class TestClass {{
    bool Method({symbolType} x, {symbolType} y) {{
        return [|x == y|];
    }}
}}
";
            var fixedSource = $@"
using Microsoft.CodeAnalysis;
class TestClass {{
    bool Method({symbolType} x, {symbolType} y) {{
        return Equals(x, y);
    }}
}}
";

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Theory]
        [WorkItem(2335, "https://github.com/dotnet/roslyn-analyzers/issues/2335")]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public async Task CompareTwoSymbolsByIdentity_CSharp(string symbolType)
        {
            var source = $@"
using Microsoft.CodeAnalysis;
class TestClass {{
    bool Method1({symbolType} x, {symbolType} y) {{
        return (object)x == y;
    }}
    bool Method2({symbolType} x, {symbolType} y) {{
        return x == (object)y;
    }}
    bool Method3({symbolType} x, {symbolType} y) {{
        return (object)x == (object)y;
    }}
}}
";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Theory]
        [CombinatorialData]
        public async Task CompareSymbolWithNull_CSharp(
            [CombinatorialValues(nameof(ISymbol), nameof(INamedTypeSymbol))] string symbolType,
            [CombinatorialValues("==", "!=")] string @operator,
            [CombinatorialValues("null", "default", "default(ISymbol)")] string value)
        {
            var source = $@"
using Microsoft.CodeAnalysis;
class TestClass {{
    bool Method1({symbolType} x) {{
        return x {@operator} {value};
    }}

    bool Method2({symbolType} x) {{
        return {value} {@operator} x;
    }}
}}
";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Theory]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public async Task CompareSymbolWithNullPattern_CSharp(string symbolType)
        {
            var source = $@"
using Microsoft.CodeAnalysis;
class TestClass {{
    bool Method1({symbolType} x) {{
        return x is null;
    }}
}}
";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Theory]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public async Task CompareTwoSymbolsEquals_VisualBasic(string symbolType)
        {
            var source = $@"
Imports Microsoft.CodeAnalysis
Class TestClass
    Function Method(x As {symbolType}, y As {symbolType}) As Boolean
        Return [|x Is y|]
    End Function
End Class
";
            var fixedSource = $@"
Imports Microsoft.CodeAnalysis
Class TestClass
    Function Method(x As {symbolType}, y As {symbolType}) As Boolean
        Return Equals(x, y)
    End Function
End Class
";

            await VerifyVB.VerifyCodeFixAsync(source, fixedSource);
        }

        [Theory]
        [WorkItem(2335, "https://github.com/dotnet/roslyn-analyzers/issues/2335")]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public async Task CompareTwoSymbolsByIdentity_VisualBasic(string symbolType)
        {
            var source = $@"
Imports Microsoft.CodeAnalysis
Class TestClass
    Function Method(x As {symbolType}, y As {symbolType}) As Boolean
        Return DirectCast(x, Object) Is y
    End Function
End Class
";

            await VerifyVB.VerifyAnalyzerAsync(source);
        }

        [Theory]
        [CombinatorialData]
        public async Task CompareSymbolWithNull_VisualBasic(
            [CombinatorialValues(nameof(ISymbol), nameof(INamedTypeSymbol))] string symbolType,
            [CombinatorialValues("Is", "IsNot")] string @operator)
        {
            var source = $@"
Imports Microsoft.CodeAnalysis
Class TestClass
    Function Method1(x As {symbolType}) As Boolean
        Return x {@operator} Nothing
    End Function

    Function Method2(x As {symbolType}) As Boolean
        Return Nothing {@operator} x
    End Function
End Class
";

            await VerifyVB.VerifyAnalyzerAsync(source);
        }
    }
}
