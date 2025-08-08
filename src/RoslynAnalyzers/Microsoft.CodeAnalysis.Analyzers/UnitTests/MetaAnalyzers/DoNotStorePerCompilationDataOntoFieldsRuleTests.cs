// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        [Fact, WorkItem(7196, "https://github.com/dotnet/roslyn-analyzers/issues/7196")]
        public async Task CSharp_VerifyDiagnosticAsync()
        {
            DiagnosticResult[] expected =
            [
                GetCSharpExpectedDiagnostic(19, 29, violatingTypeName: typeof(ITypeSymbol).FullName),
                GetCSharpExpectedDiagnostic(20, 28, violatingTypeName: typeof(CSharpCompilation).FullName),
                GetCSharpExpectedDiagnostic(21, 27, violatingTypeName: typeof(INamedTypeSymbol).FullName),
                GetCSharpExpectedDiagnostic(22, 31, violatingTypeName: "MyCompilation"),
                GetCSharpExpectedDiagnostic(23, 29, violatingTypeName: typeof(IBinaryOperation).FullName),
                GetCSharpExpectedDiagnostic(24, 29, violatingTypeName: typeof(ISymbol).FullName),
                GetCSharpExpectedDiagnostic(25, 29, violatingTypeName: typeof(IOperation).FullName)
            ];

            await VerifyCS.VerifyAnalyzerAsync("""

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
                    private static readonly ISymbol x6;
                    private static readonly IOperation x7;

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
                }
                """, expected);
        }

        [Fact, WorkItem(7196, "https://github.com/dotnet/roslyn-analyzers/issues/7196")]
        public async Task VisualBasic_VerifyDiagnosticAsync()
        {
            DiagnosticResult[] expected =
            [
                GetBasicExpectedDiagnostic(19, 35, violatingTypeName: typeof(ITypeSymbol).FullName),
                GetBasicExpectedDiagnostic(20, 34, violatingTypeName: typeof(VisualBasicCompilation).FullName),
                GetBasicExpectedDiagnostic(21, 36, violatingTypeName: typeof(INamedTypeSymbol).FullName),
                GetBasicExpectedDiagnostic(22, 40, violatingTypeName: "MyCompilation"),
                GetBasicExpectedDiagnostic(23, 35, violatingTypeName: typeof(IBinaryOperation).FullName),
                GetBasicExpectedDiagnostic(24, 35, violatingTypeName: typeof(ISymbol).FullName),
                GetBasicExpectedDiagnostic(25, 35, violatingTypeName: typeof(IOperation).FullName)
            ];

            await VerifyVB.VerifyAnalyzerAsync("""

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
                    Private Shared ReadOnly x6 As ISymbol
                    Private Shared ReadOnly x7 As IOperation

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Throw New NotImplementedException
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                    End Sub
                End Class

                """, expected);
        }

        [Fact]
        public Task CSharp_NoDiagnosticCasesAsync()
            => VerifyCS.VerifyAnalyzerAsync("""

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
                }
                """);

        [Fact]
        public Task VisualBasic_NoDiagnosticCasesAsync()
            => VerifyVB.VerifyAnalyzerAsync("""

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

                """);

        [Fact, WorkItem(4308, "https://github.com/dotnet/roslyn-analyzers/issues/4308")]
        public Task CSharp_NestedStruct_NoDiagnosticAsync()
            => VerifyCS.VerifyAnalyzerAsync("""

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
                }
                """);

        [Theory]
        [InlineData("Func")]
        [InlineData("Action")]
        public Task CSharp_Func_NoDiagnostic(string delegateType)
            => VerifyCS.VerifyAnalyzerAsync($$"""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;
                using Microsoft.CodeAnalysis.Operations;

                [DiagnosticAnalyzer(LanguageNames.CSharp)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly {{delegateType}}<IBinaryOperation> x;

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
                }
                """);

        [Theory]
        [InlineData("Func")]
        [InlineData("Action")]
        public Task CSharp_NestedFunc_NoDiagnostic(string delegateType)
            => VerifyCS.VerifyAnalyzerAsync($$"""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;
                using Microsoft.CodeAnalysis.Operations;

                [DiagnosticAnalyzer(LanguageNames.CSharp)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly ImmutableArray<{{delegateType}}<IBinaryOperation>> x;

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
                }
                """);

        [Theory]
        [InlineData("Func")]
        [InlineData("Action")]
        public Task CSharp_NestedNestedFunc_NoDiagnostic(string delegateType)
            => VerifyCS.VerifyAnalyzerAsync($$"""

                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;
                using Microsoft.CodeAnalysis.Operations;

                [DiagnosticAnalyzer(LanguageNames.CSharp)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly {{delegateType}}<List<IBinaryOperation>> x;

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
                }
                """);

        [Theory]
        [CombinatorialData]
        public Task CSharp_MultiFunc_NoDiagnostic([CombinatorialValues("Func", "Action")] string delegateType, [CombinatorialValues("bool", "int, string")] string types)
            => VerifyCS.VerifyAnalyzerAsync($$"""

                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;
                using Microsoft.CodeAnalysis.Operations;

                [DiagnosticAnalyzer(LanguageNames.CSharp)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly {{delegateType}}<IBinaryOperation, {{types}}> x;

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
                }
                """);

        [Theory]
        [InlineData("Func")]
        [InlineData("Action")]
        public Task VisualBasic_Func_NoDiagnostic(string delegateType)
            => VerifyVB.VerifyAnalyzerAsync($"""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.CodeAnalysis.Operations
                Imports Microsoft.CodeAnalysis.VisualBasic

                <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly x1 As {delegateType}(Of IBinaryOperation)

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Throw New NotImplementedException
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)

                    End Sub
                End Class
                """);

        [Theory]
        [InlineData("Func")]
        [InlineData("Action")]
        public Task VisualBasic_NestedFunc_NoDiagnostic(string delegateType)
            => VerifyVB.VerifyAnalyzerAsync($"""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.CodeAnalysis.Operations
                Imports Microsoft.CodeAnalysis.VisualBasic

                <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly x1 As ImmutableArray(Of {delegateType}(Of IBinaryOperation))

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Throw New NotImplementedException
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)

                    End Sub
                End Class
                """);

        [Theory]
        [InlineData("Func")]
        [InlineData("Action")]
        public Task VisualBasic_NestedNestedFunc_NoDiagnostic(string delegateType)
            => VerifyVB.VerifyAnalyzerAsync($"""

                Imports System
                Imports System.Collections.Generic
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.CodeAnalysis.Operations
                Imports Microsoft.CodeAnalysis.VisualBasic

                <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly x1 As {delegateType}(Of List(Of IBinaryOperation))

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Throw New NotImplementedException
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)

                    End Sub
                End Class
                """);

        [Theory]
        [CombinatorialData]
        public Task VisualBasic_MultiFunc_NoDiagnostic([CombinatorialValues("Func", "Action")] string delegateType, [CombinatorialValues("Int32", "Int32, String")] string types)
            => VerifyVB.VerifyAnalyzerAsync($"""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.CodeAnalysis.Operations
                Imports Microsoft.CodeAnalysis.VisualBasic

                <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly x1 As {delegateType}(Of IBinaryOperation, {types})

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Throw New NotImplementedException
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)

                    End Sub
                End Class
                """);

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string violatingTypeName) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic()
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(violatingTypeName);

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string violatingTypeName) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyVB.Diagnostic()
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(violatingTypeName);
    }
}
