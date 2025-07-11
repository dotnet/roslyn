// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.CSharpDiagnosticAnalyzerApiUsageAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.BasicDiagnosticAnalyzerApiUsageAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class DiagnosticAnalyzerApiUsageAnalyzerTests
    {
        private static async Task VerifyCSharpAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AnalyzerConfigFiles =
                    {
                        ("/.globalconfig",
                        """
                        is_global = true

                        roslyn_correctness.assembly_reference_validation = relaxed
                        """),
                    }
                }
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private static async Task VerifyVisualBasicAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AnalyzerConfigFiles =
                    {
                        ("/.globalconfig",
                        """
                        is_global = true

                        roslyn_correctness.assembly_reference_validation = relaxed
                        """),
                    }
                }
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        [Fact]
        public async Task NoDiagnosticCasesAsync()
        {
            await VerifyCSharpAnalyzerAsync("""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using System.Threading.Tasks;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;

                interface I<T> { }

                internal delegate void MyDelegateGlobal();

                class MyAnalyzer : DiagnosticAnalyzer
                {
                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics=> throw new NotImplementedException();

                    internal delegate void MyDelegateInType();

                    public override void Initialize(AnalysisContext context)
                    {
                        var x = typeof(I<>);
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
                }
                """);

            await VerifyVisualBasicAnalyzerAsync("""
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

                Friend Delegate Sub MyDelegateGlobal()

                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer
                    Friend Delegate Sub MyDelegateInType()

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
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task DirectlyAccessedType_InDeclaration_DiagnosticAsync(bool relaxedValidation)
        {
            var csTest = new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        """
                        using System;
                        using System.Collections.Generic;
                        using System.Collections.Immutable;
                        using Microsoft.CodeAnalysis;
                        using Microsoft.CodeAnalysis.CSharp;
                        using Microsoft.CodeAnalysis.Diagnostics;

                        interface I<T> { }

                        class {|#0:MyAnalyzer|} : DiagnosticAnalyzer, I<Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider>
                        {
                            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

                            public override void Initialize(AnalysisContext context)
                            {
                            }
                        }
                        """,
                    },
                }
            };

            if (relaxedValidation)
            {
                csTest.TestState.AnalyzerConfigFiles.Add(
                    ("/.globalconfig",
                    """
                    is_global = true

                    roslyn_correctness.assembly_reference_validation = relaxed
                    """));
                csTest.ExpectedDiagnostics.Add(
                    // Test0.cs(11,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
                    GetCSharpExpectedDiagnostic(0, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));
            }

            await csTest.RunAsync();

            var vbTest = new VerifyVB.Test
            {
                TestState =
                {
                    Sources =
                    {
                        """
                        Imports System
                        Imports System.Collections.Generic
                        Imports System.Collections.Immutable
                        Imports Microsoft.CodeAnalysis
                        Imports Microsoft.CodeAnalysis.Diagnostics
                        Imports Microsoft.CodeAnalysis.VisualBasic

                        Interface I(Of T)
                        End Interface

                        Class {|#0:MyAnalyzer|}
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
                        """,
                    },
                }
            };

            if (relaxedValidation)
            {
                vbTest.TestState.AnalyzerConfigFiles.Add(
                    ("/.globalconfig",
                    """
                    is_global = true

                    roslyn_correctness.assembly_reference_validation = relaxed
                    """));
                vbTest.ExpectedDiagnostics.Add(
                    // Test0.vb(12,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
                    GetBasicExpectedDiagnostic(0, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));
            }

            await vbTest.RunAsync();
        }

        [Fact]
        public async Task DirectlyAccessedType_InMemberDeclaration_DiagnosticAsync()
        {
            await VerifyCSharpAnalyzerAsync("""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;

                class {|#0:MyAnalyzer|} : DiagnosticAnalyzer
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
                }
                """,
            // Test0.cs(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllContext, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider'.
            GetCSharpExpectedDiagnostic(0, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllContext, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider"));

            await VerifyVisualBasicAnalyzerAsync("""
                Imports System
                Imports System.Collections.Generic
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.CodeAnalysis.VisualBasic

                Class {|#0:MyAnalyzer|}
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
                """,

            // Test0.vb(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllContext, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider'.
            GetBasicExpectedDiagnostic(0, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllContext, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider"));
        }

        [Fact]
        public async Task DirectlyAccessedType_InMemberBody_DiagnosticAsync()
        {
            await VerifyCSharpAnalyzerAsync("""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;

                class {|#0:MyAnalyzer|} : DiagnosticAnalyzer
                {
                    void Method()
                    {
                        CodeFixProvider c = null;
                    }

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """,
            // Test0.cs(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetCSharpExpectedDiagnostic(0, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));

            await VerifyVisualBasicAnalyzerAsync("""
                Imports System
                Imports System.Collections.Generic
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.CodeAnalysis.VisualBasic

                Class {|#0:MyAnalyzer|}
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
                """,

            // Test0.vb(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetBasicExpectedDiagnostic(0, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));
        }

        [Fact]
        public async Task DirectlyAccessedType_StaticMember_DiagnosticAsync()
        {
            await VerifyCSharpAnalyzerAsync("""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;
                using static Microsoft.CodeAnalysis.CodeActions.WarningAnnotation;

                class {|#0:MyAnalyzer|} : DiagnosticAnalyzer
                {
                    void Method()
                    {
                        var c = Kind;
                    }

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """,
            // Test0.cs(11,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeActions.WarningAnnotation'.
            GetCSharpExpectedDiagnostic(0, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeActions.WarningAnnotation"));

            await VerifyVisualBasicAnalyzerAsync("""
                Imports System
                Imports System.Collections.Generic
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.CodeAnalysis.VisualBasic
                Imports WarningAnnotation = Microsoft.CodeAnalysis.CodeActions.WarningAnnotation

                Class {|#0:MyAnalyzer|}
                    Inherits DiagnosticAnalyzer

                    Sub Method()
                        Dim c = WarningAnnotation.Kind
                    End Sub

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Throw New NotImplementedException
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                    End Sub
                End Class
                """,
            // Test0.vb(11,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeActions.WarningAnnotation'.
            GetBasicExpectedDiagnostic(0, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeActions.WarningAnnotation"));
        }

        [Fact]
        public async Task DirectlyAccessedType_StaticMember_02_DiagnosticAsync()
        {
            await VerifyCSharpAnalyzerAsync("""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;
                using static Microsoft.CodeAnalysis.CodeFixes.WellKnownFixAllProviders;

                class {|#0:MyAnalyzer|} : DiagnosticAnalyzer
                {
                    void Method()
                    {
                        var c = BatchFixer;
                    }

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """,
            // Test0.cs(11,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.FixAllProvider, Microsoft.CodeAnalysis.CodeFixes.WellKnownFixAllProviders'.
            GetCSharpExpectedDiagnostic(0, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.FixAllProvider, Microsoft.CodeAnalysis.CodeFixes.WellKnownFixAllProviders"));

            await VerifyVisualBasicAnalyzerAsync("""
                Imports System
                Imports System.Collections.Generic
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.CodeAnalysis.VisualBasic
                Imports WellKnownFixAllProviders = Microsoft.CodeAnalysis.CodeFixes.WellKnownFixAllProviders

                Class {|#0:MyAnalyzer|}
                    Inherits DiagnosticAnalyzer

                    Sub Method()
                        Dim c = WellKnownFixAllProviders.BatchFixer
                    End Sub

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Throw New NotImplementedException
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                    End Sub
                End Class
                """,
            // Test0.vb(11,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.FixAllProvider, Microsoft.CodeAnalysis.CodeFixes.WellKnownFixAllProviders'.
            GetBasicExpectedDiagnostic(0, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.FixAllProvider, Microsoft.CodeAnalysis.CodeFixes.WellKnownFixAllProviders"));
        }

        [Fact]
        public async Task DirectlyAccessedType_CastAndTypeCheck_DiagnosticAsync()
        {
            await VerifyCSharpAnalyzerAsync("""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;

                class {|#0:MyAnalyzer|} : DiagnosticAnalyzer
                {
                    void Method(object o)
                    {
                        var c = (CodeFixProvider)o;
                        var c2 = o is FixAllProvider;
                    }

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """,
            // Test0.cs(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider'.
            GetCSharpExpectedDiagnostic(0, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider"));

            await VerifyVisualBasicAnalyzerAsync("""
                Imports System
                Imports System.Collections.Generic
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.CodeAnalysis.VisualBasic

                Class {|#0:MyAnalyzer|}
                    Inherits DiagnosticAnalyzer

                    Sub Method(o As Object)
                        Dim c = DirectCast(o, CodeFixProvider)
                        Dim c2 = TypeOf o Is FixAllProvider
                    End Sub

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Throw New NotImplementedException
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                    End Sub
                End Class
                """,
            // Test0.vb(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct accesses to type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider'.
            GetBasicExpectedDiagnostic(0, "MyAnalyzer", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider"));
        }

        [Fact]
        public Task IndirectlyAccessedType_StaticMember_DiagnosticAsync()
            => VerifyCSharpAnalyzerAsync("""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using System.Threading.Tasks;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;
                using static Fixer;

                class {|#0:MyAnalyzer|} : DiagnosticAnalyzer
                {
                    private string Value => FixerValue;

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }

                class Fixer : CodeFixProvider
                {
                    internal static readonly string FixerValue = "something";

                    public sealed override ImmutableArray<string> FixableDiagnosticIds => throw new NotImplementedException();

                    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
                    {
                        return null;
                    }

                }
                """,
            // Test0.cs(12,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Fixer', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixContext, Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetCSharpExpectedDiagnostic(0, "MyAnalyzer", "Fixer", "Microsoft.CodeAnalysis.CodeFixes.CodeFixContext, Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));

        [Fact]
        public async Task IndirectlyAccessedType_ExtensionMethod_DiagnosticAsync()
        {
            await VerifyCSharpAnalyzerAsync("""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;
                using static FixerExtensions;

                class {|#0:MyAnalyzer|} : DiagnosticAnalyzer
                {
                    void M()
                    {
                        this.ExtensionMethod();
                    }

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }

                public static class FixerExtensions
                {
                    public static void ExtensionMethod(this DiagnosticAnalyzer analyzer)
                    {
                        CodeFixProvider c = null;
                    }
                }
                """,
            // Test0.cs(11,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'FixerExtensions', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetCSharpExpectedDiagnostic(0, "MyAnalyzer", "FixerExtensions", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));

            await VerifyVisualBasicAnalyzerAsync("""
                Imports System
                Imports System.Collections.Generic
                Imports System.Collections.Immutable
                Imports System.Runtime.CompilerServices
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.CodeAnalysis.VisualBasic
                Imports FixerExtensions

                Class {|#0:MyAnalyzer|}
                    Inherits DiagnosticAnalyzer

                    Private Sub M()
                        Me.ExtensionMethod()
                    End Sub

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Throw New NotImplementedException
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                    End Sub
                End Class

                Module FixerExtensions
                    <Extension()>
                    Sub ExtensionMethod(analyzer As DiagnosticAnalyzer)
                        Dim c As CodeFixProvider = Nothing
                    End Sub
                End Module
                """,
            // Test0.vb(12,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'FixerExtensions', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetBasicExpectedDiagnostic(0, "MyAnalyzer", "FixerExtensions", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));
        }

        [Fact]
        public async Task IndirectlyAccessedType_InvokedFromAnalyzer_DiagnosticAsync()
        {
            await VerifyCSharpAnalyzerAsync("""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;

                class {|#0:MyAnalyzer|} : DiagnosticAnalyzer
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
                """,
            // Test0.cs(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class2', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetCSharpExpectedDiagnostic(0, "MyAnalyzer", "Class2", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));

            await VerifyVisualBasicAnalyzerAsync("""
                Imports System
                Imports System.Collections.Generic
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.CodeAnalysis.VisualBasic

                Class {|#0:MyAnalyzer|}
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
                """,

            // Test0.vb(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class2', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetBasicExpectedDiagnostic(0, "MyAnalyzer", "Class2", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));
        }

        [Fact]
        public async Task IndirectlyAccessedType_NotInvokedFromAnalyzer_DiagnosticAsync()
        {
            // We report diagnostic if there is a transitive access to a type referencing something from Workspaces.
            // This is regardless of whether the transitive access is actually reachable from a possible code path or not.

            await VerifyCSharpAnalyzerAsync("""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;

                class {|#0:MyAnalyzer|} : DiagnosticAnalyzer
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
                """,
            // Test0.cs(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class2, Class3', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider'.
            GetCSharpExpectedDiagnostic(0, "MyAnalyzer", "Class2, Class3", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider"));

            await VerifyVisualBasicAnalyzerAsync("""
                Imports System
                Imports System.Collections.Generic
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.CodeAnalysis.VisualBasic

                Class {|#0:MyAnalyzer|}
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
                """,
            // Test0.vb(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class2, Class3', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider'.
            GetBasicExpectedDiagnostic(0, "MyAnalyzer", "Class2, Class3", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider"));
        }

        [Fact]
        public async Task IndirectlyAccessedType_Transitive_DiagnosticAsync()
        {
            await VerifyCSharpAnalyzerAsync("""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;

                class {|#0:MyAnalyzer|} : DiagnosticAnalyzer
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
                """,
            // Test0.cs(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class3', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetCSharpExpectedDiagnostic(0, "MyAnalyzer", "Class3", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));

            await VerifyVisualBasicAnalyzerAsync("""
                Imports System
                Imports System.Collections.Generic
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.CodeAnalysis.VisualBasic

                Class {|#0:MyAnalyzer|}
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
                """,
            // Test0.vb(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class3', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider'.
            GetBasicExpectedDiagnostic(0, "MyAnalyzer", "Class3", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"));
        }

        [Fact]
        public async Task TypeDependencyGraphWithCycles_DiagnosticAsync()
        {
            await VerifyCSharpAnalyzerAsync("""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;

                class {|#0:MyAnalyzer|} : DiagnosticAnalyzer
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
                """,
            // Test0.cs(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class2, Class3', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider'.
            GetCSharpExpectedDiagnostic(0, "MyAnalyzer", "Class2, Class3", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider"));

            await VerifyVisualBasicAnalyzerAsync("""
                Imports System
                Imports System.Collections.Generic
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.CodeAnalysis.VisualBasic

                Class {|#0:MyAnalyzer|}
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
                """,
            // Test0.vb(10,7): warning RS1022: Change diagnostic analyzer type 'MyAnalyzer' to remove all direct and/or indirect accesses to type(s) 'Class2, Class3', which access type(s) 'Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider'.
            GetBasicExpectedDiagnostic(0, "MyAnalyzer", "Class2, Class3", "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, Microsoft.CodeAnalysis.CodeFixes.FixAllProvider"));
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int location, string analyzerTypeName, string violatingTypes) =>
            VerifyCS.Diagnostic(CSharpDiagnosticAnalyzerApiUsageAnalyzer.DoNotUseTypesFromAssemblyDirectRule)
                .WithLocation(location)
                .WithArguments(analyzerTypeName, violatingTypes);

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int location, string analyzerTypeName, string violatingIndirectTypes, string violatingTypes) =>
            VerifyCS.Diagnostic(CSharpDiagnosticAnalyzerApiUsageAnalyzer.DoNotUseTypesFromAssemblyIndirectRule)
                .WithLocation(location)
                .WithArguments(analyzerTypeName, violatingIndirectTypes, violatingTypes);

        private static DiagnosticResult GetBasicExpectedDiagnostic(int location, string analyzerTypeName, string violatingTypes) =>
            VerifyVB.Diagnostic(BasicDiagnosticAnalyzerApiUsageAnalyzer.DoNotUseTypesFromAssemblyDirectRule)
                .WithLocation(location)
                .WithArguments(analyzerTypeName, violatingTypes);

        private static DiagnosticResult GetBasicExpectedDiagnostic(int location, string analyzerTypeName, string violatingIndirectTypes, string violatingTypes) =>
            VerifyVB.Diagnostic(BasicDiagnosticAnalyzerApiUsageAnalyzer.DoNotUseTypesFromAssemblyIndirectRule)
                .WithLocation(location)
                .WithArguments(analyzerTypeName, violatingIndirectTypes, violatingTypes);
    }
}
