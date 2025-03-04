// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.EnableConcurrentExecutionAnalyzer,
    Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.Fixers.CSharpEnableConcurrentExecutionFix>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.EnableConcurrentExecutionAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.CodeFixes.BasicEnableConcurrentExecutionFix>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class EnableConcurrentExecutionAnalyzerTests
    {
        [Fact]
        public async Task TestSimpleCase_CSharpAsync()
        {
            var code = @"
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

class Analyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw null;
    public override void Initialize(AnalysisContext [|context|])
    {
    }
}
";
            var fixedCode = @"
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

class Analyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw null;
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
        }

        [Fact]
        public async Task TestSimpleCase_VisualBasicAsync()
        {
            var code = @"
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

Class Analyzer
    Inherits DiagnosticAnalyzer

    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New System.Exception
        End Get
    End Property

    Public Overrides Sub Initialize([|context|] As AnalysisContext)
    End Sub
End Class
";
            var fixedCode = @"
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

Class Analyzer
    Inherits DiagnosticAnalyzer

    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New System.Exception
        End Get
    End Property

    Public Overrides Sub Initialize([|context|] As AnalysisContext)
        context.EnableConcurrentExecution()
    End Sub
End Class
";

            await VerifyVB.VerifyCodeFixAsync(code, fixedCode);
        }

        [Fact]
        public async Task RenamedMethod_CSharpAsync()
        {
            var code = @"
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

class Analyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw null;
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
    }

    public void NotInitialize(AnalysisContext context)
    {
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(code);
        }

        [Fact]
        public async Task RenamedMethod_VisualBasicAsync()
        {
            var code = @"
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

Class Analyzer
    Inherits DiagnosticAnalyzer

    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New System.Exception
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        context.EnableConcurrentExecution()
    End Sub

    Public Sub NotInitialize(context As AnalysisContext)
    End Sub
End Class
";

            await VerifyVB.VerifyAnalyzerAsync(code);
        }

        [Fact, WorkItem(2698, "https://github.com/dotnet/roslyn-analyzers/issues/2698")]
        public async Task RS1026_ExpressionBodiedMethodAsync()
        {
            var code = @"
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

class Analyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw null;
    public override void Initialize(AnalysisContext [|context|])
        => context.RegisterCompilationAction(x => { });
}
";
            var fixedCode = @"
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

class Analyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw null;
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(x => { });
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
        }
    }
}
