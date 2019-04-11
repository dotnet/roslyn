// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.FixAnalyzers.FixerWithFixAllAnalyzer,
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers.FixerWithFixAllFix>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.FixAnalyzers.FixerWithFixAllAnalyzer,
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers.FixerWithFixAllFix>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.FixAnalyzers
{
    public class FixerWithFixAllFixerTests
    {
        #region CSharp tests

        [Fact]
        public async Task CSharp_VerifyFix_NonSealedType()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;

class {|RS1016:C1|} : CodeFixProvider
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
}
";
            var fixedSource = @"
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
        var codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", _ => Task.FromResult(context.Document))|};        
        return null;
    }

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task CSharp_VerifyFix_SealedType()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;

sealed class {|RS1016:C1|} : CodeFixProvider
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
}
";
            var fixedSource = @"
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;

sealed class C1 : CodeFixProvider
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
        var codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", _ => Task.FromResult(context.Document))|};        
        return null;
    }

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task CSharp_NoDiagnostic()
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

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", _ => Task.FromResult(context.Document))|};        
        return null;
    }
}

class C2 : CodeFixProvider
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
        return null;
    }

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", _ => Task.FromResult(context.Document))|};        
        return null;
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task CSharp_VerifyFixAll()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;

sealed class {|RS1016:C1|} : CodeFixProvider
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
}

sealed class {|RS1016:C2|} : CodeFixProvider
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
}
";
            var fixedSource = @"
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;

sealed class C1 : CodeFixProvider
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
        var codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", _ => Task.FromResult(context.Document))|};        
        return null;
    }

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }
}

sealed class C2 : CodeFixProvider
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
        var codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", _ => Task.FromResult(context.Document))|};        
        return null;
    }

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        #endregion

        #region VisualBasic tests

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/23410")]
        public async Task VisualBasic_VerifyFix_NonSealedType()
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
		Dim codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))|}
		Return Nothing
	End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
	    Return WellKnownFixAllProviders.BatchFixer
    End Function
End Class
";
            var fixedSource = @"
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
		Dim codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))|}
		Return Nothing
	End Function

    Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
	    Return WellKnownFixAllProviders.BatchFixer
    End Function
End Class
";
            await VerifyVB.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task VisualBasic_VerifyFix_SealedType()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeActions

NotInheritable Class C1
	Inherits CodeFixProvider
	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
		' Regular cases.
		Dim codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))|}
		Return Nothing
	End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
	    Return WellKnownFixAllProviders.BatchFixer
    End Function
End Class
";
            var fixedSource = @"
Imports System
Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeActions

NotInheritable Class C1
	Inherits CodeFixProvider
	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
		' Regular cases.
		Dim codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))|}
		Return Nothing
	End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
	    Return WellKnownFixAllProviders.BatchFixer
    End Function
End Class
";
            await VerifyVB.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task VisualBasic_NoDiagnostic()
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
		Dim codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))|}
		Return Nothing
	End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
	    Return WellKnownFixAllProviders.BatchFixer
    End Function
End Class

Class C2
	Inherits CodeFixProvider
	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
		' Regular cases.
		Dim codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))|}
		Return Nothing
	End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
	    Return Nothing
    End Function
End Class
";
            await VerifyVB.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task VisualBasic_VerifyFixAll()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeActions

NotInheritable Class C1
	Inherits CodeFixProvider
	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
		' Regular cases.
		Dim codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))|}
		Return Nothing
	End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
	    Return WellKnownFixAllProviders.BatchFixer
    End Function
End Class

NotInheritable Class C2
	Inherits CodeFixProvider
	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
		' Regular cases.
		Dim codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))|}
		Return Nothing
	End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
	    Return WellKnownFixAllProviders.BatchFixer
    End Function
End Class
";
            var fixedSource = @"
Imports System
Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeActions

NotInheritable Class C1
	Inherits CodeFixProvider
	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
		' Regular cases.
		Dim codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))|}
		Return Nothing
	End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
	    Return WellKnownFixAllProviders.BatchFixer
    End Function
End Class

NotInheritable Class C2
	Inherits CodeFixProvider
	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
		' Regular cases.
		Dim codeAction1_1 = {|RS1010:CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))|}
		Return Nothing
	End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
	    Return WellKnownFixAllProviders.BatchFixer
    End Function
End Class
";

            await VerifyVB.VerifyCodeFixAsync(source, fixedSource);
        }

        #endregion
    }
}
