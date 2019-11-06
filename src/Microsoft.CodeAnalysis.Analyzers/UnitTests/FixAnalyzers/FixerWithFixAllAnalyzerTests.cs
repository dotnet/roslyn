// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Analyzers.FixAnalyzers;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Testing;
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

        private const string CSharpCustomCodeActions = @"
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
            return ""DummyEquivalenceKey"";
        }
    }
}

public class MyDerivedCodeActionWithEquivalenceKey : MyAbstractCodeActionWithEquivalenceKey
{
}
";
        private async Task TestCSharpCoreAsync(string source, DiagnosticResult missingGetFixAllProviderOverrideDiagnostic,
            bool withCustomCodeActions = false, params DiagnosticResult[] expected)
        {
            var fixAllProviderString = @"public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }";

            var sourceSuffix = @"
}";

            if (withCustomCodeActions)
            {
                sourceSuffix += CSharpCustomCodeActions;
            }

            // Verify expected diagnostics for fixer that supports FixAllProvider.
            await VerifyCS.VerifyAnalyzerAsync(source + fixAllProviderString + sourceSuffix, expected);

            // Verify RS1016 (OverrideGetFixAllProviderRule) diagnostic for fixer that does not support FixAllProvider.
            var allExpected = new[] { missingGetFixAllProviderOverrideDiagnostic }
                .Concat(expected.Where(x => x.Severity == DiagnosticSeverity.Error))
                .ToArray();
            await VerifyCS.VerifyAnalyzerAsync(source + sourceSuffix, allExpected);
        }

        [Fact]
        public async Task CSharp_CodeActionCreate_VerifyDiagnostics()
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

            var expected = new[]
            {
                // Test0.cs(21,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(21, 29),
                // Test0.cs(22,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(22, 29),
                // Test0.cs(23,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(23, 29),
                // Test0.cs(26,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(26, 29),
                // Test0.cs(27,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(27, 29),
                // Test0.cs(28,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(28, 29)
            };

            // Test0.cs(8,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetCSharpOverrideGetFixAllProviderExpectedDiagnostic(8, 7, "C1");
            await TestCSharpCoreAsync(source, missingGetFixAllProviderOverrideDiagnostic, expected: expected);
        }

        [Fact]
        public async Task CSharp_CodeActionCreate_NoDiagnostics()
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
        string nullKey = null;
        var codeAction3_1 = CodeAction.Create(""Title3_1"", _ => Task.FromResult(context.Document), nullKey);
        var codeAction3_2 = CodeAction.Create(""Title3_1"", _ => Task.FromResult(context.Document), GetKey());

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
";

            await TestCSharpCoreAsync(source,
                // Test0.cs(8,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
                GetCSharpOverrideGetFixAllProviderExpectedDiagnostic(8, 7, "C1"),
                false,
                // Test0.cs(21,40): error CS1501: No overload for method 'Create' takes 1 arguments
                DiagnosticResult.CompilerError("CS1501").WithLocation(21, 40),
                // Test0.cs(22,40): error CS7036: There is no argument given that corresponds to the required formal parameter 'title' of 'CodeAction.Create(string, Func<CancellationToken, Task<Document>>, string)'
                DiagnosticResult.CompilerError("CS7036").WithLocation(22, 40));
        }

        [Fact]
        public async Task CSharp_CodeActionCreate_NoDiagnosticsOnSubType()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;

class C2 : C1
{
}

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
        var equivalenceKey = ""equivalenceKey"";
        var codeAction2_1 = CodeAction.Create(""Title2_1"", _ => Task.FromResult(context.Document), equivalenceKey);
        return null;
    }
";

            // Test0.cs(12,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetCSharpOverrideGetFixAllProviderExpectedDiagnostic(12, 7, "C1");

            await TestCSharpCoreAsync(source, missingGetFixAllProviderOverrideDiagnostic);
        }

        [Fact]
        public async Task CSharp_CodeActionCreate_DiagnosticsOnAbstractType()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;

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
        var codeAction1_1 = CodeAction.Create(""Title1_1"", _ => Task.FromResult(context.Document));
        return null;
    }
