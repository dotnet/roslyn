// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpSecurityCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.CompareSymbolsCorrectlyAnalyzer,
    Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.Fixers.CSharpCompareSymbolsCorrectlyFix>;
using VerifyVB = Test.Utilities.VisualBasicSecurityCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.CompareSymbolsCorrectlyAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.Fixers.BasicCompareSymbolsCorrectlyFix>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class CompareSymbolsCorrectlyTests
    {
        private const string MinimalSymbolImplementationCSharp = """
            using System;
            using System.Collections.Immutable;
            using System.Globalization;
            using System.Threading;
            using Microsoft.CodeAnalysis;

            class Symbol : ISymbol {
                public SymbolKind Kind => throw new NotImplementedException();
                public string Language => throw new NotImplementedException();
                public string Name => throw new NotImplementedException();
                public string MetadataName => throw new NotImplementedException();
                public ISymbol ContainingSymbol => throw new NotImplementedException();
                public IAssemblySymbol ContainingAssembly => throw new NotImplementedException();
                public IModuleSymbol ContainingModule => throw new NotImplementedException();
                public INamedTypeSymbol ContainingType => throw new NotImplementedException();
                public INamespaceSymbol ContainingNamespace => throw new NotImplementedException();
                public bool IsDefinition => throw new NotImplementedException();
                public bool IsStatic => throw new NotImplementedException();
                public bool IsVirtual => throw new NotImplementedException();
                public bool IsOverride => throw new NotImplementedException();
                public bool IsAbstract => throw new NotImplementedException();
                public bool IsSealed => throw new NotImplementedException();
                public bool IsExtern => throw new NotImplementedException();
                public bool IsImplicitlyDeclared => throw new NotImplementedException();
                public bool CanBeReferencedByName => throw new NotImplementedException();
                public ImmutableArray<Location> Locations => throw new NotImplementedException();
                public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw new NotImplementedException();
                public Accessibility DeclaredAccessibility => throw new NotImplementedException();
                public ISymbol OriginalDefinition => throw new NotImplementedException();
                public bool HasUnsupportedMetadata => throw new NotImplementedException();

                public void Accept(SymbolVisitor visitor) => throw new NotImplementedException();
                public TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new NotImplementedException();
                public bool Equals(ISymbol other) => throw new NotImplementedException();
                public ImmutableArray<AttributeData> GetAttributes() => throw new NotImplementedException();
                public string GetDocumentationCommentId() => throw new NotImplementedException();
                public string GetDocumentationCommentXml(CultureInfo preferredCulture, bool expandIncludes, CancellationToken cancellationToken) => throw new NotImplementedException();
                public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat format) => throw new NotImplementedException();
                public string ToDisplayString(SymbolDisplayFormat format) => throw new NotImplementedException();
                public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format) => throw new NotImplementedException();
                public string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format) => throw new NotImplementedException();

                public static bool operator ==(Symbol left, Symbol right) => throw new NotImplementedException();
                public static bool operator !=(Symbol left, Symbol right) => throw new NotImplementedException();
            }
            """;
        private const string MinimalSymbolImplementationVisualBasic = """
            Imports System
            Imports System.Collections.Immutable
            Imports System.Globalization
            Imports System.Threading
            Imports Microsoft.CodeAnalysis

            Class Symbol
                Implements ISymbol

                Public ReadOnly Property Kind As SymbolKind Implements ISymbol.Kind
                Public ReadOnly Property Language As String Implements ISymbol.Language
                Public ReadOnly Property Name As String Implements ISymbol.Name
                Public ReadOnly Property MetadataName As String Implements ISymbol.MetadataName
                Public ReadOnly Property ContainingSymbol As ISymbol Implements ISymbol.ContainingSymbol
                Public ReadOnly Property ContainingAssembly As IAssemblySymbol Implements ISymbol.ContainingAssembly
                Public ReadOnly Property ContainingModule As IModuleSymbol Implements ISymbol.ContainingModule
                Public ReadOnly Property ContainingType As INamedTypeSymbol Implements ISymbol.ContainingType
                Public ReadOnly Property ContainingNamespace As INamespaceSymbol Implements ISymbol.ContainingNamespace
                Public ReadOnly Property IsDefinition As Boolean Implements ISymbol.IsDefinition
                Public ReadOnly Property IsStatic As Boolean Implements ISymbol.IsStatic
                Public ReadOnly Property IsVirtual As Boolean Implements ISymbol.IsVirtual
                Public ReadOnly Property IsOverride As Boolean Implements ISymbol.IsOverride
                Public ReadOnly Property IsAbstract As Boolean Implements ISymbol.IsAbstract
                Public ReadOnly Property IsSealed As Boolean Implements ISymbol.IsSealed
                Public ReadOnly Property IsExtern As Boolean Implements ISymbol.IsExtern
                Public ReadOnly Property IsImplicitlyDeclared As Boolean Implements ISymbol.IsImplicitlyDeclared
                Public ReadOnly Property CanBeReferencedByName As Boolean Implements ISymbol.CanBeReferencedByName
                Public ReadOnly Property Locations As ImmutableArray(Of Location) Implements ISymbol.Locations
                Public ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference) Implements ISymbol.DeclaringSyntaxReferences
                Public ReadOnly Property DeclaredAccessibility As Accessibility Implements ISymbol.DeclaredAccessibility
                Public ReadOnly Property OriginalDefinition As ISymbol Implements ISymbol.OriginalDefinition
                Public ReadOnly Property HasUnsupportedMetadata As Boolean Implements ISymbol.HasUnsupportedMetadata

                Public Sub Accept(visitor As SymbolVisitor) Implements ISymbol.Accept
                    Throw New NotImplementedException()
                End Sub

                Public Function GetAttributes() As ImmutableArray(Of AttributeData) Implements ISymbol.GetAttributes
                    Throw New NotImplementedException()
                End Function

                Public Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult Implements ISymbol.Accept
                    Throw New NotImplementedException()
                End Function

                Public Function GetDocumentationCommentId() As String Implements ISymbol.GetDocumentationCommentId
                    Throw New NotImplementedException()
                End Function

                Public Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String Implements ISymbol.GetDocumentationCommentXml
                    Throw New NotImplementedException()
                End Function

                Public Function ToDisplayString(Optional format As SymbolDisplayFormat = Nothing) As String Implements ISymbol.ToDisplayString
                    Throw New NotImplementedException()
                End Function

                Public Function ToDisplayParts(Optional format As SymbolDisplayFormat = Nothing) As ImmutableArray(Of SymbolDisplayPart) Implements ISymbol.ToDisplayParts
                    Throw New NotImplementedException()
                End Function

                Public Function ToMinimalDisplayString(semanticModel As SemanticModel, position As Integer, Optional format As SymbolDisplayFormat = Nothing) As String Implements ISymbol.ToMinimalDisplayString
                    Throw New NotImplementedException()
                End Function

                Public Function ToMinimalDisplayParts(semanticModel As SemanticModel, position As Integer, Optional format As SymbolDisplayFormat = Nothing) As ImmutableArray(Of SymbolDisplayPart) Implements ISymbol.ToMinimalDisplayParts
                    Throw New NotImplementedException()
                End Function

                Public Function Equals(other As ISymbol) As Boolean Implements IEquatable(Of ISymbol).Equals
                    Throw New NotImplementedException()
                End Function

                Public Shared Operator =(left As Symbol, right As Symbol) As Boolean
                    Throw New NotImplementedException()
                End Operator

                Public Shared Operator <>(left As Symbol, right As Symbol) As Boolean
                    Throw New NotImplementedException()
                End Operator
            End Class
            """;

        private const string SymbolEqualityComparerStubVisualBasic =
            """
            Imports System.Collections.Generic

            Namespace Microsoft
                Namespace CodeAnalysis
                    Public Class SymbolEqualityComparer
                        Implements IEqualityComparer(Of ISymbol)

                        Public Shared ReadOnly [Default] As SymbolEqualityComparer = New SymbolEqualityComparer()

                        Private Sub New()
                        End Sub

                        Public Function Equals(x As ISymbol, y As ISymbol) As Boolean Implements IEqualityComparer(Of ISymbol).Equals
                            Throw New System.NotImplementedException()
                        End Function

                        Public Function GetHashCode(obj As ISymbol) As Integer Implements IEqualityComparer(Of ISymbol).GetHashCode
                            Throw New System.NotImplementedException()
                        End Function
                    End Class
                End Namespace
            End Namespace
            """;

        private const string SymbolEqualityComparerStubCSharp =
            """
            using System.Collections.Generic;

            namespace Microsoft.CodeAnalysis
            {
                public class SymbolEqualityComparer : IEqualityComparer<ISymbol>
                {
                    public static readonly SymbolEqualityComparer Default = new SymbolEqualityComparer();

                    private SymbolEqualityComparer()
                    {
                    }

                    public bool Equals(ISymbol x, ISymbol y)
                    {
                        throw new System.NotImplementedException();
                    }

                    public int GetHashCode(ISymbol obj)
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """;

        [Theory]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public async Task CompareTwoSymbolsEquals_CSharpAsync(string symbolType)
        {
            var source = $$"""
                using Microsoft.CodeAnalysis;
                class TestClass {
                    bool Method({{symbolType}} x, {{symbolType}} y) {
                        return [|x == y|];
                    }
                }
                """;
            var fixedSource = $$"""
                using Microsoft.CodeAnalysis;
                class TestClass {
                    bool Method({{symbolType}} x, {{symbolType}} y) {
                        return SymbolEqualityComparer.Default.Equals(x, y);
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestState = { Sources = { source, SymbolEqualityComparerStubCSharp } },
                FixedState = { Sources = { fixedSource, SymbolEqualityComparerStubCSharp } },
            }.RunAsync();
        }

        [Theory]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public Task CompareTwoSymbolsEquals_NoComparer_CSharpAsync(string symbolType)
            => VerifyCS.VerifyCodeFixAsync($$"""
                using Microsoft.CodeAnalysis;
                class TestClass {
                    bool Method({{symbolType}} x, {{symbolType}} y) {
                        return [|x == y|];
                    }
                }
                """, $$"""
                using Microsoft.CodeAnalysis;
                class TestClass {
                    bool Method({{symbolType}} x, {{symbolType}} y) {
                        return Equals(x, y);
                    }
                }
                """);

        [Theory]
        [WorkItem(2335, "https://github.com/dotnet/roslyn-analyzers/issues/2335")]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public Task CompareTwoSymbolsByIdentity_CSharpAsync(string symbolType)
            => VerifyCS.VerifyAnalyzerAsync($$"""
                using Microsoft.CodeAnalysis;
                class TestClass {
                    bool Method1({{symbolType}} x, {{symbolType}} y) {
                        return (object)x == y;
                    }
                    bool Method2({{symbolType}} x, {{symbolType}} y) {
                        return x == (object)y;
                    }
                    bool Method3({{symbolType}} x, {{symbolType}} y) {
                        return (object)x == (object)y;
                    }
                }
                """);

        [Fact]
        [WorkItem(2336, "https://github.com/dotnet/roslyn-analyzers/issues/2336")]
        public async Task CompareTwoSymbolImplementations_CSharpAsync()
        {
            var source = $$"""
                class TestClass {
                    bool Method(Symbol x, Symbol y) {
                        return x == y;
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestState = { Sources = { source, MinimalSymbolImplementationCSharp } },
            }.RunAsync();
        }

        [Theory]
        [WorkItem(2336, "https://github.com/dotnet/roslyn-analyzers/issues/2336")]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public async Task CompareSymbolImplementationWithInterface_CSharpAsync(string symbolType)
        {
            var source = $$"""
                using Microsoft.CodeAnalysis;
                class TestClass {
                    bool Method(Symbol x, {{symbolType}} y) {
                        return [|x == y|];
                    }
                }
                """;
            var fixedSource = $$"""
                using Microsoft.CodeAnalysis;
                class TestClass {
                    bool Method(Symbol x, {{symbolType}} y) {
                        return SymbolEqualityComparer.Default.Equals(x, y);
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestState = { Sources = { source, MinimalSymbolImplementationCSharp, SymbolEqualityComparerStubCSharp } },
                FixedState = { Sources = { fixedSource, MinimalSymbolImplementationCSharp, SymbolEqualityComparerStubCSharp } },
            }.RunAsync();
        }

        [Theory]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public async Task CompareSymbolImplementationWithInterface_NoComparer_CSharpAsync(string symbolType)
        {
            var source = $$"""
                using Microsoft.CodeAnalysis;
                class TestClass {
                    bool Method(Symbol x, {{symbolType}} y) {
                        return [|x == y|];
                    }
                }
                """;
            var fixedSource = $$"""
                using Microsoft.CodeAnalysis;
                class TestClass {
                    bool Method(Symbol x, {{symbolType}} y) {
                        return Equals(x, y);
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestState = { Sources = { source, MinimalSymbolImplementationCSharp } },
                FixedState = { Sources = { fixedSource, MinimalSymbolImplementationCSharp } },
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public Task CompareSymbolWithNull_CSharpAsync(
            [CombinatorialValues(nameof(ISymbol), nameof(INamedTypeSymbol))] string symbolType,
            [CombinatorialValues("==", "!=")] string @operator,
            [CombinatorialValues("null", "default", "default(ISymbol)")] string value)
            => VerifyCS.VerifyAnalyzerAsync($$"""
                using Microsoft.CodeAnalysis;
                class TestClass {
                    bool Method1({{symbolType}} x) {
                        return x {{@operator}} {{value}};
                    }

                    bool Method2({{symbolType}} x) {
                        return {{value}} {{@operator}} x;
                    }
                }
                """);

        [Theory]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public Task CompareSymbolWithNullPattern_CSharpAsync(string symbolType)
            => VerifyCS.VerifyAnalyzerAsync($$"""
                using Microsoft.CodeAnalysis;
                class TestClass {
                    bool Method1({{symbolType}} x) {
                        return x is null;
                    }
                }
                """);

        [Theory]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public async Task CompareTwoSymbolsEquals_VisualBasicAsync(string symbolType)
        {
            var source = $"""
                Imports Microsoft.CodeAnalysis
                Class TestClass
                    Function Method(x As {symbolType}, y As {symbolType}) As Boolean
                        Return [|x Is y|]
                    End Function
                End Class
                """;
            var fixedSource = $"""
                Imports Microsoft.CodeAnalysis
                Class TestClass
                    Function Method(x As {symbolType}, y As {symbolType}) As Boolean
                        Return SymbolEqualityComparer.Default.Equals(x, y)
                    End Function
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState = { Sources = { source, SymbolEqualityComparerStubVisualBasic } },
                FixedState = { Sources = { fixedSource, SymbolEqualityComparerStubVisualBasic } },
            }.RunAsync();
        }

        [Theory]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public Task CompareTwoSymbolsEquals_NoComparer_VisualBasicAsync(string symbolType)
            => VerifyVB.VerifyCodeFixAsync($"""
                Imports Microsoft.CodeAnalysis
                Class TestClass
                    Function Method(x As {symbolType}, y As {symbolType}) As Boolean
                        Return [|x Is y|]
                    End Function
                End Class
                """, $"""
                Imports Microsoft.CodeAnalysis
                Class TestClass
                    Function Method(x As {symbolType}, y As {symbolType}) As Boolean
                        Return Equals(x, y)
                    End Function
                End Class
                """);

        [Theory]
        [WorkItem(2335, "https://github.com/dotnet/roslyn-analyzers/issues/2335")]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public Task CompareTwoSymbolsByIdentity_VisualBasicAsync(string symbolType)
            => VerifyVB.VerifyAnalyzerAsync($"""
                Imports Microsoft.CodeAnalysis
                Class TestClass
                    Function Method(x As {symbolType}, y As {symbolType}) As Boolean
                        Return DirectCast(x, Object) Is y
                    End Function
                End Class
                """);

        [Theory]
        [WorkItem(2336, "https://github.com/dotnet/roslyn-analyzers/issues/2336")]
        [CombinatorialData]
        public async Task CompareTwoSymbolImplementations_VisualBasicAsync(
            [CombinatorialValues("Symbol", nameof(ISymbol), nameof(INamedTypeSymbol))] string symbolType,
            [CombinatorialValues("=", "<>", "Is", "IsNot")] string @operator)
        {
            var source = $"""
                Imports Microsoft.CodeAnalysis
                Class TestClass
                    Function Method1(x As Symbol, y As {symbolType}) As Boolean
                        Return x {@operator} y
                    End Function

                    Function Method2(x As Symbol, y As {symbolType}) As Boolean
                        Return y {@operator} x
                    End Function
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState = { Sources = { source, MinimalSymbolImplementationVisualBasic } },
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public Task CompareSymbolWithNull_VisualBasicAsync(
            [CombinatorialValues(nameof(ISymbol), nameof(INamedTypeSymbol))] string symbolType,
            [CombinatorialValues("Is", "IsNot")] string @operator)
            => VerifyVB.VerifyAnalyzerAsync($"""
                Imports Microsoft.CodeAnalysis
                Class TestClass
                    Function Method1(x As {symbolType}) As Boolean
                        Return x {@operator} Nothing
                    End Function

                    Function Method2(x As {symbolType}) As Boolean
                        Return Nothing {@operator} x
                    End Function
                End Class
                """);

        [Theory]
        [CombinatorialData]
        public async Task CompareSymbolFromInstanceEquals_VisualBasicAsync(
            [CombinatorialValues(nameof(ISymbol), nameof(INamedTypeSymbol))] string symbolType,
            [CombinatorialValues("", "Not ")] string @operator)
        {
            var source = $"""
                Imports Microsoft.CodeAnalysis
                Class TestClass
                    Sub Method1(x As {symbolType}, y As {symbolType})
                        If {@operator}[|x.Equals(y)|] Then Exit Sub
                    End Sub
                End Class
                """;

            var fixedSource = $"""
                Imports Microsoft.CodeAnalysis
                Class TestClass
                    Sub Method1(x As {symbolType}, y As {symbolType})
                        If {@operator}SymbolEqualityComparer.Default.Equals(x, y) Then Exit Sub
                    End Sub
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState = { Sources = { source, SymbolEqualityComparerStubVisualBasic } },
                FixedState = { Sources = { fixedSource, SymbolEqualityComparerStubVisualBasic } },
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task CompareSymbolFromInstanceEquals_CSharpAsync(
            [CombinatorialValues(nameof(ISymbol), nameof(INamedTypeSymbol))] string symbolType,
            [CombinatorialValues("", "!")] string @operator)
        {
            var source = $$"""
                using Microsoft.CodeAnalysis;
                class TestClass
                {
                    void Method1({{symbolType}} x , {{symbolType}} y)
                    {
                        if ({{@operator}}[|x.Equals(y)|]) return;
                    }
                }
                """;

            var fixedSource = $$"""
                using Microsoft.CodeAnalysis;
                class TestClass
                {
                    void Method1({{symbolType}} x , {{symbolType}} y)
                    {
                        if ({{@operator}}SymbolEqualityComparer.Default.Equals(x, y)) return;
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestState = { Sources = { source, SymbolEqualityComparerStubCSharp } },
                FixedState = { Sources = { fixedSource, SymbolEqualityComparerStubCSharp } },
            }.RunAsync();
        }

        [Fact]
        public async Task CompareSymbolFromInstanceEqualsWithConditionalAccess_VisualBasicAsync()
        {
            var source = """
                Imports Microsoft.CodeAnalysis
                Class TestClass
                    Sub Method1(x As ISymbol, y As ISymbol)
                        If x?[|.Equals(y)|] Then Exit Sub
                    End Sub
                End Class
                """;

            var fixedSource = """
                Imports Microsoft.CodeAnalysis
                Class TestClass
                    Sub Method1(x As ISymbol, y As ISymbol)
                        If SymbolEqualityComparer.Default.Equals(x, y) Then Exit Sub
                    End Sub
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState = { Sources = { source, SymbolEqualityComparerStubVisualBasic } },
                FixedState = { Sources = { fixedSource, SymbolEqualityComparerStubVisualBasic } },
            }.RunAsync();
        }

        [Fact]
        public async Task CompareSymbolFromInstanceEqualsWithNullConditionalAccess_CSharpAsync()
        {
            var source = """
                using Microsoft.CodeAnalysis;
                class TestClass
                {
                    void Method1(ISymbol x, ISymbol y)
                    {
                        if (x?[|.Equals(y)|] == true) return;
                    }
                }
                """;

            var fixedSource = """
                using Microsoft.CodeAnalysis;
                class TestClass
                {
                    void Method1(ISymbol x, ISymbol y)
                    {
                        if (SymbolEqualityComparer.Default.Equals(x, y) == true) return;
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestState = { Sources = { source, SymbolEqualityComparerStubCSharp } },
                FixedState = { Sources = { fixedSource, SymbolEqualityComparerStubCSharp } },
            }.RunAsync();
        }

        [Fact]
        public async Task CompareSymbolFromInstanceEqualsWithChainConditionalAccess_VisualBasicAsync()
        {
            var source = """
                Imports Microsoft.CodeAnalysis

                Class A
                    Public b As B
                End Class

                Class B
                    Public s As ISymbol
                End Class

                Class TestClass
                    Sub Method1(a As A, b As B, s As ISymbol)
                        If a?.b?.s?[|.Equals(s)|] Then Exit Sub
                        If b?.s?[|.Equals(s)|] Then Exit Sub
                    End Sub
                End Class
                """;

            var fixedSource = """
                Imports Microsoft.CodeAnalysis

                Class A
                    Public b As B
                End Class

                Class B
                    Public s As ISymbol
                End Class

                Class TestClass
                    Sub Method1(a As A, b As B, s As ISymbol)
                        If SymbolEqualityComparer.Default.Equals(a?.b?.s, s) Then Exit Sub
                        If SymbolEqualityComparer.Default.Equals(b?.s, s) Then Exit Sub
                    End Sub
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState = { Sources = { source, SymbolEqualityComparerStubVisualBasic } },
                FixedState = { Sources = { fixedSource, SymbolEqualityComparerStubVisualBasic } },
            }.RunAsync();
        }

        [Fact]
        public async Task CompareSymbolFromInstanceEqualsWithChainNullConditionalAccess_CSharpAsync()
        {
            var source = """
                using Microsoft.CodeAnalysis;

                class A
                {
                    public B b;
                }

                class B
                {
                    public ISymbol s;
                }

                class TestClass
                {
                    void Method1(A a, B b, ISymbol s)
                    {
                        if (a?.b?.s?[|.Equals(s)|] == true) return;
                        if (b?.s?[|.Equals(s)|] == true) return;
                    }
                }
                """;

            var fixedSource = """
                using Microsoft.CodeAnalysis;

                class A
                {
                    public B b;
                }

                class B
                {
                    public ISymbol s;
                }

                class TestClass
                {
                    void Method1(A a, B b, ISymbol s)
                    {
                        if (SymbolEqualityComparer.Default.Equals(a?.b?.s, s) == true) return;
                        if (SymbolEqualityComparer.Default.Equals(b?.s, s) == true) return;
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestState = { Sources = { source, SymbolEqualityComparerStubCSharp } },
                FixedState = { Sources = { fixedSource, SymbolEqualityComparerStubCSharp } },
            }.RunAsync();
        }

        [Fact]
        public async Task CompareSymbolFromInstanceEqualsWithChain_CSharpAsync()
        {
            var source = """
                using Microsoft.CodeAnalysis;

                class A
                {
                    public B b;
                    public B GetB() => null;
                }

                class B
                {
                    public ISymbol s;
                    public ISymbol GetS() => null;
                }

                class TestClass
                {
                    void Method1(A a, ISymbol symbol)
                    {
                        if ([|a.b.s.Equals(symbol)|] == true) return;
                        if ([|a.GetB().GetS().Equals(symbol)|] == true) return;
                    }
                }
                """;

            var fixedSource = """
                using Microsoft.CodeAnalysis;

                class A
                {
                    public B b;
                    public B GetB() => null;
                }

                class B
                {
                    public ISymbol s;
                    public ISymbol GetS() => null;
                }

                class TestClass
                {
                    void Method1(A a, ISymbol symbol)
                    {
                        if (SymbolEqualityComparer.Default.Equals(a.b.s, symbol) == true) return;
                        if (SymbolEqualityComparer.Default.Equals(a.GetB().GetS(), symbol) == true) return;
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestState = { Sources = { source, SymbolEqualityComparerStubCSharp } },
                FixedState = { Sources = { fixedSource, SymbolEqualityComparerStubCSharp } },
            }.RunAsync();
        }

        [Theory]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public async Task CompareSymbolImplementationWithInterface_EqualsComparison_CSharpAsync(string symbolType)
        {
            var source = $$"""
                using Microsoft.CodeAnalysis;
                class TestClass {
                    bool Method(ISymbol x, {{symbolType}} y) {
                        return [|Equals(x, y)|];
                    }
                }
                """;
            var fixedSource = $$"""
                using Microsoft.CodeAnalysis;
                class TestClass {
                    bool Method(ISymbol x, {{symbolType}} y) {
                        return SymbolEqualityComparer.Default.Equals(x, y);
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestState = { Sources = { source, SymbolEqualityComparerStubCSharp } },
                FixedState = { Sources = { fixedSource, SymbolEqualityComparerStubCSharp } },
            }.RunAsync();
        }

        [Fact, WorkItem(2493, "https://github.com/dotnet/roslyn-analyzers/issues/2493")]
        public async Task GetHashCode_DiagnosticAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""
                using Microsoft.CodeAnalysis;
                public class C
                {
                    public int M(ISymbol symbol, INamedTypeSymbol namedType)
                    {
                        return [|symbol.GetHashCode()|] + [|namedType.GetHashCode()|];
                    }
                }
                """);

            await VerifyVB.VerifyAnalyzerAsync("""
                Imports Microsoft.CodeAnalysis

                Public Class C
                    Public Function M(ByVal symbol As ISymbol, ByVal namedType As INamedTypeSymbol) As Integer
                        Return [|symbol.GetHashCode()|] + [|namedType.GetHashCode()|]
                    End Function
                End Class
                """);
        }

        [Fact, WorkItem(2493, "https://github.com/dotnet/roslyn-analyzers/issues/2493")]
        public async Task CollectionConstructorsKnownToRequireComparer_DiagnosticAsync()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    ReferenceAssemblies = CreateNetCoreReferenceAssemblies(),
                    Sources =
                    {
                        """
                        using System.Collections.Concurrent;
                        using System.Collections.Generic;
                        using System.Linq;
                        using Microsoft.CodeAnalysis;

                        public class C
                        {
                            public void MethodWithDiagnostics(IEnumerable<ISymbol> symbols)
                            {
                                var kvps = symbols.Select(s => new KeyValuePair<ISymbol, int>(s, 0));

                                [|new Dictionary<ISymbol, int>()|];
                                [|new Dictionary<ISymbol, int>(42)|];
                                [|new Dictionary<ISymbol, int>(capacity: 42)|];
                                [|new Dictionary<ISymbol, int>(kvps)|];

                                [|new HashSet<ISymbol>()|];
                                [|new HashSet<ISymbol>(42)|];
                                [|new HashSet<ISymbol>(symbols)|];

                                [|new ConcurrentDictionary<ISymbol, int>()|];
                                [|new ConcurrentDictionary<ISymbol, int>(kvps)|];
                                [|new ConcurrentDictionary<ISymbol, int>(1, 42)|];
                            }

                            public void MethodWithoutDiagnostics()
                            {
                                new Dictionary<int, ISymbol>();
                                new HashSet<string>();
                                new ConcurrentDictionary<int, ISymbol>();

                                new Dictionary<ISymbol, int>(SymbolEqualityComparer.Default);
                            }
                        }
                        """,
                        SymbolEqualityComparerStubCSharp,
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        """
                        using System.Collections.Concurrent;
                        using System.Collections.Generic;
                        using System.Linq;
                        using Microsoft.CodeAnalysis;

                        public class C
                        {
                            public void MethodWithDiagnostics(IEnumerable<ISymbol> symbols)
                            {
                                var kvps = symbols.Select(s => new KeyValuePair<ISymbol, int>(s, 0));

                                new Dictionary<ISymbol, int>(SymbolEqualityComparer.Default);
                                new Dictionary<ISymbol, int>(42, SymbolEqualityComparer.Default);
                                new Dictionary<ISymbol, int>(capacity: 42, SymbolEqualityComparer.Default);
                                new Dictionary<ISymbol, int>(kvps, SymbolEqualityComparer.Default);

                                new HashSet<ISymbol>(SymbolEqualityComparer.Default);
                                new HashSet<ISymbol>(42, SymbolEqualityComparer.Default);
                                new HashSet<ISymbol>(symbols, SymbolEqualityComparer.Default);

                                new ConcurrentDictionary<ISymbol, int>(SymbolEqualityComparer.Default);
                                new ConcurrentDictionary<ISymbol, int>(kvps, SymbolEqualityComparer.Default);
                                new ConcurrentDictionary<ISymbol, int>(1, 42, SymbolEqualityComparer.Default);
                            }

                            public void MethodWithoutDiagnostics()
                            {
                                new Dictionary<int, ISymbol>();
                                new HashSet<string>();
                                new ConcurrentDictionary<int, ISymbol>();

                                new Dictionary<ISymbol, int>(SymbolEqualityComparer.Default);
                            }
                        }
                        """,
                        SymbolEqualityComparerStubCSharp,
                    }
                }
            }.RunAsync();

            await new VerifyVB.Test
            {
                TestState =
                {
                    ReferenceAssemblies = CreateNetCoreReferenceAssemblies(),
                    Sources =
                    {
                        """
                        Imports System.Collections.Concurrent
                        Imports System.Collections.Generic
                        Imports System.Linq
                        Imports Microsoft.CodeAnalysis

                        Public Class C
                            Public Sub MethodWithDiagnostics(symbols As IEnumerable(Of ISymbol))
                                Dim kvps = symbols.[Select](Function(s) New KeyValuePair(Of ISymbol, Integer)(s, 0))

                                Dim x11 = [|New Dictionary(Of ISymbol, Integer)()|]
                                Dim x12 = [|New Dictionary(Of ISymbol, Integer)(42)|]
                                Dim x13 = [|New Dictionary(Of ISymbol, Integer)(kvps)|]

                                Dim x21 = [|New HashSet(Of ISymbol)()|]
                                Dim x22 = [|New HashSet(Of ISymbol)(42)|]
                                Dim x23 = [|New HashSet(Of ISymbol)(symbols)|]

                                Dim x31 = [|New ConcurrentDictionary(Of ISymbol, Integer)()|]
                                Dim x32 = [|New ConcurrentDictionary(Of ISymbol, Integer)(kvps)|]
                                Dim x33 = [|New ConcurrentDictionary(Of ISymbol, Integer)(1, 42)|]
                            End Sub

                            Public Sub MethodWithoutDiagnostics()
                                Dim x1 = New Dictionary(Of Integer, ISymbol)()
                                Dim x2 = New HashSet(Of String)()
                                Dim x3 = New ConcurrentDictionary(Of Integer, ISymbol)()

                                Dim x4 = New Dictionary(Of ISymbol, Integer)(SymbolEqualityComparer.Default)
                            End Sub
                        End Class
                        """,
                        SymbolEqualityComparerStubVisualBasic,
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        """
                        Imports System.Collections.Concurrent
                        Imports System.Collections.Generic
                        Imports System.Linq
                        Imports Microsoft.CodeAnalysis

                        Public Class C
                            Public Sub MethodWithDiagnostics(symbols As IEnumerable(Of ISymbol))
                                Dim kvps = symbols.[Select](Function(s) New KeyValuePair(Of ISymbol, Integer)(s, 0))

                                Dim x11 = New Dictionary(Of ISymbol, Integer)(SymbolEqualityComparer.Default)
                                Dim x12 = New Dictionary(Of ISymbol, Integer)(42, SymbolEqualityComparer.Default)
                                Dim x13 = New Dictionary(Of ISymbol, Integer)(kvps, SymbolEqualityComparer.Default)

                                Dim x21 = New HashSet(Of ISymbol)(SymbolEqualityComparer.Default)
                                Dim x22 = New HashSet(Of ISymbol)(42, SymbolEqualityComparer.Default)
                                Dim x23 = New HashSet(Of ISymbol)(symbols, SymbolEqualityComparer.Default)

                                Dim x31 = New ConcurrentDictionary(Of ISymbol, Integer)(SymbolEqualityComparer.Default)
                                Dim x32 = New ConcurrentDictionary(Of ISymbol, Integer)(kvps, SymbolEqualityComparer.Default)
                                Dim x33 = New ConcurrentDictionary(Of ISymbol, Integer)(1, 42, SymbolEqualityComparer.Default)
                            End Sub

                            Public Sub MethodWithoutDiagnostics()
                                Dim x1 = New Dictionary(Of Integer, ISymbol)()
                                Dim x2 = New HashSet(Of String)()
                                Dim x3 = New ConcurrentDictionary(Of Integer, ISymbol)()

                                Dim x4 = New Dictionary(Of ISymbol, Integer)(SymbolEqualityComparer.Default)
                            End Sub
                        End Class
                        """,
                        SymbolEqualityComparerStubVisualBasic,
                    },
                }
            }.RunAsync();
        }

        [Fact, WorkItem(2493, "https://github.com/dotnet/roslyn-analyzers/issues/2493")]
        public async Task CollectionMethodsKnownToRequireComparer_DiagnosticAsync()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        """
                        using System;
                        using System.Collections.Immutable;
                        using System.Collections.Generic;
                        using System.Linq;
                        using Microsoft.CodeAnalysis;
                        public class C
                        {
                            public void MethodWithDiagnostics(IEnumerable<KeyValuePair<ISymbol, int>> kvps, IEnumerable<ISymbol> symbols, IEnumerable<ISymbol> symbols2, ISymbol symbol)
                            {
                                [|ImmutableHashSet.Create<ISymbol>()|];
                                [|ImmutableHashSet.CreateBuilder<ISymbol>()|];
                                [|ImmutableHashSet.CreateRange(symbols)|];
                                [|symbols.ToImmutableHashSet()|];

                                [|ImmutableDictionary.Create<ISymbol, int>()|];
                                [|ImmutableDictionary.CreateBuilder<ISymbol, int>()|];
                                [|ImmutableDictionary.CreateRange(kvps)|];
                                [|kvps.ToImmutableDictionary()|];

                                [|symbols.Contains(symbol)|];
                                [|symbols.Distinct()|];
                                [|symbols.GroupBy(x => x)|];
                                [|symbols.GroupJoin(symbols2, x => x, x => x, (x, y) => x)|];
                                [|symbols.Intersect(symbols2)|];
                                [|symbols.Join(symbols2, x => x, x => x, (x, y) => x)|];
                                [|symbols.SequenceEqual(symbols2)|];
                                [|symbols.ToDictionary(x => x)|];
                                [|symbols.ToLookup(x => x)|];
                                [|symbols.Union(symbols2)|];

                                [|ImmutableHashSet.ToImmutableHashSet(symbols)|];
                                symbols?[|.ToImmutableHashSet()|];
                            }

                            public void MethodWithoutDiagnostics(IEnumerable<KeyValuePair<int, ISymbol>> kvps, IEnumerable<int> integers, int integer)
                            {
                                ImmutableHashSet.Create<int>();
                                ImmutableHashSet.CreateBuilder<int>();
                                ImmutableHashSet.CreateRange(integers);
                                integers.ToImmutableHashSet();

                                ImmutableDictionary.Create<int, ISymbol>();
                                ImmutableDictionary.CreateBuilder<int, ISymbol>();
                                ImmutableDictionary.CreateRange(kvps);
                                kvps.ToImmutableDictionary();

                                integers.Contains(integer);
                                integers.Distinct();
                                integers.GroupBy(x => x);
                                integers.GroupJoin(integers, x => x, x => x, (x, y) => x);
                                integers.Intersect(integers);
                                integers.Join(integers, x => x, x => x, (x, y) => x);
                                integers.SequenceEqual(integers);
                                integers.ToDictionary(x => x);
                                integers.ToLookup(x => x);
                                integers.Union(integers);
                            }
                        }
                        """,
                        SymbolEqualityComparerStubCSharp,
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        """
                        using System;
                        using System.Collections.Immutable;
                        using System.Collections.Generic;
                        using System.Linq;
                        using Microsoft.CodeAnalysis;
                        public class C
                        {
                            public void MethodWithDiagnostics(IEnumerable<KeyValuePair<ISymbol, int>> kvps, IEnumerable<ISymbol> symbols, IEnumerable<ISymbol> symbols2, ISymbol symbol)
                            {
                                ImmutableHashSet.Create<ISymbol>(SymbolEqualityComparer.Default);
                                ImmutableHashSet.CreateBuilder<ISymbol>(SymbolEqualityComparer.Default);
                                ImmutableHashSet.CreateRange(SymbolEqualityComparer.Default, symbols);
                                symbols.ToImmutableHashSet(SymbolEqualityComparer.Default);

                                ImmutableDictionary.Create<ISymbol, int>(SymbolEqualityComparer.Default);
                                ImmutableDictionary.CreateBuilder<ISymbol, int>(SymbolEqualityComparer.Default);
                                ImmutableDictionary.CreateRange(SymbolEqualityComparer.Default, kvps);
                                kvps.ToImmutableDictionary(SymbolEqualityComparer.Default);

                                symbols.Contains(symbol, SymbolEqualityComparer.Default);
                                symbols.Distinct(SymbolEqualityComparer.Default);
                                symbols.GroupBy(x => x, SymbolEqualityComparer.Default);
                                symbols.GroupJoin(symbols2, x => x, x => x, (x, y) => x, SymbolEqualityComparer.Default);
                                symbols.Intersect(symbols2, SymbolEqualityComparer.Default);
                                symbols.Join(symbols2, x => x, x => x, (x, y) => x, SymbolEqualityComparer.Default);
                                symbols.SequenceEqual(symbols2, SymbolEqualityComparer.Default);
                                symbols.ToDictionary(x => x, SymbolEqualityComparer.Default);
                                symbols.ToLookup(x => x, SymbolEqualityComparer.Default);
                                symbols.Union(symbols2, SymbolEqualityComparer.Default);

                                ImmutableHashSet.ToImmutableHashSet(symbols, SymbolEqualityComparer.Default);
                                symbols?.ToImmutableHashSet(SymbolEqualityComparer.Default);
                            }

                            public void MethodWithoutDiagnostics(IEnumerable<KeyValuePair<int, ISymbol>> kvps, IEnumerable<int> integers, int integer)
                            {
                                ImmutableHashSet.Create<int>();
                                ImmutableHashSet.CreateBuilder<int>();
                                ImmutableHashSet.CreateRange(integers);
                                integers.ToImmutableHashSet();

                                ImmutableDictionary.Create<int, ISymbol>();
                                ImmutableDictionary.CreateBuilder<int, ISymbol>();
                                ImmutableDictionary.CreateRange(kvps);
                                kvps.ToImmutableDictionary();

                                integers.Contains(integer);
                                integers.Distinct();
                                integers.GroupBy(x => x);
                                integers.GroupJoin(integers, x => x, x => x, (x, y) => x);
                                integers.Intersect(integers);
                                integers.Join(integers, x => x, x => x, (x, y) => x);
                                integers.SequenceEqual(integers);
                                integers.ToDictionary(x => x);
                                integers.ToLookup(x => x);
                                integers.Union(integers);
                            }
                        }
                        """,
                        SymbolEqualityComparerStubCSharp,
                    },
                }
            }.RunAsync();

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources =
                    {
                        """
                        Imports System
                        Imports System.Collections.Immutable
                        Imports System.Collections.Generic
                        Imports System.Linq
                        Imports Microsoft.CodeAnalysis

                        Public Class C
                            Public Sub MethodWithDiagnostics(kvps As IEnumerable(Of KeyValuePair(Of ISymbol, Integer)), symbols As IEnumerable(Of ISymbol), symbols2 As IEnumerable(Of ISymbol), symbol As ISymbol)
                                Dim x1 = [|ImmutableHashSet.Create(Of ISymbol)()|]
                                Dim x2 = [|ImmutableHashSet.CreateBuilder(Of ISymbol)()|]
                                Dim x3 = [|ImmutableHashSet.CreateRange(symbols)|]
                                Dim x4 = [|symbols.ToImmutableHashSet()|]

                                Dim x5 = [|ImmutableDictionary.Create(Of ISymbol, Integer)()|]
                                Dim x6 = [|ImmutableDictionary.CreateBuilder(Of ISymbol, Integer)()|]
                                Dim x7 = [|ImmutableDictionary.CreateRange(kvps)|]
                                Dim x8 = [|kvps.ToImmutableDictionary()|]

                                Dim x9 = [|symbols.Contains(symbol)|]
                                Dim x10 = [|symbols.Distinct()|]
                                Dim x11 = [|symbols.GroupBy(Function(x) x)|]
                                Dim x12 = [|symbols.GroupJoin(symbols2, Function(x) x, Function(x) x, Function(x, y) x)|]
                                Dim x13 = [|symbols.Intersect(symbols2)|]
                                Dim x14 = [|symbols.Join(symbols2, Function(x) x, Function(x) x, Function(x, y) x)|]
                                Dim x15 = [|symbols.SequenceEqual(symbols2)|]
                                Dim x16 = [|symbols.ToDictionary(Function(x) x)|]
                                Dim x17 = [|symbols.ToLookup(Function(x) x)|]
                                Dim x18 = [|symbols.Union(symbols2)|]

                                Dim x19 = [|ImmutableHashSet.ToImmutableHashSet(symbols)|]
                            End Sub

                            Public Sub MethodWithoutDiagnostics(kvps As IEnumerable(Of KeyValuePair(Of Integer, ISymbol)), integers As IEnumerable(Of Integer), i As Integer)
                                Dim x1 = ImmutableHashSet.Create(Of Integer)()
                                Dim x2 = ImmutableHashSet.CreateBuilder(Of Integer)()
                                Dim x3 = ImmutableHashSet.CreateRange(integers)
                                Dim x4 = integers.ToImmutableHashSet()

                                Dim x5 = ImmutableDictionary.Create(Of Integer, ISymbol)()
                                Dim x6 = ImmutableDictionary.CreateBuilder(Of Integer, ISymbol)()
                                Dim x7 = ImmutableDictionary.CreateRange(kvps)
                                Dim x8 = kvps.ToImmutableDictionary()

                                Dim x9 = integers.Contains(i)
                                Dim x10 = integers.Distinct()
                                Dim x11 = integers.GroupBy(Function(x) x)
                                Dim x12 = integers.GroupJoin(integers, Function(x) x, Function(x) x, Function(x, y) x)
                                Dim x13 = integers.Intersect(integers)
                                Dim x14 = integers.Join(integers, Function(x) x, Function(x) x, Function(x, y) x)
                                Dim x15 = integers.SequenceEqual(integers)
                                Dim x16 = integers.ToDictionary(Function(x) x)
                                Dim x17 = integers.ToLookup(Function(x) x)
                                Dim x18 = integers.Union(integers)
                            End Sub
                        End Class
                        """,
                        SymbolEqualityComparerStubVisualBasic,
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        """
                        Imports System
                        Imports System.Collections.Immutable
                        Imports System.Collections.Generic
                        Imports System.Linq
                        Imports Microsoft.CodeAnalysis

                        Public Class C
                            Public Sub MethodWithDiagnostics(kvps As IEnumerable(Of KeyValuePair(Of ISymbol, Integer)), symbols As IEnumerable(Of ISymbol), symbols2 As IEnumerable(Of ISymbol), symbol As ISymbol)
                                Dim x1 = ImmutableHashSet.Create(Of ISymbol)(SymbolEqualityComparer.Default)
                                Dim x2 = ImmutableHashSet.CreateBuilder(Of ISymbol)(SymbolEqualityComparer.Default)
                                Dim x3 = ImmutableHashSet.CreateRange(SymbolEqualityComparer.Default, symbols)
                                Dim x4 = symbols.ToImmutableHashSet(SymbolEqualityComparer.Default)

                                Dim x5 = ImmutableDictionary.Create(Of ISymbol, Integer)(SymbolEqualityComparer.Default)
                                Dim x6 = ImmutableDictionary.CreateBuilder(Of ISymbol, Integer)(SymbolEqualityComparer.Default)
                                Dim x7 = ImmutableDictionary.CreateRange(SymbolEqualityComparer.Default, kvps)
                                Dim x8 = kvps.ToImmutableDictionary(SymbolEqualityComparer.Default)

                                Dim x9 = symbols.Contains(symbol, SymbolEqualityComparer.Default)
                                Dim x10 = symbols.Distinct(SymbolEqualityComparer.Default)
                                Dim x11 = symbols.GroupBy(Function(x) x, SymbolEqualityComparer.Default)
                                Dim x12 = symbols.GroupJoin(symbols2, Function(x) x, Function(x) x, Function(x, y) x, SymbolEqualityComparer.Default)
                                Dim x13 = symbols.Intersect(symbols2, SymbolEqualityComparer.Default)
                                Dim x14 = symbols.Join(symbols2, Function(x) x, Function(x) x, Function(x, y) x, SymbolEqualityComparer.Default)
                                Dim x15 = symbols.SequenceEqual(symbols2, SymbolEqualityComparer.Default)
                                Dim x16 = symbols.ToDictionary(Function(x) x, SymbolEqualityComparer.Default)
                                Dim x17 = symbols.ToLookup(Function(x) x, SymbolEqualityComparer.Default)
                                Dim x18 = symbols.Union(symbols2, SymbolEqualityComparer.Default)

                                Dim x19 = ImmutableHashSet.ToImmutableHashSet(symbols, SymbolEqualityComparer.Default)
                            End Sub

                            Public Sub MethodWithoutDiagnostics(kvps As IEnumerable(Of KeyValuePair(Of Integer, ISymbol)), integers As IEnumerable(Of Integer), i As Integer)
                                Dim x1 = ImmutableHashSet.Create(Of Integer)()
                                Dim x2 = ImmutableHashSet.CreateBuilder(Of Integer)()
                                Dim x3 = ImmutableHashSet.CreateRange(integers)
                                Dim x4 = integers.ToImmutableHashSet()

                                Dim x5 = ImmutableDictionary.Create(Of Integer, ISymbol)()
                                Dim x6 = ImmutableDictionary.CreateBuilder(Of Integer, ISymbol)()
                                Dim x7 = ImmutableDictionary.CreateRange(kvps)
                                Dim x8 = kvps.ToImmutableDictionary()

                                Dim x9 = integers.Contains(i)
                                Dim x10 = integers.Distinct()
                                Dim x11 = integers.GroupBy(Function(x) x)
                                Dim x12 = integers.GroupJoin(integers, Function(x) x, Function(x) x, Function(x, y) x)
                                Dim x13 = integers.Intersect(integers)
                                Dim x14 = integers.Join(integers, Function(x) x, Function(x) x, Function(x, y) x)
                                Dim x15 = integers.SequenceEqual(integers)
                                Dim x16 = integers.ToDictionary(Function(x) x)
                                Dim x17 = integers.ToLookup(Function(x) x)
                                Dim x18 = integers.Union(integers)
                            End Sub
                        End Class
                        """,
                        SymbolEqualityComparerStubVisualBasic,
                    },
                }
            }.RunAsync();
        }

        [Fact, WorkItem(4469, "https://github.com/dotnet/roslyn-analyzers/issues/4469")]
        public Task RS1024_SymbolEqualityComparerDefaultAsync()
            => new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        """
                        using System;
                        using System.Collections.Generic;
                        using System.Linq;
                        using Microsoft.CodeAnalysis;

                        public class C
                        {
                            public void M(IEnumerable<ISymbol> e, ISymbol symbol, INamedTypeSymbol type)
                            {
                                e.Contains(symbol, SymbolEqualityComparer.Default);

                                var asyncMethods = type.GetMembers()
                                    .OfType<IMethodSymbol>()
                                    .Where(x => x.IsAsync)
                                    .ToLookup(x => x.ContainingType, x => x, SymbolEqualityComparer.Default);
                            }
                        }
                        """,
                        SymbolEqualityComparerStubCSharp,
                    },
                },
            }.RunAsync();

        [Fact]
        [WorkItem(4470, "https://github.com/dotnet/roslyn-analyzers/issues/4470")]
        [WorkItem(4568, "https://github.com/dotnet/roslyn-analyzers/issues/4568")]
        public Task RS1024_InvocationArgumentTypeIsNullAsync()
            => new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        """
                        using System;
                        using System.Collections.Generic;
                        using System.Linq;
                        using Microsoft.CodeAnalysis;

                        public class C
                        {
                            private readonly HashSet<ITypeSymbol> _types = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
                        }
                        """,
                        SymbolEqualityComparerStubCSharp,
                    },
                },
            }.RunAsync();

        [Fact, WorkItem(4413, "https://github.com/dotnet/roslyn-analyzers/issues/4413")]
        public Task RS1024_SourceCollectionIsSymbolButLambdaIsNotAsync()
            => new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        """
                        using System;
                        using System.Collections.Generic;
                        using System.Linq;
                        using Microsoft.CodeAnalysis;

                        public class C
                        {
                            public void M1(IFieldSymbol[] fields)
                            {
                                var result = fields.ToLookup(f => f.Name);
                            }

                            public void M2(IEnumerable<IPropertySymbol> source, IEnumerable<IPropertySymbol> destination)
                            {
                                var result = source.Join(destination, p => (p.Name, p.Type.Name), p => (p.Name, p.Type.Name), (p1, p2) => p1.Name);
                            }
                        }
                        """,
                        SymbolEqualityComparerStubCSharp,
                    },
                },
            }.RunAsync();

        [Fact, WorkItem(4956, "https://github.com/dotnet/roslyn-analyzers/issues/4956")]
        public Task RS1024_StringGetHashCodeAsync()
            => new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        """
                        using System;

                        class C
                        {
                            void M()
                            {
                                ReadOnlySpan<char> testROS = default;
                                int hashCode = string.GetHashCode(testROS, StringComparison.OrdinalIgnoreCase);
                            }
                        }
                        """
                        , SymbolEqualityComparerStubCSharp
                    },
                    ReferenceAssemblies = CreateNetCoreReferenceAssemblies()
                }
            }.RunAsync();

        [Fact]
        public async Task RS1024_GetHashCodeOnInt64Async()
        {
            var code = """
                using System;
                using Microsoft.CodeAnalysis;

                static class HashCodeHelper
                {
                    public static Int32 GetHashCode(Int64 x) => 0;
                    public static Int32 GetHashCode(ISymbol symbol) => [|symbol.GetHashCode()|];
                }

                public class C
                {
                    public int GetHashCode(Int64 obj)
                    {
                        return HashCodeHelper.GetHashCode(obj);
                    }

                    public int GetHashCode(ISymbol symbol)
                    {
                        return HashCodeHelper.GetHashCode(symbol);
                    }

                    public int GetHashCode(object o)
                    {
                        if (o is ISymbol symbol)
                        {
                            return [|HashCode.Combine(symbol)|];
                        }

                        return HashCode.Combine(o);
                    }

                    public int GetHashCode(object o1, object o2)
                    {
                        if (o1 is ISymbol symbol1 && o2 is ISymbol symbol2)
                        {
                            return [|HashCode.Combine(symbol1, symbol2)|];
                        }

                        if (o1 is ISymbol symbolFirst)
                        {
                            return [|HashCode.Combine(symbolFirst, o2)|];
                        }

                        if (o2 is ISymbol symbolSecond)
                        {
                            return [|HashCode.Combine(o1, symbolSecond)|];
                        }

                        return HashCode.Combine(o1, o2);
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        code, SymbolEqualityComparerStubCSharp
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        code, SymbolEqualityComparerStubCSharp
                    },
                    MarkupHandling = Testing.MarkupMode.Allow
                },
                ReferenceAssemblies = CreateNetCoreReferenceAssemblies()
            }.RunAsync();
        }

        [Fact]
        [WorkItem(5715, "https://github.com/dotnet/roslyn-analyzers/issues/5715")]
        public async Task RS1024_CustomComparer_Instance_Is_InterfaceAsync()
        {
            var csCode = """
                using System.Collections.Generic;
                using System.Linq;
                using Microsoft.CodeAnalysis;

                public class C
                {
                    public void M(IEnumerable<ITypeSymbol> symbols)
                    {
                        _ = new HashSet<ISymbol>(SymbolNameComparer.Instance);
                        _ = symbols.ToDictionary(s => s, s => s.ToDisplayString(), SymbolNameComparer.Instance);
                        _ = symbols.ToDictionary(s => s, s => s.ToDisplayString(), SymbolEqualityComparer.Default);
                    }
                }

                internal sealed class SymbolNameComparer : EqualityComparer<ISymbol>
                {
                    private SymbolNameComparer() { }

                    internal static IEqualityComparer<ISymbol> Instance { get; } = new SymbolNameComparer();

                    public override bool Equals(ISymbol x, ISymbol y) => true;

                    public override int GetHashCode(ISymbol obj) => 0;
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = csCode,
                FixedCode = csCode,
                ReferenceAssemblies = CreateNetCoreReferenceAssemblies(),

            }.RunAsync();

            var vbCode = """
                Imports System.Collections.Generic
                Imports System.Linq
                Imports Microsoft.CodeAnalysis

                Public Class C

                    Public Sub M(symbols As IEnumerable(Of ITypeSymbol))
                        Dim x As New HashSet(Of ISymbol)(SymbolNameComparer.Instance)
                        Dim y = symbols.ToDictionary(Function(s) s, Function(s) s.ToDisplayString(), SymbolNameComparer.Instance)
                        Dim z = symbols.ToDictionary(Function(s) s, Function(s) s.ToDisplayString(), SymbolEqualityComparer.Default)
                    End Sub
                End Class

                Class SymbolNameComparer
                    Inherits EqualityComparer(Of ISymbol)

                    Private Sub New()
                    End Sub

                    Friend Shared Property Instance As IEqualityComparer(Of ISymbol) = New SymbolNameComparer()

                    Public Overrides Function Equals(x As ISymbol, y As ISymbol) As Boolean
                        Return True
                    End Function

                    Public Overrides Function GetHashCode(obj As ISymbol) As Integer
                        Return 0
                    End Function
                End Class
                """;

            await new VerifyVB.Test
            {
                TestCode = vbCode,
                FixedCode = vbCode,
                ReferenceAssemblies = CreateNetCoreReferenceAssemblies(),

            }.RunAsync();
        }

        [Fact]
        [WorkItem(5715, "https://github.com/dotnet/roslyn-analyzers/issues/5715")]
        public async Task RS1024_CustomComparer_Instance_Is_TypeAsync()
        {
            var csCode = """
                using System.Collections.Generic;
                using System.Linq;
                using Microsoft.CodeAnalysis;

                public class C
                {
                    public void M(IEnumerable<ITypeSymbol> symbols)
                    {
                        _ = new HashSet<ISymbol>(SymbolNameComparer.Instance);
                        _ = symbols.ToDictionary(s => s, s => s.ToDisplayString(), SymbolNameComparer.Instance);
                        _ = symbols.ToDictionary(s => s, s => s.ToDisplayString(), SymbolEqualityComparer.Default);
                    }
                }

                internal sealed class SymbolNameComparer : EqualityComparer<ISymbol>
                {
                    private SymbolNameComparer() { }

                    internal static SymbolNameComparer Instance { get; } = new SymbolNameComparer();

                    public override bool Equals(ISymbol x, ISymbol y) => true;

                    public override int GetHashCode(ISymbol obj) => 0;
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = csCode,
                FixedCode = csCode,
                ReferenceAssemblies = CreateNetCoreReferenceAssemblies(),

            }.RunAsync();

            var vbCode = """
                Imports System.Collections.Generic
                Imports System.Linq
                Imports Microsoft.CodeAnalysis

                Public Class C
                    Public Sub M(symbols As IEnumerable(Of ITypeSymbol))
                        Dim x As New HashSet(Of ISymbol)(SymbolNameComparer.Instance)
                        Dim y = symbols.ToDictionary(Function(s) s, Function(s) s.ToDisplayString(), SymbolNameComparer.Instance)
                        Dim z = symbols.ToDictionary(Function(s) s, Function(s) s.ToDisplayString(), SymbolEqualityComparer.Default)
                    End Sub
                End Class

                Class SymbolNameComparer
                    Inherits EqualityComparer(Of ISymbol)

                    Private Sub New()
                    End Sub

                    Friend Shared Property Instance As SymbolNameComparer = New SymbolNameComparer()

                    Public Overrides Function Equals(x As ISymbol, y As ISymbol) As Boolean
                        Return True
                    End Function

                    Public Overrides Function GetHashCode(obj As ISymbol) As Integer
                        Return 0
                    End Function
                End Class
                """;

            await new VerifyVB.Test
            {
                TestCode = vbCode,
                FixedCode = vbCode,
                ReferenceAssemblies = CreateNetCoreReferenceAssemblies(),

            }.RunAsync();
        }

        private static ReferenceAssemblies CreateNetCoreReferenceAssemblies()
            => ReferenceAssemblies.NetCore.NetCoreApp31.AddPackages(ImmutableArray.Create(
                new PackageIdentity("Microsoft.CodeAnalysis", "4.0.1"),
                new PackageIdentity("System.Runtime.Serialization.Formatters", "4.3.0"),
                new PackageIdentity("System.Configuration.ConfigurationManager", "4.7.0"),
                new PackageIdentity("System.Security.Cryptography.Cng", "4.7.0"),
                new PackageIdentity("System.Security.Permissions", "4.7.0"),
                new PackageIdentity("Microsoft.VisualBasic", "10.3.0")));
    }
}
