// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public Task CSharp_VerifyFix_NonSealedTypeAsync()
            => VerifyCS.VerifyCodeFixAsync("""
                using System;
                using System.Collections.Immutable;
                using System.Threading.Tasks;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CodeActions;

                [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C1))]
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
                        var codeAction1_1 = CodeAction.Create("Title1_1", _ => Task.FromResult(context.Document));        
                        return null;
                    }
                }
                """, """
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
                        var codeAction1_1 = {|RS1010:CodeAction.Create("Title1_1", _ => Task.FromResult(context.Document))|};        
                        return null;
                    }

                    public sealed override FixAllProvider GetFixAllProvider()
                    {
                        return WellKnownFixAllProviders.BatchFixer;
                    }
                }
                """);

        [Fact]
        public Task CSharp_VerifyFix_SealedTypeAsync()
            => VerifyCS.VerifyCodeFixAsync("""
                using System;
                using System.Collections.Immutable;
                using System.Threading.Tasks;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CodeActions;

                [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C1))]
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
                        var codeAction1_1 = CodeAction.Create("Title1_1", _ => Task.FromResult(context.Document));        
                        return null;
                    }
                }
                """, """
                using System;
                using System.Collections.Immutable;
                using System.Threading.Tasks;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CodeActions;

                [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C1))]
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
                        var codeAction1_1 = {|RS1010:CodeAction.Create("Title1_1", _ => Task.FromResult(context.Document))|};        
                        return null;
                    }

                    public override FixAllProvider GetFixAllProvider()
                    {
                        return WellKnownFixAllProviders.BatchFixer;
                    }
                }
                """);

        [Fact]
        public async Task CSharp_NoDiagnosticAsync()
        {
            var source = """
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
                        return WellKnownFixAllProviders.BatchFixer;
                    }

                    public override Task RegisterCodeFixesAsync(CodeFixContext context)
                    {
                        var codeAction1_1 = {|RS1010:CodeAction.Create("Title1_1", _ => Task.FromResult(context.Document))|};        
                        return null;
                    }
                }

                [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C2))]
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
                        var codeAction1_1 = {|RS1010:CodeAction.Create("Title1_1", _ => Task.FromResult(context.Document))|};        
                        return null;
                    }
                }
                """;
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public Task CSharp_VerifyFixAllAsync()
            => VerifyCS.VerifyCodeFixAsync("""
                using System;
                using System.Collections.Immutable;
                using System.Threading.Tasks;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CodeActions;

                [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C1))]
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
                        var codeAction1_1 = CodeAction.Create("Title1_1", _ => Task.FromResult(context.Document));        
                        return null;
                    }
                }

                [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C2))]
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
                        var codeAction1_1 = CodeAction.Create("Title1_1", _ => Task.FromResult(context.Document));        
                        return null;
                    }
                }
                """, """
                using System;
                using System.Collections.Immutable;
                using System.Threading.Tasks;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CodeFixes;
                using Microsoft.CodeAnalysis.CodeActions;

                [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C1))]
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
                        var codeAction1_1 = {|RS1010:CodeAction.Create("Title1_1", _ => Task.FromResult(context.Document))|};        
                        return null;
                    }

                    public override FixAllProvider GetFixAllProvider()
                    {
                        return WellKnownFixAllProviders.BatchFixer;
                    }
                }

                [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C2))]
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
                        var codeAction1_1 = {|RS1010:CodeAction.Create("Title1_1", _ => Task.FromResult(context.Document))|};        
                        return null;
                    }

                    public override FixAllProvider GetFixAllProvider()
                    {
                        return WellKnownFixAllProviders.BatchFixer;
                    }
                }
                """);

        #endregion

        #region VisualBasic tests

        [Fact()]
        public Task VisualBasic_VerifyFix_NonSealedTypeAsync()
            => VerifyVB.VerifyCodeFixAsync("""
                Imports System
                Imports System.Collections.Immutable
                Imports System.Threading.Tasks
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.CodeActions

                <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(C1))>
                Class {|RS1016:C1|}
                	Inherits CodeFixProvider
                	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                		' Regular cases.
                		Dim codeAction1_1 = CodeAction.Create("Title1_1", Function(x) Task.FromResult(context.Document))
                		Return Nothing
                	End Function
                End Class
                """, """
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
                		Dim codeAction1_1 = {|RS1010:CodeAction.Create("Title1_1", Function(x) Task.FromResult(context.Document))|}
                		Return Nothing
                	End Function

                    Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
                        Return WellKnownFixAllProviders.BatchFixer
                    End Function
                End Class
                """);

        [Fact]
        public Task VisualBasic_VerifyFix_SealedTypeAsync()
            => VerifyVB.VerifyCodeFixAsync("""
                Imports System
                Imports System.Collections.Immutable
                Imports System.Threading.Tasks
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.CodeActions

                <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(C1))>
                NotInheritable Class {|RS1016:C1|}
                	Inherits CodeFixProvider
                	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                		' Regular cases.
                		Dim codeAction1_1 = CodeAction.Create("Title1_1", Function(x) Task.FromResult(context.Document))
                		Return Nothing
                	End Function
                End Class
                """, """
                Imports System
                Imports System.Collections.Immutable
                Imports System.Threading.Tasks
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.CodeActions

                <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(C1))>
                NotInheritable Class C1
                	Inherits CodeFixProvider
                	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                		' Regular cases.
                		Dim codeAction1_1 = {|RS1010:CodeAction.Create("Title1_1", Function(x) Task.FromResult(context.Document))|}
                		Return Nothing
                	End Function

                    Public Overrides Function GetFixAllProvider() As FixAllProvider
                        Return WellKnownFixAllProviders.BatchFixer
                    End Function
                End Class
                """);

        [Fact]
        public async Task VisualBasic_NoDiagnosticAsync()
        {
            var source = """
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
                		Dim codeAction1_1 = {|RS1010:CodeAction.Create("Title1_1", Function(x) Task.FromResult(context.Document))|}
                		Return Nothing
                	End Function

                    Public Overrides Function GetFixAllProvider() As FixAllProvider
                	    Return WellKnownFixAllProviders.BatchFixer
                    End Function
                End Class

                <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(C2))>
                Class C2
                	Inherits CodeFixProvider
                	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                		' Regular cases.
                		Dim codeAction1_1 = {|RS1010:CodeAction.Create("Title1_1", Function(x) Task.FromResult(context.Document))|}
                		Return Nothing
                	End Function

                    Public Overrides Function GetFixAllProvider() As FixAllProvider
                	    Return Nothing
                    End Function
                End Class
                """;
            await VerifyVB.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public Task VisualBasic_VerifyFixAllAsync()
            => VerifyVB.VerifyCodeFixAsync("""
                Imports System
                Imports System.Collections.Immutable
                Imports System.Threading.Tasks
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.CodeActions

                <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(C1))>
                NotInheritable Class {|RS1016:C1|}
                	Inherits CodeFixProvider
                	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                		' Regular cases.
                		Dim codeAction1_1 = CodeAction.Create("Title1_1", Function(x) Task.FromResult(context.Document))
                		Return Nothing
                	End Function
                End Class

                <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(C2))>
                NotInheritable Class {|RS1016:C2|}
                	Inherits CodeFixProvider
                	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                		' Regular cases.
                		Dim codeAction1_1 = CodeAction.Create("Title1_1", Function(x) Task.FromResult(context.Document))
                		Return Nothing
                	End Function
                End Class
                """, """
                Imports System
                Imports System.Collections.Immutable
                Imports System.Threading.Tasks
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.CodeFixes
                Imports Microsoft.CodeAnalysis.CodeActions

                <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(C1))>
                NotInheritable Class C1
                	Inherits CodeFixProvider
                	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                		' Regular cases.
                		Dim codeAction1_1 = {|RS1010:CodeAction.Create("Title1_1", Function(x) Task.FromResult(context.Document))|}
                		Return Nothing
                	End Function

                    Public Overrides Function GetFixAllProvider() As FixAllProvider
                        Return WellKnownFixAllProviders.BatchFixer
                    End Function
                End Class

                <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(C2))>
                NotInheritable Class C2
                	Inherits CodeFixProvider
                	Public Overrides ReadOnly Property FixableDiagnosticIds() As ImmutableArray(Of String)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                		' Regular cases.
                		Dim codeAction1_1 = {|RS1010:CodeAction.Create("Title1_1", Function(x) Task.FromResult(context.Document))|}
                		Return Nothing
                	End Function

                    Public Overrides Function GetFixAllProvider() As FixAllProvider
                        Return WellKnownFixAllProviders.BatchFixer
                    End Function
                End Class
                """);

        #endregion
    }
}
