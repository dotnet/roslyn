// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
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
        private const string MinimalSymbolImplementationCSharp = @"
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
";
        private const string MinimalSymbolImplementationVisualBasic = @"
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
";

        private const string SymbolEqualityComparerStubVisualBasic =
@"
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
End Namespace";

        private const string SymbolEqualityComparerStubCSharp =
@"
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
}";

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
        return SymbolEqualityComparer.Default.Equals(x, y);
    }}
}}
";
            await new VerifyCS.Test
            {
                TestState = { Sources = { source, SymbolEqualityComparerStubCSharp } },
                FixedState = { Sources = { fixedSource, SymbolEqualityComparerStubCSharp } },
            }.RunAsync();
        }

        [Theory]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public async Task CompareTwoSymbolsEquals_NoComparer_CSharp(string symbolType)
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

        [Fact]
        [WorkItem(2336, "https://github.com/dotnet/roslyn-analyzers/issues/2336")]
        public async Task CompareTwoSymbolImplementations_CSharp()
        {
            var source = $@"
class TestClass {{
    bool Method(Symbol x, Symbol y) {{
        return x == y;
    }}
}}
";

            await new VerifyCS.Test
            {
                TestState = { Sources = { source, MinimalSymbolImplementationCSharp } },
            }.RunAsync();
        }

        [Theory]
        [WorkItem(2336, "https://github.com/dotnet/roslyn-analyzers/issues/2336")]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public async Task CompareSymbolImplementationWithInterface_CSharp(string symbolType)
        {
            var source = $@"
using Microsoft.CodeAnalysis;
class TestClass {{
    bool Method(Symbol x, {symbolType} y) {{
        return [|x == y|];
    }}
}}
";
            var fixedSource = $@"
using Microsoft.CodeAnalysis;
class TestClass {{
    bool Method(Symbol x, {symbolType} y) {{
        return SymbolEqualityComparer.Default.Equals(x, y);
    }}
}}
";

            await new VerifyCS.Test
            {
                TestState = { Sources = { source, MinimalSymbolImplementationCSharp, SymbolEqualityComparerStubCSharp } },
                FixedState = { Sources = { fixedSource, MinimalSymbolImplementationCSharp, SymbolEqualityComparerStubCSharp } },
            }.RunAsync();
        }

        [Theory]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public async Task CompareSymbolImplementationWithInterface_NoComparer_CSharp(string symbolType)
        {
            var source = $@"
using Microsoft.CodeAnalysis;
class TestClass {{
    bool Method(Symbol x, {symbolType} y) {{
        return [|x == y|];
    }}
}}
";
            var fixedSource = $@"
using Microsoft.CodeAnalysis;
class TestClass {{
    bool Method(Symbol x, {symbolType} y) {{
        return Equals(x, y);
    }}
}}
";

            await new VerifyCS.Test
            {
                TestState = { Sources = { source, MinimalSymbolImplementationCSharp } },
                FixedState = { Sources = { fixedSource, MinimalSymbolImplementationCSharp } },
            }.RunAsync();
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
        Return SymbolEqualityComparer.Default.Equals(x, y)
    End Function
End Class
";

            await new VerifyVB.Test
            {
                TestState = { Sources = { source, SymbolEqualityComparerStubVisualBasic } },
                FixedState = { Sources = { fixedSource, SymbolEqualityComparerStubVisualBasic } },
            }.RunAsync();
        }

        [Theory]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public async Task CompareTwoSymbolsEquals_NoComparer_VisualBasic(string symbolType)
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
        [WorkItem(2336, "https://github.com/dotnet/roslyn-analyzers/issues/2336")]
        [CombinatorialData]
        public async Task CompareTwoSymbolImplementations_VisualBasic(
            [CombinatorialValues("Symbol", nameof(ISymbol), nameof(INamedTypeSymbol))] string symbolType,
            [CombinatorialValues("=", "<>", "Is", "IsNot")] string @operator)
        {
            var source = $@"
Imports Microsoft.CodeAnalysis
Class TestClass
    Function Method1(x As Symbol, y As {symbolType}) As Boolean
        Return x {@operator} y
    End Function

    Function Method2(x As Symbol, y As {symbolType}) As Boolean
        Return y {@operator} x
    End Function
End Class
";

            await new VerifyVB.Test
            {
                TestState = { Sources = { source, MinimalSymbolImplementationVisualBasic } },
            }.RunAsync();
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

        [Theory]
        [CombinatorialData]
        public async Task CompareSymbolFromInstanceEquals_VisualBasic(
            [CombinatorialValues(nameof(ISymbol), nameof(INamedTypeSymbol))] string symbolType,
            [CombinatorialValues("", "Not ")] string @operator)
        {
            var source = $@"
Imports Microsoft.CodeAnalysis
Class TestClass
    Sub Method1(x As {symbolType}, y As {symbolType})
        If {@operator}[|x.Equals(y)|] Then Exit Sub
    End Sub
End Class
";

            var fixedSource = $@"
Imports Microsoft.CodeAnalysis
Class TestClass
    Sub Method1(x As {symbolType}, y As {symbolType})
        If {@operator}SymbolEqualityComparer.Default.Equals(x, y) Then Exit Sub
    End Sub
End Class
";

            await new VerifyVB.Test
            {
                TestState = { Sources = { source, SymbolEqualityComparerStubVisualBasic } },
                FixedState = { Sources = { fixedSource, SymbolEqualityComparerStubVisualBasic } },
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task CompareSymbolFromInstanceEquals_CSharp(
            [CombinatorialValues(nameof(ISymbol), nameof(INamedTypeSymbol))] string symbolType,
            [CombinatorialValues("", "!")] string @operator)
        {
            var source = $@"
using Microsoft.CodeAnalysis;
class TestClass
{{
    void Method1({symbolType} x , {symbolType} y)
    {{
        if ({@operator}[|x.Equals(y)|]) return;
    }}
}}
";

            var fixedSource = $@"
using Microsoft.CodeAnalysis;
class TestClass
{{
    void Method1({symbolType} x , {symbolType} y)
    {{
        if ({@operator}SymbolEqualityComparer.Default.Equals(x, y)) return;
    }}
}}
";

            await new VerifyCS.Test
            {
                TestState = { Sources = { source, SymbolEqualityComparerStubCSharp } },
                FixedState = { Sources = { fixedSource, SymbolEqualityComparerStubCSharp } },
            }.RunAsync();
        }

        [Theory]
        [InlineData(nameof(ISymbol))]
        [InlineData(nameof(INamedTypeSymbol))]
        public async Task CompareSymbolImplementationWithInterface_EqualsComparison_CSharp(string symbolType)
        {
            var source = $@"
using Microsoft.CodeAnalysis;
class TestClass {{
    bool Method(ISymbol x, {symbolType} y) {{
        return [|Equals(x, y)|];
    }}
}}
";
            var fixedSource = $@"
using Microsoft.CodeAnalysis;
class TestClass {{
    bool Method(ISymbol x, {symbolType} y) {{
        return SymbolEqualityComparer.Default.Equals(x, y);
    }}
}}
";

            await new VerifyCS.Test
            {
                TestState = { Sources = { source, SymbolEqualityComparerStubCSharp } },
                FixedState = { Sources = { fixedSource, SymbolEqualityComparerStubCSharp } },
            }.RunAsync();
        }

        [Fact, WorkItem(2493, "https://github.com/dotnet/roslyn-analyzers/issues/2493")]
        public async Task GetHashCode_Diagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using Microsoft.CodeAnalysis;
public class C
{
    public int M(ISymbol symbol, INamedTypeSymbol namedType)
    {
        return [|symbol.GetHashCode()|] + [|namedType.GetHashCode()|];
    }
}");

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports Microsoft.CodeAnalysis

Public Class C
    Public Function M(ByVal symbol As ISymbol, ByVal namedType As INamedTypeSymbol) As Integer
        Return [|symbol.GetHashCode()|] + [|namedType.GetHashCode()|]
    End Function
End Class");
        }

        [Fact, WorkItem(2493, "https://github.com/dotnet/roslyn-analyzers/issues/2493")]
        public async Task CollectionConstructorsKnownToRequireComparer_Diagnostic()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
public class C
{
    public void MethodWithDiagnostics()
    {
        [|new Dictionary<ISymbol, int>()|];
        [|new HashSet<ISymbol>()|];
        [|new ConcurrentDictionary<ISymbol, int>()|];
    }

    public void MethodWithoutDiagnostics()
    {
        new Dictionary<int, ISymbol>();
        new HashSet<string>();
        new ConcurrentDictionary<int, ISymbol>();
    }
}",
                        SymbolEqualityComparerStubCSharp,
                    },
                },
            }.RunAsync();

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis

