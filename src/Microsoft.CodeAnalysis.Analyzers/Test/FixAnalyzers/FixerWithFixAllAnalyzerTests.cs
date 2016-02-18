// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Analyzers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Analyzers.FixAnalyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers.FixAnalyzers;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Analyzers.FixAnalyzers
{
    public class FixerWithFixAllAnalyzerTests : CodeFixTestBase
    {
        #region CSharp tests

        private static readonly string s_CSharpCustomCodeActions = @"
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
            return ""DummyEquivalenceKey"";
        }
    }
}
";
        private void TestCSharpCore(string source, bool withCustomCodeActions = false, params DiagnosticResult[] expected)
        {
            var fixAllProviderString = @"public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }";

            var sourceSuffix = @"
}";

            if (withCustomCodeActions)
            {
                sourceSuffix = sourceSuffix + s_CSharpCustomCodeActions;
            }

            // Verify expected diagnostics for fixer that supports FixAllProvider.
            VerifyCSharp(source + fixAllProviderString + sourceSuffix, expected);

            // Verify no diagnostics for fixer that does not support FixAllProvider.
            VerifyCSharp(source + sourceSuffix);
        }

        [Fact]
        public void CSharp_CodeActionCreate_VerifyDiagnostics()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;

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
        var codeAction1_1 = CodeAction.Create(""Title1_1"", _ => Task.FromResult(context.Document));
        var codeAction1_2 = CodeAction.Create(""Title1_2"", createChangedDocument: _ => Task.FromResult(context.Document));
        var codeAction1_3 = CodeAction.Create(createChangedDocument: _ => Task.FromResult(context.Document), title: ""Title1_3"");

        // Null argument for equivalenceKey.
        var codeAction2_1 = CodeAction.Create(""Title2_1"", _ => Task.FromResult(context.Document), null);
        var codeAction2_2 = CodeAction.Create(createChangedDocument: _ => Task.FromResult(context.Document), equivalenceKey: null, title: ""Title2_2"");
        var codeAction2_3 = CodeAction.Create(""Title2_3"", _ => Task.FromResult(context.Document), equivalenceKey: null);
        
        return null;
    }
";

            var expected = new DiagnosticResult[]
            {
                // Test0.cs(21,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCodeActionCreateExpectedDiagnostic(21, 29),
                // Test0.cs(22,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCodeActionCreateExpectedDiagnostic(22, 29),
                // Test0.cs(23,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCodeActionCreateExpectedDiagnostic(23, 29),
                // Test0.cs(26,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCodeActionCreateExpectedDiagnostic(26, 29),
                // Test0.cs(27,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCodeActionCreateExpectedDiagnostic(27, 29),
                // Test0.cs(28,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCodeActionCreateExpectedDiagnostic(28, 29)
            };

            TestCSharpCore(source, expected: expected);
        }

        [Fact]
        public void CSharp_CodeActionCreate_NoDiagnostics()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;

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
        var codeAction1_1 = CodeAction.Create(""Title1_1"");
        var codeAction1_2 = CodeAction.Create(createChangedDocument: _ => Task.FromResult(context.Document), equivalenceKey: null);

        // Correct non-null arguments
        var equivalenceKey = ""equivalenceKey"";
        var codeAction2_1 = CodeAction.Create(""Title2_1"", _ => Task.FromResult(context.Document), equivalenceKey);
        var codeAction2_2 = CodeAction.Create(title: ""Title2_2"", createChangedDocument: _ => Task.FromResult(context.Document), equivalenceKey: equivalenceKey);
        var codeAction2_3 = CodeAction.Create(equivalenceKey: equivalenceKey, title: ""Title2_3"", createChangedDocument: _ => Task.FromResult(context.Document));

        // Conservative no diagnostic cases.
        var nullKey = null;
        var codeAction3_1 = CodeAction.Create(""Title2_1"", _ => Task.FromResult(context.Document), nullKey);
        var codeAction3_2 = CodeAction.Create(""Title2_1"", _ => Task.FromResult(context.Document), GetKey());
        
        context.RegisterCodeFix(codeAction, context.Diagnostics);
        return null;
    }

    private string GetKey()
    {
        return null;
    }
