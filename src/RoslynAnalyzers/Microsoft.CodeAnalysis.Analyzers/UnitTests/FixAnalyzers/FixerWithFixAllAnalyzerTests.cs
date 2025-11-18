// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Analyzers.FixAnalyzers;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.FixAnalyzers.FixerWithFixAllAnalyzer,
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers.FixerWithFixAllFix>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.FixAnalyzers.FixerWithFixAllAnalyzer,
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers.FixerWithFixAllFix>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.FixAnalyzers
{
    public class FixerWithFixAllAnalyzerTests
    {
        #region CSharp tests

        private const string CSharpCustomCodeActions = """

            public class MyCodeActionNoEquivalenceKey : CodeAction
            {
                public override string Title
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            public class MyCodeActionWithEquivalenceKey : CodeAction
            {
                public override string Title
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                public override string EquivalenceKey
                {
                    get
                    {
                        return "DummyEquivalenceKey";
                    }
                }
            }

            public abstract class MyAbstractCodeActionWithEquivalenceKey : CodeAction
            {
                public override string Title
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                public override string EquivalenceKey
                {
                    get
                    {
                        return "DummyEquivalenceKey";
                    }
                }
            }

            public class MyDerivedCodeActionWithEquivalenceKey : MyAbstractCodeActionWithEquivalenceKey
            {
            }

            """;
        private async Task TestCSharpCoreAsync(string source, DiagnosticResult missingGetFixAllProviderOverrideDiagnostic,
            bool withCustomCodeActions = false, params DiagnosticResult[] expected)
        {
            var fixAllProviderString = """
                public override FixAllProvider GetFixAllProvider()
                    {
                        return WellKnownFixAllProviders.BatchFixer;
                    }
                """;

            var sourceSuffix = """

                }
                """;

            if (withCustomCodeActions)
            {
                sourceSuffix += CSharpCustomCodeActions;
            }

            // Verify expected diagnostics for fixer that supports FixAllProvider.
            await VerifyCS.VerifyAnalyzerAsync(source + fixAllProviderString + sourceSuffix, expected);

            // Verify RS1016 (OverrideGetFixAllProviderRule) diagnostic for fixer that does not support FixAllProvider.
            expected = [missingGetFixAllProviderOverrideDiagnostic];
            await VerifyCS.VerifyAnalyzerAsync(source + sourceSuffix, expected);
        }

        [Fact]
        public async Task CSharp_CodeActionCreate_VerifyDiagnosticsAsync()
        {
            var expected = new[]
            {
                // Test0.cs(23,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(23, 29),
                // Test0.cs(24,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(24, 29),
                // Test0.cs(25,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(25, 29),
                // Test0.cs(28,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(28, 29),
                // Test0.cs(29,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(29, 29),
                // Test0.cs(30,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(30, 29)
            };

            // Test0.cs(10,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetCSharpOverrideGetFixAllProviderExpectedDiagnostic(10, 7, "C1");
            await TestCSharpCoreAsync("""

                using System;
                using System.Collections.Immutable;
                using System.Threading.Tasks;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CodeActions;

                [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C1))]
                class C1 : CodeFixProvider
                {
                    public override ImmutableArray<string> FixableDiagnosticIds
                    {
                        get
                        {
                            throw new NotImplementedException();
                        }
                    }

                    public override Task RegisterCodeFixesAsync(CodeFixContext context)
                    {
                        // Regular cases.
                        var codeAction1_1 = CodeAction.Create("Title1_1", _ => Task.FromResult(context.Document));
                        var codeAction1_2 = CodeAction.Create("Title1_2", createChangedDocument: _ => Task.FromResult(context.Document));
                        var codeAction1_3 = CodeAction.Create(createChangedDocument: _ => Task.FromResult(context.Document), title: "Title1_3");

                        // Null argument for equivalenceKey.
                        var codeAction2_1 = CodeAction.Create("Title2_1", _ => Task.FromResult(context.Document), null);
                        var codeAction2_2 = CodeAction.Create(createChangedDocument: _ => Task.FromResult(context.Document), equivalenceKey: null, title: "Title2_2");
                        var codeAction2_3 = CodeAction.Create("Title2_3", _ => Task.FromResult(context.Document), equivalenceKey: null);

                        return null;
                    }

                """, missingGetFixAllProviderOverrideDiagnostic, expected: expected);
        }

        [Fact]
        public async Task CSharp_CodeActionCreate_NoDiagnosticsAsync()
        {

            // Test0.cs(10,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetCSharpOverrideGetFixAllProviderExpectedDiagnostic(10, 7, "C1");

            await TestCSharpCoreAsync("""

                using System;
                using System.Collections.Immutable;
                using System.Threading.Tasks;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CodeActions;

                [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C1))]
                class C1 : CodeFixProvider
                {
                    public override ImmutableArray<string> FixableDiagnosticIds
                    {
                        get
                        {
                            throw new NotImplementedException();
                        }
                    }

                    public override Task RegisterCodeFixesAsync(CodeFixContext context)
                    {
                        // Overload resolution failure cases.
                        var codeAction1_1 = CodeAction.{|CS1501:Create|}("Title1_1");
                        var codeAction1_2 = CodeAction.{|CS7036:Create|}(createChangedDocument: _ => Task.FromResult(context.Document), equivalenceKey: null);

                        // Correct non-null arguments
                        var equivalenceKey = "equivalenceKey";
                        var codeAction2_1 = CodeAction.Create("Title2_1", _ => Task.FromResult(context.Document), equivalenceKey);
                        var codeAction2_2 = CodeAction.Create(title: "Title2_2", createChangedDocument: _ => Task.FromResult(context.Document), equivalenceKey: equivalenceKey);
                        var codeAction2_3 = CodeAction.Create(equivalenceKey: equivalenceKey, title: "Title2_3", createChangedDocument: _ => Task.FromResult(context.Document));

                        // Conservative no diagnostic cases.
                        string nullKey = null;
                        var codeAction3_1 = CodeAction.Create("Title3_1", _ => Task.FromResult(context.Document), nullKey);
                        var codeAction3_2 = CodeAction.Create("Title3_1", _ => Task.FromResult(context.Document), GetKey());

                        context.RegisterCodeFix(codeAction1_1, context.Diagnostics);
                        context.RegisterCodeFix(codeAction1_2, context.Diagnostics);

                        context.RegisterCodeFix(codeAction2_1, context.Diagnostics);
                        context.RegisterCodeFix(codeAction2_2, context.Diagnostics);
                        context.RegisterCodeFix(codeAction2_3, context.Diagnostics);

                        context.RegisterCodeFix(codeAction3_1, context.Diagnostics);
                        context.RegisterCodeFix(codeAction3_2, context.Diagnostics);

                        return null;
                    }

                    private string GetKey()
                    {
                        return null;
                    }

                """, missingGetFixAllProviderOverrideDiagnostic);
        }

        [Fact]
        public async Task CSharp_CodeActionCreate_DiagnosticsOnExportedCodeFixProviderTypeAsync()
        {
            var expected = new[]
            {
                // Test0.cs(26,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(26, 29)
            };

            // Test0.cs(10,16): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetCSharpOverrideGetFixAllProviderExpectedDiagnostic(10, 7, "C2");

            await TestCSharpCoreAsync("""

                using System;
                using System.Collections.Immutable;
                using System.Threading.Tasks;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CodeActions;

                [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C2))]
                class C2 : C1
                {
                }

                abstract class C1 : CodeFixProvider
                {
                    public override ImmutableArray<string> FixableDiagnosticIds
                    {
                        get
                        {
                            throw new NotImplementedException();
                        }
                    }

                    public override Task RegisterCodeFixesAsync(CodeFixContext context)
                    {
                        var codeAction1_1 = CodeAction.Create("Title1_1", _ => Task.FromResult(context.Document));
                        return null;
                    }

                """, missingGetFixAllProviderOverrideDiagnostic, expected: expected);
        }

        [Fact]
        public async Task CSharp_CustomCodeAction_VerifyDiagnosticsAsync()
        {
            var expected = new[]
            {
                // Test0.cs(22,26): warning RS1011: 'MyCodeActionNoEquivalenceKey' has the default value of 'null' for property 'EquivalenceKey'. Either override this property on 'MyCodeActionNoEquivalenceKey' to return a non-null and unique value across all code actions per-fixer or use such an existing code action.
                GetCSharpOverrideCodeActionEquivalenceKeyExpectedDiagnostic(22, 26, "MyCodeActionNoEquivalenceKey")
            };

            // Test0.cs(10,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetCSharpOverrideGetFixAllProviderExpectedDiagnostic(10, 7, "C1");

            await TestCSharpCoreAsync("""

                using System;
                using System.Collections.Immutable;
                using System.Threading.Tasks;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CodeActions;

                [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C1))]
                class C1 : CodeFixProvider
                {
                    public override ImmutableArray<string> FixableDiagnosticIds
                    {
                        get
                        {
                            throw new NotImplementedException();
                        }
                    }

                    public override Task RegisterCodeFixesAsync(CodeFixContext context)
                    {
                        var codeAction = new MyCodeActionNoEquivalenceKey();
                        return null;
                    }

                """, missingGetFixAllProviderOverrideDiagnostic, withCustomCodeActions: true, expected: expected);
        }

        [Fact]
        public async Task CSharp_CustomCodeAction_NoDiagnosticsAsync()
        {
            // Test0.cs(10,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetCSharpOverrideGetFixAllProviderExpectedDiagnostic(10, 7, "C1");

            await TestCSharpCoreAsync("""

                using System;
                using System.Collections.Immutable;
                using System.Threading.Tasks;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CodeActions;

                [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C1))]
                class C1 : CodeFixProvider
                {
                    public override ImmutableArray<string> FixableDiagnosticIds
                    {
                        get
                        {
                            throw new NotImplementedException();
                        }
                    }

                    public override Task RegisterCodeFixesAsync(CodeFixContext context)
                    {
                        CodeAction codeAction = new MyCodeActionWithEquivalenceKey();        
                        context.RegisterCodeFix(codeAction, context.Diagnostics);
                        
                        codeAction = new MyDerivedCodeActionWithEquivalenceKey();        
                        context.RegisterCodeFix(codeAction, context.Diagnostics);
                        return null;
                    }

                    private string GetKey()
                    {
                        return null;
                    }

                """, missingGetFixAllProviderOverrideDiagnostic, withCustomCodeActions: true);
        }

        [Fact, WorkItem(3475, "https://github.com/dotnet/roslyn-analyzers/issues/3475")]
        public Task CSharp_CodeActionCreateNestedActions_NoDiagnosticsAsync()
            => new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis", "3.3.0"))),
                TestCode = """

                using System;
                using System.Collections.Immutable;
                using System.Threading.Tasks;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CodeActions;

                [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C1))]
                class C1 : CodeFixProvider
                {
                    public override ImmutableArray<string> FixableDiagnosticIds
                    {
                        get
                        {
                            throw new NotImplementedException();
                        }
                    }

                    public override FixAllProvider GetFixAllProvider()
                    {
                        throw new NotImplementedException();
                    }

                    public override Task RegisterCodeFixesAsync(CodeFixContext context)
                    {
                        var c1 = CodeAction.Create(
                            "Title1",
                            ImmutableArray.Create(
                                CodeAction.Create("Title1_1", _ => Task.FromResult(context.Document), equivalenceKey: "Title1_1"),
                                {|#0:CodeAction.Create("Title1_2", _ => Task.FromResult(context.Document))|}
                            ),
                            false);

                        var c2 = CodeAction.Create(
                            "Title2",
                            ImmutableArray.Create(
                                CodeAction.Create("Title2_1", _ => Task.FromResult(context.Document), equivalenceKey: "Title2_1"),
                                {|#1:CodeAction.Create("Title2_2", _ => Task.FromResult(context.Document))|}
                            ),
                            true);

                        return null;
                    }
                }
                """,
                ExpectedDiagnostics =
                {
                    VerifyCS.Diagnostic(FixerWithFixAllAnalyzer.CreateCodeActionEquivalenceKeyRule).WithLocation(0).WithArguments("equivalenceKey"),
                    VerifyCS.Diagnostic(FixerWithFixAllAnalyzer.CreateCodeActionEquivalenceKeyRule).WithLocation(1).WithArguments("equivalenceKey"),
                }
            }.RunAsync();

        #endregion

        #region VisualBasic tests

        private const string VisualBasicCustomCodeActions = """


            Public Class MyCodeActionNoEquivalenceKey
            	Inherits CodeAction
            	Public Overrides ReadOnly Property Title() As String
            		Get
            			Throw New NotImplementedException()
            		End Get
            	End Property
            End Class

            Public Class MyCodeActionWithEquivalenceKey
            	Inherits CodeAction
            	Public Overrides ReadOnly Property Title() As String
            		Get
            			Throw New NotImplementedException()
            		End Get
            	End Property

            	Public Overrides ReadOnly Property EquivalenceKey() As String
            		Get
            			Return "DummyEquivalenceKey"
            		End Get
            	End Property
            End Class

            Public MustInherit Class MyAbstractCodeActionWithEquivalenceKey
            	Inherits CodeAction
            	Public Overrides ReadOnly Property Title() As String
            		Get
            			Throw New NotImplementedException()
            		End Get
            	End Property

            	Public Overrides ReadOnly Property EquivalenceKey() As String
            		Get
            			Return "DummyEquivalenceKey"
            		End Get
            	End Property
            End Class

            Public Class MyDerivedCodeActionWithEquivalenceKey
            	Inherits MyAbstractCodeActionWithEquivalenceKey
            End Class

            """;
        private async Task TestBasicCoreAsync(string source, DiagnosticResult missingGetFixAllProviderOverrideDiagnostic,
            bool withCustomCodeActions = false, params DiagnosticResult[] expected)
        {
            var fixAllProviderString = """
                Public Overrides Function GetFixAllProvider() As FixAllProvider
                	Return WellKnownFixAllProviders.BatchFixer
                End Function

                """;

            var sourceSuffix = """

                End Class

                """;

            if (withCustomCodeActions)
            {
                sourceSuffix += VisualBasicCustomCodeActions;
            }

            // Verify expected diagnostics for fixer that supports FixAllProvider.
            await VerifyVB.VerifyAnalyzerAsync(source + fixAllProviderString + sourceSuffix, expected);

            // Verify RS1016 (OverrideGetFixAllProviderRule) diagnostic for fixer that does not support FixAllProvider.
            expected = [missingGetFixAllProviderOverrideDiagnostic];
            await VerifyVB.VerifyAnalyzerAsync(source + sourceSuffix, expected);
        }

        [Fact]
        public async Task VisualBasic_CodeActionCreate_VerifyDiagnosticsAsync()
        {
            var expected = new[]
            {
                // Test0.vb(20,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(20, 23),
                // Test0.vb(21,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(21, 23),
                // Test0.vb(22,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(22, 23),
                // Test0.vb(25,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(25, 23),
                // Test0.vb(26,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(26, 23),
                // Test0.vb(27,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(27, 23)
            };

            // Test0.vb(10,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetBasicOverrideGetFixAllProviderExpectedDiagnostic(10, 7, "C1");

            await TestBasicCoreAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports System.Threading.Tasks
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.CodeActions

                <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(C1))>
                Class C1
                	Inherits CodeFixProvider
                	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                		' Regular cases.
                		Dim codeAction1_1 = CodeAction.Create("Title1_1", Function(x) Task.FromResult(context.Document))
                		Dim codeAction1_2 = CodeAction.Create("Title1_2", createChangedDocument := Function(x) Task.FromResult(context.Document))
                		Dim codeAction1_3 = CodeAction.Create(createChangedDocument := Function(x) Task.FromResult(context.Document), title := "Title1_3")

                		' Null argument for equivalenceKey.
                		Dim codeAction2_1 = CodeAction.Create("Title2_1", Function(x) Task.FromResult(context.Document), Nothing)
                		Dim codeAction2_2 = CodeAction.Create(createChangedDocument := Function(x) Task.FromResult(context.Document), equivalenceKey := Nothing, title := "Title2_2")
                		Dim codeAction2_3 = CodeAction.Create("Title2_3", Function(x) Task.FromResult(context.Document), equivalenceKey := Nothing)

                		Return Nothing
                	End Function

                """, missingGetFixAllProviderOverrideDiagnostic, expected: expected);
        }

        [Fact]
        public async Task VisualBasic_CodeActionCreate_NoDiagnosticsAsync()
        {
            // Test0.vb(10,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetBasicOverrideGetFixAllProviderExpectedDiagnostic(10, 7, "C1");

            await TestBasicCoreAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports System.Threading.Tasks
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.CodeActions

                <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(C1))>
                Class C1
                	Inherits CodeFixProvider
                	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                		' Overload resolution failure cases.
                		Dim codeAction1_1 = CodeAction.{|BC30516:Create|}("Title1_1")
                		Dim codeAction1_2 = CodeAction.{|BC30518:Create|}(createChangedDocument := Function(x) Task.FromResult(context.Document), equivalenceKey := Nothing)

                		' Correct non-null arguments
                		Dim equivalenceKey = "equivalenceKey"
                		Dim codeAction2_1 = CodeAction.Create("Title2_1", Function(x) Task.FromResult(context.Document), equivalenceKey)
                		Dim codeAction2_2 = CodeAction.Create(title := "Title2_2", createChangedDocument := Function(x) Task.FromResult(context.Document), equivalenceKey := equivalenceKey)
                		Dim codeAction2_3 = CodeAction.Create(equivalenceKey := equivalenceKey, title := "Title2_3", createChangedDocument := Function(x) Task.FromResult(context.Document))

                		' Conservative no diagnostic cases.
                		Dim nullKey As String = Nothing
                		Dim codeAction3_1 = CodeAction.Create("Title3_1", Function(x) Task.FromResult(context.Document), nullKey)
                		Dim codeAction3_2 = CodeAction.Create("Title3_1", Function(x) Task.FromResult(context.Document), GetKey())

                		context.RegisterCodeFix(codeAction1_1, context.Diagnostics)
                		context.RegisterCodeFix(codeAction1_2, context.Diagnostics)

                		context.RegisterCodeFix(codeAction2_1, context.Diagnostics)
                		context.RegisterCodeFix(codeAction2_2, context.Diagnostics)
                		context.RegisterCodeFix(codeAction2_3, context.Diagnostics)

                		context.RegisterCodeFix(codeAction3_1, context.Diagnostics)
                		context.RegisterCodeFix(codeAction3_2, context.Diagnostics)

                		Return Nothing
                	End Function

                	Private Function GetKey() As String
                		Return Nothing
                	End Function

                """, missingGetFixAllProviderOverrideDiagnostic);
        }

        [Fact]
        public async Task VisualBasic_CodeActionCreate_DiagnosticsOnExportedCodeFixProviderTypeAsync()
        {
            var expected = new[]
            {
                // Test0.vb(23,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(23, 23)
            };

            // Test0.vb(10,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetBasicOverrideGetFixAllProviderExpectedDiagnostic(10, 7, "C2");

            await TestBasicCoreAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports System.Threading.Tasks
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.CodeActions

                <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(C2))>
                Class C2
                    Inherits C1
                End Class

                MustInherit Class C1
                	Inherits CodeFixProvider
                	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                		Dim codeAction1_1 = CodeAction.Create("Title1_1", Function(x) Task.FromResult(context.Document))
                		Return Nothing
                	End Function

                	Private Function GetKey() As String
                		Return Nothing
                	End Function

                """, missingGetFixAllProviderOverrideDiagnostic, expected: expected);
        }

        [Fact]
        public async Task VisualBasic_CustomCodeAction_VerifyDiagnosticsAsync()
        {
            var expected = new[]
            {
                // Test0.vb(19,20): warning RS1011: 'MyCodeActionNoEquivalenceKey' has the default value of 'null' for property 'EquivalenceKey'. Either override this property on 'MyCodeActionNoEquivalenceKey' to return a non-null and unique value across all code actions per-fixer or use such an existing code action.
                GetBasicOverrideCodeActionEquivalenceKeyExpectedDiagnostic(19, 20, "MyCodeActionNoEquivalenceKey")
            };

            // Test0.vb(10,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetBasicOverrideGetFixAllProviderExpectedDiagnostic(10, 7, "C1");

            await TestBasicCoreAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports System.Threading.Tasks
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.CodeActions

                <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(C1))>
                Class C1
                	Inherits CodeFixProvider
                	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                		Dim codeAction = New MyCodeActionNoEquivalenceKey()
                		Return Nothing
                	End Function

                """, missingGetFixAllProviderOverrideDiagnostic, withCustomCodeActions: true, expected: expected);
        }

        [Fact]
        public async Task VisualBasic_CustomCodeAction_NoDiagnosticsAsync()
        {
            // Test0.vb(10,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetBasicOverrideGetFixAllProviderExpectedDiagnostic(10, 7, "C1");

            await TestBasicCoreAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports System.Threading.Tasks
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.CodeActions

                <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(C1))>
                Class C1
                	Inherits CodeFixProvider
                	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                		Dim codeAction As CodeAction = New MyCodeActionWithEquivalenceKey()
                		context.RegisterCodeFix(codeAction, context.Diagnostics)

                        codeAction = New MyDerivedCodeActionWithEquivalenceKey()
                		context.RegisterCodeFix(codeAction, context.Diagnostics)

                		Return Nothing
                	End Function

                	Private Function GetKey() As String
                		Return Nothing
                	End Function

                """, missingGetFixAllProviderOverrideDiagnostic, withCustomCodeActions: true);
        }

        #endregion

        private static DiagnosticResult GetCSharpOverrideCodeActionEquivalenceKeyExpectedDiagnostic(int line, int column, string customCodeActionName)
        {
#pragma warning disable RS0030 // Do not use banned APIs
            return VerifyCS.Diagnostic(FixerWithFixAllAnalyzer.OverrideCodeActionEquivalenceKeyRule).WithLocation(line, column).WithArguments(customCodeActionName, nameof(CodeAction.EquivalenceKey));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        private static DiagnosticResult GetBasicOverrideCodeActionEquivalenceKeyExpectedDiagnostic(int line, int column, string customCodeActionName)
        {
#pragma warning disable RS0030 // Do not use banned APIs
            return VerifyVB.Diagnostic(FixerWithFixAllAnalyzer.OverrideCodeActionEquivalenceKeyRule).WithLocation(line, column).WithArguments(customCodeActionName, nameof(CodeAction.EquivalenceKey));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        private static DiagnosticResult GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(int line, int column)
        {
#pragma warning disable RS0030 // Do not use banned APIs
            return VerifyCS.Diagnostic(FixerWithFixAllAnalyzer.CreateCodeActionEquivalenceKeyRule).WithLocation(line, column).WithArguments("equivalenceKey");
#pragma warning restore RS0030 // Do not use banned APIs
        }

        private static DiagnosticResult GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(int line, int column)
        {
#pragma warning disable RS0030 // Do not use banned APIs
            return VerifyVB.Diagnostic(FixerWithFixAllAnalyzer.CreateCodeActionEquivalenceKeyRule).WithLocation(line, column).WithArguments("equivalenceKey");
#pragma warning restore RS0030 // Do not use banned APIs
        }

        private static DiagnosticResult GetCSharpOverrideGetFixAllProviderExpectedDiagnostic(int line, int column, string codeFixProviderTypeName)
        {
#pragma warning disable RS0030 // Do not use banned APIs
            return VerifyCS.Diagnostic(FixerWithFixAllAnalyzer.OverrideGetFixAllProviderRule).WithLocation(line, column).WithArguments(codeFixProviderTypeName);
#pragma warning restore RS0030 // Do not use banned APIs
        }

        private static DiagnosticResult GetBasicOverrideGetFixAllProviderExpectedDiagnostic(int line, int column, string codeFixProviderTypeName)
        {
#pragma warning disable RS0030 // Do not use banned APIs
            return VerifyVB.Diagnostic(FixerWithFixAllAnalyzer.OverrideGetFixAllProviderRule).WithLocation(line, column).WithArguments(codeFixProviderTypeName);
#pragma warning restore RS0030 // Do not use banned APIs
        }
    }
}