Public Class C
    Public Sub MethodWithDiagnostics()
        Dim x1 = [|New Dictionary(Of ISymbol, Integer)()|]
        Dim x2 = [|New HashSet(Of ISymbol)()|]
        Dim x3 = [|New ConcurrentDictionary(Of ISymbol, Integer)()|]
    End Sub

    Public Sub MethodWithoutDiagnostics()
        Dim x1 = New Dictionary(Of Integer, ISymbol)()
        Dim x2 = New HashSet(Of String)()
        Dim x3 = New ConcurrentDictionary(Of Integer, ISymbol)()
    End Sub
End Class",
                        SymbolEqualityComparerStubVisualBasic,
                    },
                },
            }.RunAsync();
        }

        [Fact, WorkItem(2493, "https://github.com/dotnet/roslyn-analyzers/issues/2493")]
        public async Task CollectionMethodsKnownToRequireComparer_Diagnostic()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"
using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
public class C
{
    public void MethodWithDiagnostics(IEnumerable<KeyValuePair<ISymbol, int>> kvps, IEnumerable<ISymbol> symbols, ISymbol symbol)
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
        [|symbols.GroupJoin(symbols, x => x, x => x, (x, y) => x)|];
        [|symbols.Intersect(symbols)|];
        [|symbols.Join(symbols, x => x, x => x, (x, y) => x)|];
        [|symbols.SequenceEqual(symbols)|];
        [|symbols.ToDictionary(x => x)|];
        [|symbols.ToLookup(x => x)|];
        [|symbols.Union(symbols)|];
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
}",
                        SymbolEqualityComparerStubCSharp,
                    },
                },
            }.RunAsync();

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"
Imports System
Imports System.Collections.Immutable
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.CodeAnalysis

