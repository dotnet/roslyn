// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.CSharpDiagnosticAnalyzerFieldsAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.BasicDiagnosticAnalyzerFieldsAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class DoNotStorePerCompilationDataOntoFieldsRuleTests
    {
        [Fact]
        public async Task CSharp_VerifyDiagnostic()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using MyNamedType = Microsoft.CodeAnalysis.INamedTypeSymbol;

abstract class {|CS1729:MyCompilation|} : Compilation
{
    // Compile error: no public constructor exists on Compilation.
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly ITypeSymbol x1;
    public static readonly CSharpCompilation x2;
    private readonly List<MyNamedType> x3;
    private static Dictionary<MyCompilation, MyNamedType> x4;
    private static readonly IBinaryOperation x5;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}";
            DiagnosticResult[] expected = new[]
            {
                GetCSharpExpectedDiagnostic(19, 29, violatingTypeName: typeof(ITypeSymbol).FullName),
                GetCSharpExpectedDiagnostic(20, 28, violatingTypeName: typeof(CSharpCompilation).FullName),
                GetCSharpExpectedDiagnostic(21, 27, violatingTypeName: typeof(INamedTypeSymbol).FullName),
                GetCSharpExpectedDiagnostic(22, 31, violatingTypeName: "MyCompilation"),
                GetCSharpExpectedDiagnostic(23, 29, violatingTypeName: typeof(IBinaryOperation).FullName)
            };

            await VerifyCS.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task VisualBasic_VerifyDiagnostic()
        {
            var source = @"
Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic
Imports MyNamedType = Microsoft.CodeAnalysis.INamedTypeSymbol

MustInherit Class {|BC31399:MyCompilation|}
    Inherits Compilation ' Compile error: no public constructor exists on Compilation.
End Class

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Private Shared ReadOnly x1 As ITypeSymbol
    Public Shared ReadOnly x2 As VisualBasicCompilation
    Private ReadOnly x3 As List(Of MyNamedType)
    Private Shared x4 As Dictionary(Of MyCompilation, MyNamedType)
    Private Shared ReadOnly x5 As IBinaryOperation

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
    End Sub
End Class
";
            DiagnosticResult[] expected = new[]
            {
                GetBasicExpectedDiagnostic(19, 35, violatingTypeName: typeof(ITypeSymbol).FullName),
                GetBasicExpectedDiagnostic(20, 34, violatingTypeName: typeof(VisualBasicCompilation).FullName),
                GetBasicExpectedDiagnostic(21, 36, violatingTypeName: typeof(INamedTypeSymbol).FullName),
                GetBasicExpectedDiagnostic(22, 40, violatingTypeName: "MyCompilation"),
                GetBasicExpectedDiagnostic(23, 35, violatingTypeName: typeof(IBinaryOperation).FullName)
            };

            await VerifyVB.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task CSharp_NoDiagnosticCases()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MyNamedType = Microsoft.CodeAnalysis.INamedTypeSymbol;

abstract class {|CS1729:MyCompilation|} : Compilation
{
    // Compile error: no public constructor exists on Compilation.
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor x1;
    private readonly List<LocalizableResourceString> x2;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override void Initialize(AnalysisContext context)
    {
        var analyzer = new NestedCompilationAnalyzer();
        context.RegisterCompilationStartAction(analyzer.StartCompilation);
    }

    private class NestedCompilationAnalyzer
    {
        // Ok to store per-compilation data here.
        private readonly Dictionary<MyCompilation, MyNamedType> x;

        internal void StartCompilation(CompilationStartAnalysisContext context)
        {
        }
    }

    private struct NestedStructCompilationAnalyzer
    {
        // Ok to store per-compilation data here.
        private readonly Dictionary<MyCompilation, MyNamedType> y;

        internal void StartCompilation(CompilationStartAnalysisContext context)
        {
        }
    }
}

class MyAnalyzerWithoutAttribute : DiagnosticAnalyzer
{
    // Ok to store per-compilation data here.
    private static ITypeSymbol x;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override void Initialize(AnalysisContext context)
    {
        throw new NotImplementedException();
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task VisualBasic_NoDiagnosticCases()
        {
            var source = @"
Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports MyNamedType = Microsoft.CodeAnalysis.INamedTypeSymbol

MustInherit Class {|BC31399:MyCompilation|}
    Inherits Compilation ' Compile error: no public constructor exists on Compilation.
End Class

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Private Shared ReadOnly x1 As DiagnosticDescriptor
    Private ReadOnly x2 As List(Of LocalizableResourceString)

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        Dim compilationAnalyzer = New NestedCompilationAnalyzer
        context.RegisterCompilationStartAction(AddressOf compilationAnalyzer.StartCompilation)
    End Sub

    Class NestedCompilationAnalyzer
        ' Ok to store per-compilation data here.
        Private ReadOnly x As Dictionary(Of MyCompilation, MyNamedType)

        Friend Sub StartCompilation(context As CompilationStartAnalysisContext)
        End Sub
    End Class

    Structure NestedStructCompilationAnalyzer
        ' Ok to store per-compilation data here.
        Private ReadOnly y As Dictionary(Of MyCompilation, MyNamedType)

        Friend Sub StartCompilation(context As CompilationStartAnalysisContext)
        End Sub
    End Structure
End Class

Class MyAnalyzerWithoutAttribute
    Inherits DiagnosticAnalyzer

    ' Ok to store per-compilation data here.
    Private Shared x As ITypeSymbol

    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        Throw New NotImplementedException()
    End Sub
End Class
";

            await VerifyVB.VerifyAnalyzerAsync(source);
        }

        [Fact, WorkItem(4308, "https://github.com/dotnet/roslyn-analyzers/issues/4308")]
        public async Task CSharp_NestedStruct_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MyNamespace
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AnyInstanceInjectionAnalyzer : DiagnosticAnalyzer
    {
        public struct DependencyAccess
        {
            public IMethodSymbol method;
            public string expectedName;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        public void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var accessors = new ConcurrentBag<DependencyAccess>();

            context.RegisterSymbolAction(
                symbolContext => AnalyzeSymbol(symbolContext, accessors),
                SymbolKind.Property,
                SymbolKind.Field
            );

            context.RegisterSemanticModelAction(
                semanticModelContext => AnalyzeSemanticModel(semanticModelContext, accessors)
            );
        }

        public void AnalyzeSymbol(SymbolAnalysisContext context, ConcurrentBag<DependencyAccess> accessors)
        {
            // collect symbols for analysis
        }

        public void AnalyzeSemanticModel(SemanticModelAnalysisContext context, ConcurrentBag<DependencyAccess> accessors)
        {
            foreach (var access in accessors)
            {
                // analyze
            }
        }
    }
}");
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string violatingTypeName) =>
#pragma warning disable RS0030 // Do not used banned APIs
            VerifyCS.Diagnostic()
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not used banned APIs
                .WithArguments(violatingTypeName);

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string violatingTypeName) =>
#pragma warning disable RS0030 // Do not used banned APIs
            VerifyVB.Diagnostic()
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not used banned APIs
                .WithArguments(violatingTypeName);
    }
}