";
            var expected = new DiagnosticResult[]
            {
                // Test0.cs(24,29): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(24, 29)
            };

            // Test0.cs(12,16): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetCSharpOverrideGetFixAllProviderExpectedDiagnostic(12, 16, "C1");

            await TestCSharpCoreAsync(source, missingGetFixAllProviderOverrideDiagnostic, expected: expected);
        }

        [Fact]
        public async Task CSharp_CustomCodeAction_VerifyDiagnostics()
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

            var expected = new[]
            {
                // Test0.cs(20,26): warning RS1011: 'MyCodeActionNoEquivalenceKey' has the default value of 'null' for property 'EquivalenceKey'. Either override this property on 'MyCodeActionNoEquivalenceKey' to return a non-null and unique value across all code actions per-fixer or use such an existing code action.
                GetCSharpOverrideCodeActionEquivalenceKeyExpectedDiagnostic(20, 26, "MyCodeActionNoEquivalenceKey")
            };

            // Test0.cs(8,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetCSharpOverrideGetFixAllProviderExpectedDiagnostic(8, 7, "C1");

            await TestCSharpCoreAsync(source, missingGetFixAllProviderOverrideDiagnostic, withCustomCodeActions: true, expected: expected);
        }

        [Fact]
        public async Task CSharp_CustomCodeAction_NoDiagnostics()
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
";
            // Test0.cs(8,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetCSharpOverrideGetFixAllProviderExpectedDiagnostic(8, 7, "C1");

            await TestCSharpCoreAsync(source, missingGetFixAllProviderOverrideDiagnostic, withCustomCodeActions: true);
        }

        #endregion

        #region VisualBasic tests

        private const string VisualBasicCustomCodeActions = @"

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

Public MustInherit Class MyAbstractCodeActionWithEquivalenceKey
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

Public Class MyDerivedCodeActionWithEquivalenceKey
	Inherits MyAbstractCodeActionWithEquivalenceKey
End Class
";
        private async Task TestBasicCore(string source, DiagnosticResult missingGetFixAllProviderOverrideDiagnostic,
            bool withCustomCodeActions = false, params DiagnosticResult[] expected)
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
                sourceSuffix += VisualBasicCustomCodeActions;
            }

            // Verify expected diagnostics for fixer that supports FixAllProvider.
            await VerifyVB.VerifyAnalyzerAsync(source + fixAllProviderString + sourceSuffix, expected);

            // Verify RS1016 (OverrideGetFixAllProviderRule) diagnostic for fixer that does not support FixAllProvider.
            var allExpected = new[] { missingGetFixAllProviderOverrideDiagnostic }
                .Concat(expected.Where(x => x.Severity == DiagnosticSeverity.Error))
                .ToArray();
            await VerifyVB.VerifyAnalyzerAsync(source + sourceSuffix, allExpected);
        }

        [Fact]
        public async Task VisualBasic_CodeActionCreate_VerifyDiagnostics()
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
		Dim codeAction1_1 = CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))
		Dim codeAction1_2 = CodeAction.Create(""Title1_2"", createChangedDocument := Function(x) Task.FromResult(context.Document))
		Dim codeAction1_3 = CodeAction.Create(createChangedDocument := Function(x) Task.FromResult(context.Document), title := ""Title1_3"")

		' Null argument for equivalenceKey.
		Dim codeAction2_1 = CodeAction.Create(""Title2_1"", Function(x) Task.FromResult(context.Document), Nothing)
		Dim codeAction2_2 = CodeAction.Create(createChangedDocument := Function(x) Task.FromResult(context.Document), equivalenceKey := Nothing, title := ""Title2_2"")
		Dim codeAction2_3 = CodeAction.Create(""Title2_3"", Function(x) Task.FromResult(context.Document), equivalenceKey := Nothing)

		Return Nothing
	End Function