Public Class C
    Public Sub MethodWithDiagnostics(kvps As IEnumerable(Of KeyValuePair(Of ISymbol, Integer)), symbols As IEnumerable(Of ISymbol), symbol As ISymbol)
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
        Dim x12 = [|symbols.GroupJoin(symbols, Function(x) x, Function(x) x, Function(x, y) x)|]
        Dim x13 = [|symbols.Intersect(symbols)|]
        Dim x14 = [|symbols.Join(symbols, Function(x) x, Function(x) x, Function(x, y) x)|]
        Dim x15 = [|symbols.SequenceEqual(symbols)|]
        Dim x16 = [|symbols.ToDictionary(Function(x) x)|]
        Dim x17 = [|symbols.ToLookup(Function(x) x)|]
        Dim x18 = [|symbols.Union(symbols)|]
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
End Class",
                        SymbolEqualityComparerStubVisualBasic,
                    },
                },
            }.RunAsync();
        }

        [Fact, WorkItem(4469, "https://github.com/dotnet/roslyn-analyzers/issues/4469")]
        public async Task RS1024_SymbolEqualityComparerDefault()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"
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
}",
                        SymbolEqualityComparerStubCSharp,
                    },
                },
            }.RunAsync();
        }

        [Fact, WorkItem(4470, "https://github.com/dotnet/roslyn-analyzers/issues/4470")]
        public async Task RS1024_InvocationArgumentTypeIsNull()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public class C
{
    private readonly HashSet<ITypeSymbol> _types = [|new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default)|];
}",
                        SymbolEqualityComparerStubCSharp,
                    },
                },
            }.RunAsync();
        }
    }
}