";
            // Verify no diagnostics.
            TestCSharpCore(source);
        }

        [Fact]
        public void CSharp_CustomCodeAction_VerifyDiagnostics()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;

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
";

            var expected = new DiagnosticResult[]
            {
                // Test0.cs(20,26): warning RS1011: 'MyCodeActionNoEquivalenceKey' has the default value of 'null' for property 'EquivalenceKey'. Either override this property on 'MyCodeActionNoEquivalenceKey' to return a non-null and unique value across all code actions per-fixer or use such an existing code action.
                GetCSharpCustomCodeActionExpectedDiagnostic(20, 26, "MyCodeActionNoEquivalenceKey")
            };

            TestCSharpCore(source, withCustomCodeActions: true, expected: expected);
        }

        [Fact]
        public void CSharp_CustomCodeAction_NoDiagnostics()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;

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
        var codeAction = new MyCodeActionWithEquivalenceKey();        
        context.RegisterCodeFix(codeAction, context.Diagnostics);
        return null;
    }

    private string GetKey()
    {
        return null;
    }
";
            // Verify no diagnostics.
            TestCSharpCore(source, withCustomCodeActions: true);
        }

        #endregion

        #region VisualBasic tests

        private static readonly string s_VisualBasicCustomCodeActions = @"

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
			Return ""DummyEquivalenceKey""
		End Get
	End Property
End Class
";
        private void TestBasicCore(string source, bool withCustomCodeActions = false, params DiagnosticResult[] expected)
        {
            var fixAllProviderString = @"Public Overrides Function GetFixAllProvider() As FixAllProvider
	Return WellKnownFixAllProviders.BatchFixer
End Function
";

            var sourceSuffix = @"
End Class
";

            if (withCustomCodeActions)
            {
                sourceSuffix = sourceSuffix + s_VisualBasicCustomCodeActions;
            }

            // Verify expected diagnostics for fixer that supports FixAllProvider.
            VerifyBasic(source + fixAllProviderString + sourceSuffix, expected);

            // Verify no diagnostics for fixer that does not support FixAllProvider.
            VerifyBasic(source + sourceSuffix);
        }

        [Fact]
        public void VisualBasic_CodeActionCreate_VerifyDiagnostics()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeActions

Class C1
	Inherits CodeFixProvider
	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
		' Regular cases.
		Dim codeAction1_1 = CodeAction.Create(""Title1_1"", Function(_) Task.FromResult(context.Document))
		Dim codeAction1_2 = CodeAction.Create(""Title1_2"", createChangedDocument := Function(_) Task.FromResult(context.Document))
		Dim codeAction1_3 = CodeAction.Create(createChangedDocument := Function(_) Task.FromResult(context.Document), title := ""Title1_3"")

		' Null argument for equivalenceKey.
		Dim codeAction2_1 = CodeAction.Create(""Title2_1"", Function(_) Task.FromResult(context.Document), Nothing)
		Dim codeAction2_2 = CodeAction.Create(createChangedDocument := Function(_) Task.FromResult(context.Document), equivalenceKey := Nothing, title := ""Title2_2"")
		Dim codeAction2_3 = CodeAction.Create(""Title2_3"", Function(_) Task.FromResult(context.Document), equivalenceKey := Nothing)

		Return Nothing
	End Function
";

            var expected = new DiagnosticResult[]
            {
                // Test0.vb(18,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCodeActionCreateExpectedDiagnostic(18, 23),
                // Test0.vb(19,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCodeActionCreateExpectedDiagnostic(19, 23),
                // Test0.vb(20,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCodeActionCreateExpectedDiagnostic(20, 23),
                // Test0.vb(23,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCodeActionCreateExpectedDiagnostic(23, 23),
                // Test0.vb(24,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCodeActionCreateExpectedDiagnostic(24, 23),
                // Test0.vb(25,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCodeActionCreateExpectedDiagnostic(25, 23)
            };

            TestBasicCore(source, expected: expected);
        }

        [Fact]
        public void VisualBasic_CodeActionCreate_NoDiagnostics()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeActions

