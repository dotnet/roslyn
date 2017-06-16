' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.UseDefaultOptionalParameter

    <DiagnosticAnalyzer(LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class UseDefaultOptionalParameter_Analyser
        Inherits DiagnosticAnalyzer

        Public Sub New()
            MyBase.New()
        End Sub

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)
            context.RegisterSyntaxNodeAction(AddressOf Analyse, S_Kinds)
        End Sub

        Friend Const ID As String = "BC00000" '= VBFeaturesResources.DefaultOptionalParameter

        Private Shared ReadOnly S_DiagnosticDescriptor As New DiagnosticDescriptor(ID,
                                                      VBFeaturesResources.DefaultOptionalParameter,
                                                      VBFeaturesResources.DefaultOptionalParameter,
                                                      "Style",
                                                      DiagnosticSeverity.Info, True)

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(S_DiagnosticDescriptor)
            End Get
        End Property

        Private ReadOnly S_Kinds As SyntaxKind() = {SyntaxKind.Parameter}

        Private Sub Analyse(context As SyntaxNodeAnalysisContext)
            Dim Parameter = TryCast(context.Node, ParameterSyntax)
            If Parameter?.Default Is Nothing AndAlso Parameter?.Default.Value IsNot Nothing Then Return
            Dim sym = TryCast(context.ContainingSymbol, IParameterSymbol)
            If Not sym?.IsOptional Then Return
            Dim [Default] = Parameter.Default.Value
            If [Default].IsKind(SyntaxKind.NothingLiteralExpression, SyntaxKind.FalseLiteralExpression) OrElse
                [Default].ToString() = "0" Then
                context.ReportDiagnostic(Diagnostic.Create(S_DiagnosticDescriptor, location:=Parameter.Default.GetLocation))
            End If
        End Sub

    End Class

    <ExportCodeFixProvider(LanguageNames.VisualBasic, NameOf(UseDefaultOptionalParameter_CodeFix)), [Shared]>
    Friend NotInheritable Class UseDefaultOptionalParameter_CodeFix
        Inherits CodeFixProvider
        Private Const title As String = "Remove the default value"

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(UseDefaultOptionalParameter_Analyser.ID)
            End Get
        End Property

        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim Diagnostic = context.Diagnostics.First()
            Dim diagnosticSpan = Diagnostic.Location.SourceSpan
            ' Find the type declaration identified by the diagnostic.
            Dim Parameter = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType(Of ParameterSyntax)().FirstOrDefault()
            If Parameter Is Nothing Then Return
            Dim Fix = Async Function(c As CancellationToken) As Task(Of Document)
                          Return Await context.Document.ReplaceNodeAsync(Parameter, Parameter.WithDefault(Nothing), context.CancellationToken).ConfigureAwait(False)
                      End Function
            ' Register a code action that will invoke the fix.
            context.RegisterCodeFix(New MyCodeAction(title:=title, createChangedDocument:=Fix), Diagnostic)
        End Function

        Private Class MyCodeAction
            Inherits CodeAction.DocumentChangeAction
            Public Sub New(title As String, createChangedDocument As Func(Of CancellationToken, Task(Of Document)))
                MyBase.New(title, createChangedDocument, title)
            End Sub
        End Class

    End Class


End Namespace
