// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class DiagnosticAnalyzerApiUsageAnalyzerTests : CodeFixTestBase
    {
        [Fact]
        public void NoDiagnosticCases()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

interface I<T> { }

class MyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics=> throw new NotImplementedException();

    public override void Initialize(AnalysisContext context)
    {
    }
}

class MyFixer : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => throw new NotImplementedException();

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        return null;
    }
}

class MyFixer2 : I<CodeFixProvider>
{
    Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider field;
}");

            VerifyBasic(@"
Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

Interface I(Of T)
End Interface

Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
    End Sub
End Class

Class MyFixer
    Inherits CodeFixProvider

    Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public NotOverridable Overrides Function RegisterCodeFixesAsync(ByVal context As CodeFixContext) As Task
        Return Nothing
    End Function
End Class

Class MyFixer2
    Implements I(Of CodeFixProvider)

    Dim field As Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider 
End Class
");
        }

        [Fact]
        public void DirectlyAccessedType_InDeclaration_Diagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

interface I<T> { }

class MyAnalyzer : DiagnosticAnalyzer, I<Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider>
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

    public override void Initialize(AnalysisContext context)
    {
    }
}",
            // Test0.cs(11,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetCSharpExpectedDiagnostic(11, 7, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));

            VerifyBasic(@"
Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

Interface I(Of T)
End Interface

Class MyAnalyzer
    Inherits DiagnosticAnalyzer
    Implements I(Of Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider)

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
    End Sub
End Class
",
            // Test0.vb(12,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetBasicExpectedDiagnostic(12, 7, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));
        }

        [Fact]
        public void DirectlyAccessedType_InMemberDeclaration_Diagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