";

            var expected = new[]
            {
                // Test0.vb(18,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(18, 23),
                // Test0.vb(19,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(19, 23),
                // Test0.vb(20,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(20, 23),
                // Test0.vb(23,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(23, 23),
                // Test0.vb(24,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(24, 23),
                // Test0.vb(25,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(25, 23)
            };

            // Test0.vb(8,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetBasicOverrideGetFixAllProviderExpectedDiagnostic(8, 7, "C1");

            await TestBasicCore(source, missingGetFixAllProviderOverrideDiagnostic, expected: expected);
        }

        [Fact]
        public async Task VisualBasic_CodeActionCreate_NoDiagnostics()
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
		Dim codeAction1_2 = CodeAction.Create(createChangedDocument := Function(x) Task.FromResult(context.Document), equivalenceKey := Nothing)

		' Correct non-null arguments
		Dim equivalenceKey = ""equivalenceKey""
		Dim codeAction2_1 = CodeAction.Create(""Title2_1"", Function(x) Task.FromResult(context.Document), equivalenceKey)
		Dim codeAction2_2 = CodeAction.Create(title := ""Title2_2"", createChangedDocument := Function(x) Task.FromResult(context.Document), equivalenceKey := equivalenceKey)
		Dim codeAction2_3 = CodeAction.Create(equivalenceKey := equivalenceKey, title := ""Title2_3"", createChangedDocument := Function(x) Task.FromResult(context.Document))

		' Conservative no diagnostic cases.
		Dim nullKey As String = Nothing
		Dim codeAction3_1 = CodeAction.Create(""Title3_1"", Function(x) Task.FromResult(context.Document), nullKey)
		Dim codeAction3_2 = CodeAction.Create(""Title3_1"", Function(x) Task.FromResult(context.Document), GetKey())

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
";

            await TestBasicCore(source,
                // Test0.vb(8,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
                GetBasicOverrideGetFixAllProviderExpectedDiagnostic(8, 7, "C1"),
                false,
                // Test0.vb(18) : error BC30516: Overload resolution failed because no accessible 'Create' accepts this number of arguments.
                DiagnosticResult.CompilerError("BC30516").WithLocation(18, 34),
                // Test0.vb(19) : error BC30518: Overload resolution failed because no accessible 'Create' can be called with these arguments:
                DiagnosticResult.CompilerError("BC30518").WithLocation(19, 34));
        }

        [Fact]
        public async Task VisualBasic_CodeActionCreate_NoDiagnosticsOnSubType()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeActions

Class C2
    Inherits C1
End Class

Class C1
	Inherits CodeFixProvider
	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
		Dim equivalenceKey = ""equivalenceKey""
		Dim codeAction2_1 = CodeAction.Create(""Title2_1"", Function(x) Task.FromResult(context.Document), equivalenceKey)
		Return Nothing
	End Function

	Private Function GetKey() As String
		Return Nothing
	End Function
";
            // Test0.vb(12,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetBasicOverrideGetFixAllProviderExpectedDiagnostic(12, 7, "C1");

            await TestBasicCore(source, missingGetFixAllProviderOverrideDiagnostic);
        }

        [Fact]
        public async Task VisualBasic_CodeActionCreate_DiagnosticsOnAbstractType()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeActions

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
		Dim codeAction1_1 = CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))
		Return Nothing
	End Function

	Private Function GetKey() As String
		Return Nothing
	End Function
