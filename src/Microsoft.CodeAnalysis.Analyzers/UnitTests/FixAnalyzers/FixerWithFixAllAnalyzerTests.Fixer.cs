// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Analyzers.FixAnalyzers;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.FixAnalyzers
{
    public class FixerWithFixAllFixerTests : CodeFixTestBase
    {
        #region CSharp tests

        [Fact]
        public void CSharp_VerifyFix_NonSealedType()
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
        var codeAction1_1 = CodeAction.Create(""Title1_1"", _ => Task.FromResult(context.Document));        
        return null;
    }

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }
}
";

            VerifyCSharpFix(source, fixedSource);
        }

        [Fact]
        public void CSharp_VerifyFix_SealedType()
        {
            var source = @"
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
        var codeAction1_1 = CodeAction.Create(""Title1_1"", _ => Task.FromResult(context.Document));        
        return null;
    }

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }
}
";

            VerifyCSharpFix(source, fixedSource);
        }

        [Fact]
        public void CSharp_NoDiagnostic()
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
        var codeAction1_1 = CodeAction.Create(""Title1_1"", _ => Task.FromResult(context.Document));        
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
        var codeAction1_1 = CodeAction.Create(""Title1_1"", _ => Task.FromResult(context.Document));        
        return null;
    }
}
";
            VerifyCSharpFix(source, source);
        }

        [Fact]
        public void CSharp_VerifyFixAll()
        {
            var source = @"
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
        var codeAction1_1 = CodeAction.Create(""Title1_1"", _ => Task.FromResult(context.Document));        
        return null;
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
        var codeAction1_1 = CodeAction.Create(""Title1_1"", _ => Task.FromResult(context.Document));        
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
        var codeAction1_1 = CodeAction.Create(""Title1_1"", _ => Task.FromResult(context.Document));        
        return null;
    }

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }
}
";

            VerifyCSharpFix(source, fixedSource);
        }

        #endregion

        #region VisualBasic tests

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/23410")]
        public void VisualBasic_VerifyFix_NonSealedType()
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
		Dim codeAction1_1 = CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))
		Return Nothing
	End Function

    Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
	    Return WellKnownFixAllProviders.BatchFixer
    End Function
End Class
";
            VerifyBasicFix(source, fixedSource);
        }

        [Fact]
        public void VisualBasic_VerifyFix_SealedType()
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
		Dim codeAction1_1 = CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))
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
		Dim codeAction1_1 = CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))
		Return Nothing
	End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
	    Return WellKnownFixAllProviders.BatchFixer
    End Function
End Class
";
            VerifyBasicFix(source, fixedSource);
        }

        [Fact]
        public void VisualBasic_NoDiagnostic()
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
		Dim codeAction1_1 = CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))
		Return Nothing
	End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
	    Return Nothing
    End Function
End Class
";
            VerifyBasicFix(source, source);
        }

        [Fact]
        public void VisualBasic_VerifyFixAll()
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
		Dim codeAction1_1 = CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))
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
		Dim codeAction1_1 = CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))
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
		Dim codeAction1_1 = CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))
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
		Dim codeAction1_1 = CodeAction.Create(""Title1_1"", Function(x) Task.FromResult(context.Document))
		Return Nothing
	End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
	    Return WellKnownFixAllProviders.BatchFixer
    End Function
End Class
";
            VerifyBasicFix(source, fixedSource);
        }

        #endregion

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new FixerWithFixAllFix();
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new FixerWithFixAllFix();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new FixerWithFixAllAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new FixerWithFixAllAnalyzer();
        }
    }
}