class MyAnalyzer : DiagnosticAnalyzer
{
    private readonly CodeFixProvider field;
    FixAllContext Method(FixAllProvider param)
    {
        return null;
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

    public override void Initialize(AnalysisContext context)
    {
    }
}",
            // Test0.cs(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllContext, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider'.
            GetCSharpExpectedDiagnostic(10, 7, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllContext, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider"));

            VerifyBasic(@"
Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Dim field As CodeFixProvider
    Function Method(param As FixAllProvider) As FixAllContext
        Return Nothing
    End Function

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
    End Sub
End Class
",

            // Test0.vb(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllContext, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider'.
            GetBasicExpectedDiagnostic(10, 7, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllContext, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider"));
        }

        [Fact]
        public void DirectlyAccessedType_InMemberBody_Diagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

class MyAnalyzer : DiagnosticAnalyzer
{
    void Method()
    {
        CodeFixProvider c = null;
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

    public override void Initialize(AnalysisContext context)
    {
    }
}",
            // Test0.cs(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetCSharpExpectedDiagnostic(10, 7, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));

            VerifyBasic(@"
Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Sub Method()
        Dim c As CodeFixProvider = Nothing
    End Sub

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
    End Sub
End Class
",

            // Test0.vb(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetBasicExpectedDiagnostic(10, 7, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));
        }

        [Fact]
        public void IndirectlyAccessedType_InvokedFromAnalyzer_Diagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

class MyAnalyzer : DiagnosticAnalyzer
{
    void Method(Class2 c)
    {
        c.M();
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

    public override void Initialize(AnalysisContext context)
    {
    }
}

class Class2
{
    public void M()
    {
        CodeFixProvider c = null;
    }
}
",
            // Test0.cs(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class2', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetCSharpExpectedDiagnostic(10, 7, "MyAnalyzer", "Class2", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));

            VerifyBasic(@"
Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Sub Method(c As Class2)
        c.M()
    End Sub

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
    End Sub
End Class

Class Class2
    Sub M()
        Dim c As CodeFixProvider = Nothing
    End Sub
End Class
",

            // Test0.vb(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class2', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetBasicExpectedDiagnostic(10, 7, "MyAnalyzer", "Class2", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));
        }

        [Fact]
        public void IndirectlyAccessedType_NotInvokedFromAnalyzer_Diagnostic()
        {
            // We report diagnostic if there is a transitive access to a type referencing something from Workspaces.
            // This is regardless of whether the transitive access is actually reachable from a possible code path or not.

            VerifyCSharp(@"
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

class MyAnalyzer : DiagnosticAnalyzer
{
    void Method(Class2 c2, Class3 c3)
    {
        c2.M2();
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

    public override void Initialize(AnalysisContext context)
    {
    }
}

class Class2
{
    public void M()
    {
        CodeFixProvider c = null;
    }

    
    public void M2()
    {
    }
}

class Class3
{
    public void M()
    {
        FixAllProvider c = null;
    }
}
",
            // Test0.cs(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class2, Class3', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider'.
            GetCSharpExpectedDiagnostic(10, 7, "MyAnalyzer", "Class2, Class3", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider"));

            VerifyBasic(@"
Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Sub Method(c2 As Class2, c3 As Class3)
        c2.M2()
    End Sub

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
    End Sub
End Class

Class Class2
    Sub M()
        Dim c As CodeFixProvider = Nothing
    End Sub

    Sub M2()
    End Sub
End Class

Class Class3
    Sub M()
        Dim c As FixAllProvider = Nothing
    End Sub
End Class
",
            // Test0.vb(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class2, Class3', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider'.
            GetBasicExpectedDiagnostic(10, 7, "MyAnalyzer", "Class2, Class3", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider"));
        }

        [Fact]
        public void IndirectlyAccessedType_Transitive_Diagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

class MyAnalyzer : DiagnosticAnalyzer
{
    void Method(Class2 c2)
    {
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

    public override void Initialize(AnalysisContext context)
    {
    }
}

class Class2
{    
    public void M2(Class3 c3)
    {
    }
}

class Class3
{
    public void M()
    {
        CodeFixProvider c = null;
    }
}
",
            // Test0.cs(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class3', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetCSharpExpectedDiagnostic(10, 7, "MyAnalyzer", "Class3", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));

            VerifyBasic(@"
Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Sub Method(c2 As Class2)
    End Sub

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
    End Sub
End Class

Class Class2
    Sub M2(c3 As Class3)
    End Sub
End Class

Class Class3
    Sub M()
        Dim c As CodeFixProvider = Nothing
    End Sub
End Class
",
            // Test0.vb(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class3', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetBasicExpectedDiagnostic(10, 7, "MyAnalyzer", "Class3", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));
        }

        [Fact]
        public void TypeDependencyGraphWithCycles_Diagnostic()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

class MyAnalyzer : DiagnosticAnalyzer
{
    void Method(Class2 c2, Class3 c3)
    {
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

    public override void Initialize(AnalysisContext context)
    {
    }
}

class Class2
{
    public void M()
    {
        CodeFixProvider c = null;
    }
    
    public void M2(Class3 c3)
    {
    }
}

class Class3
{
    public void M(Class2 c2)
    {
        FixAllProvider c = null;
    }
}
",
            // Test0.cs(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class2, Class3', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider'.
            GetCSharpExpectedDiagnostic(10, 7, "MyAnalyzer", "Class2, Class3", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider"));

            VerifyBasic(@"
Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Sub Method(c2 As Class2, c3 As Class3)
    End Sub

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
    End Sub
End Class

Class Class2
    Sub M()
        Dim c As CodeFixProvider = Nothing
    End Sub

    Sub M2(c3 As Class3)
    End Sub
End Class

Class Class3
    Sub M(c2 As Class2)
        Dim c As FixAllProvider = Nothing
    End Sub
End Class
",
            // Test0.vb(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class2, Class3', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider'.
            GetBasicExpectedDiagnostic(10, 7, "MyAnalyzer", "Class2, Class3", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider"));
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return null;
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return null;
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpDiagnosticAnalyzerApiUsageAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicDiagnosticAnalyzerApiUsageAnalyzer();
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string analyzerTypeName, string violatingTypes)
        {
            return GetExpectedDiagnostic(LanguageNames.CSharp, line, column, analyzerTypeName, violatingIndirectTypesOpt: null, violatingTypes: violatingTypes);
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string analyzerTypeName, string violatingIndirectTypes, string violatingTypes)
        {
            return GetExpectedDiagnostic(LanguageNames.CSharp, line, column, analyzerTypeName, violatingIndirectTypes, violatingTypes);
        }

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string analyzerTypeName, string violatingTypes)
        {
            return GetExpectedDiagnostic(LanguageNames.VisualBasic, line, column, analyzerTypeName, violatingIndirectTypesOpt: null, violatingTypes: violatingTypes);
        }

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string analyzerTypeName, string violatingIndirectTypes, string violatingTypes)
        {
            return GetExpectedDiagnostic(LanguageNames.VisualBasic, line, column, analyzerTypeName, violatingIndirectTypes, violatingTypes);
        }

        private static DiagnosticResult GetExpectedDiagnostic(string language, int line, int column, string analyzerTypeName, string violatingIndirectTypesOpt, string violatingTypes)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult
            {
                Id = DiagnosticIds.DoNotUseTypesFromAssemblyRuleId,
                Message = violatingIndirectTypesOpt == null ?
                    string.Format(CodeAnalysisDiagnosticsResources.DoNotUseTypesFromAssemblyRuleDirectMessage, analyzerTypeName, violatingTypes) :
                    string.Format(CodeAnalysisDiagnosticsResources.DoNotUseTypesFromAssemblyRuleIndirectMessage, analyzerTypeName, violatingIndirectTypesOpt, violatingTypes),
                Severity = DiagnosticHelpers.DefaultDiagnosticSeverity,
                Locations = new[]
                {
                    new DiagnosticResultLocation(fileName, line, column)
                }
            };
        }
    }
}