";
            var expected = new DiagnosticResult[]
            {
                // Test0.vb(21,23): warning RS1010: Provide an explicit argument for optional parameter 'equivalenceKey', which is non-null and unique across all code actions created by this fixer.
                GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(21, 23)
            };

            // Test0.vb(12,19): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetBasicOverrideGetFixAllProviderExpectedDiagnostic(12, 19, "C1");

            await TestBasicCore(source, missingGetFixAllProviderOverrideDiagnostic, expected: expected);
        }

        [Fact]
        public async Task VisualBasic_CustomCodeAction_VerifyDiagnostics()
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

            var expected = new[]
            {
                // Test0.vb(17,20): warning RS1011: 'MyCodeActionNoEquivalenceKey' has the default value of 'null' for property 'EquivalenceKey'. Either override this property on 'MyCodeActionNoEquivalenceKey' to return a non-null and unique value across all code actions per-fixer or use such an existing code action.
                GetBasicOverrideCodeActionEquivalenceKeyExpectedDiagnostic(17, 20, "MyCodeActionNoEquivalenceKey")
            };

            // Test0.vb(8,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetBasicOverrideGetFixAllProviderExpectedDiagnostic(8, 7, "C1");

            await TestBasicCore(source, missingGetFixAllProviderOverrideDiagnostic, withCustomCodeActions: true, expected: expected);
        }

        [Fact]
        public async Task VisualBasic_CustomCodeAction_NoDiagnostics()
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
		Dim codeAction As CodeAction = New MyCodeActionWithEquivalenceKey()
		context.RegisterCodeFix(codeAction, context.Diagnostics)

        codeAction = New MyDerivedCodeActionWithEquivalenceKey()
		context.RegisterCodeFix(codeAction, context.Diagnostics)

		Return Nothing
	End Function

	Private Function GetKey() As String
		Return Nothing
	End Function
";
            // Test0.vb(8,7): warning RS1016: 'C1' registers one or more code fixes, but does not override the method 'CodeFixProvider.GetFixAllProvider'. Override this method and provide a non-null FixAllProvider for FixAll support, potentially 'WellKnownFixAllProviders.BatchFixer', or 'null' to explicitly disable FixAll support.
            var missingGetFixAllProviderOverrideDiagnostic = GetBasicOverrideGetFixAllProviderExpectedDiagnostic(8, 7, "C1");

            await TestBasicCore(source, missingGetFixAllProviderOverrideDiagnostic, withCustomCodeActions: true);
        }

        #endregion

        private static DiagnosticResult GetCSharpOverrideCodeActionEquivalenceKeyExpectedDiagnostic(int line, int column, string customCodeActionName)
        {
            return VerifyCS.Diagnostic(FixerWithFixAllAnalyzer.OverrideCodeActionEquivalenceKeyRule).WithLocation(line, column).WithArguments(customCodeActionName, nameof(CodeAction.EquivalenceKey));
        }

        private static DiagnosticResult GetBasicOverrideCodeActionEquivalenceKeyExpectedDiagnostic(int line, int column, string customCodeActionName)
        {
            return VerifyVB.Diagnostic(FixerWithFixAllAnalyzer.OverrideCodeActionEquivalenceKeyRule).WithLocation(line, column).WithArguments(customCodeActionName, nameof(CodeAction.EquivalenceKey));
        }

        private static DiagnosticResult GetCSharpCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(int line, int column)
        {
            return VerifyCS.Diagnostic(FixerWithFixAllAnalyzer.CreateCodeActionEquivalenceKeyRule).WithLocation(line, column).WithArguments("equivalenceKey");
        }

        private static DiagnosticResult GetBasicCreateCodeActionWithEquivalenceKeyExpectedDiagnostic(int line, int column)
        {
            return VerifyVB.Diagnostic(FixerWithFixAllAnalyzer.CreateCodeActionEquivalenceKeyRule).WithLocation(line, column).WithArguments("equivalenceKey");
        }

        private static DiagnosticResult GetCSharpOverrideGetFixAllProviderExpectedDiagnostic(int line, int column, string codeFixProviderTypeName)
        {
            return VerifyCS.Diagnostic(FixerWithFixAllAnalyzer.OverrideGetFixAllProviderRule).WithLocation(line, column).WithArguments(codeFixProviderTypeName);
        }

        private static DiagnosticResult GetBasicOverrideGetFixAllProviderExpectedDiagnostic(int line, int column, string codeFixProviderTypeName)
        {
            return VerifyVB.Diagnostic(FixerWithFixAllAnalyzer.OverrideGetFixAllProviderRule).WithLocation(line, column).WithArguments(codeFixProviderTypeName);
        }
    }
}