Class C1
	Inherits CodeFixProvider
	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
		' Overload resolution failure cases.
		Dim codeAction1_1 = CodeAction.Create(""Title1_1"")
		Dim codeAction1_2 = CodeAction.Create(createChangedDocument := Function(_) Task.FromResult(context.Document), equivalenceKey := Nothing)

		' Correct non-null arguments
		Dim equivalenceKey = ""equivalenceKey""
		Dim codeAction2_1 = CodeAction.Create(""Title2_1"", Function(_) Task.FromResult(context.Document), equivalenceKey)
		Dim codeAction2_2 = CodeAction.Create(title := ""Title2_2"", createChangedDocument := Function(_) Task.FromResult(context.Document), equivalenceKey := equivalenceKey)
		Dim codeAction2_3 = CodeAction.Create(equivalenceKey := equivalenceKey, title := ""Title2_3"", createChangedDocument := Function(_) Task.FromResult(context.Document))

		' Conservative no diagnostic cases.
		Dim nullKey = Nothing
		Dim codeAction3_1 = CodeAction.Create(""Title2_1"", Function(_) Task.FromResult(context.Document), nullKey)
		Dim codeAction3_2 = CodeAction.Create(""Title2_1"", Function(_) Task.FromResult(context.Document), GetKey())

		context.RegisterCodeFix(codeAction, context.Diagnostics)
		Return Nothing
	End Function

	Private Function GetKey() As String
		Return Nothing
	End Function
";
            // Verify no diagnostics.
            TestBasicCore(source);
        }

        [Fact]
        public void VisualBasic_CustomCodeAction_VerifyDiagnostics()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeActions

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
";

            var expected = new DiagnosticResult[]
            {
                // Test0.vb(17,20): warning RS1011: 'MyCodeActionNoEquivalenceKey' has the default value of 'null' for property 'EquivalenceKey'. Either override this property on 'MyCodeActionNoEquivalenceKey' to return a non-null and unique value across all code actions per-fixer or use such an existing code action.
                GetBasicCustomCodeActionExpectedDiagnostic(17, 20, "MyCodeActionNoEquivalenceKey")
            };

            TestBasicCore(source, withCustomCodeActions: true, expected: expected);
        }

        [Fact]
        public void VisualBasic_CustomCodeAction_NoDiagnostics()
        {
            var source = @"
using System;
Imports System
Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeActions

Class C1
	Inherits CodeFixProvider
	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
		Dim codeAction = New MyCodeActionWithEquivalenceKey()
		context.RegisterCodeFix(codeAction, context.Diagnostics)
		Return Nothing
	End Function

	Private Function GetKey() As String
		Return Nothing
	End Function
";
            // Verify no diagnostics.
            TestBasicCore(source, withCustomCodeActions: true);
        }

        #endregion

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
            return new CSharpFixerWithFixAllAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicFixerWithFixAllAnalyzer();
        }

        private static DiagnosticResult GetCSharpCustomCodeActionExpectedDiagnostic(int line, int column, string customCodeActionName)
        {
            string message = string.Format(CodeAnalysisDiagnosticsResources.OverrideCodeActionEquivalenceKeyMessage, customCodeActionName, "EquivalenceKey");
            return GetExpectedDiagnostic(LanguageNames.CSharp, line, column, CSharpFixerWithFixAllAnalyzer.OverrideCodeActionEquivalenceKeyRule.Id, message);
        }

        private static DiagnosticResult GetBasicCustomCodeActionExpectedDiagnostic(int line, int column, string customCodeActionName)
        {
            string message = string.Format(CodeAnalysisDiagnosticsResources.OverrideCodeActionEquivalenceKeyMessage, customCodeActionName, "EquivalenceKey");
            return GetExpectedDiagnostic(LanguageNames.VisualBasic, line, column, BasicFixerWithFixAllAnalyzer.OverrideCodeActionEquivalenceKeyRule.Id, message);
        }

        private static DiagnosticResult GetCSharpCodeActionCreateExpectedDiagnostic(int line, int column)
        {
            string message = string.Format(CodeAnalysisDiagnosticsResources.CreateCodeActionWithEquivalenceKeyMessage, "equivalenceKey");
            return GetExpectedDiagnostic(LanguageNames.CSharp, line, column, CSharpFixerWithFixAllAnalyzer.CreateCodeActionEquivalenceKeyRule.Id, message);
        }

        private static DiagnosticResult GetBasicCodeActionCreateExpectedDiagnostic(int line, int column)
        {
            string message = string.Format(CodeAnalysisDiagnosticsResources.CreateCodeActionWithEquivalenceKeyMessage, "equivalenceKey");
            return GetExpectedDiagnostic(LanguageNames.VisualBasic, line, column, BasicFixerWithFixAllAnalyzer.CreateCodeActionEquivalenceKeyRule.Id, message);
        }

        private static DiagnosticResult GetExpectedDiagnostic(string language, int line, int column, string id, string message)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult
            {
                Id = id,
                Message = message,
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation(fileName, line, column)
                }
            };
        }
    }
}
