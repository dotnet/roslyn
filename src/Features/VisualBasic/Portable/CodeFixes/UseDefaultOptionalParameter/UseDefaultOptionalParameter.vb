' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.UseDefaultOptionalParameter

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class UseDefaultOptionalParameter_Analyser
        Inherits DiagnosticAnalyzer

        Public Sub New()
            MyBase.New()
        End Sub

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)
            context.RegisterSyntaxNodeAction(AddressOf Analyse, S_Kinds)
        End Sub

        Friend Const ID As String = "BC00000" 'PROTOTYPE: Update with `offical` id.
        Private ReadOnly S_Kinds As SyntaxKind() = {SyntaxKind.Parameter}

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


        Private Sub Analyse(context As SyntaxNodeAnalysisContext)
            Dim Parameter = TryCast(context.Node, ParameterSyntax)
            If Parameter Is Nothing Then Exit Sub
            If Parameter.Default Is Nothing Then Exit Sub
            If Parameter.Default.Value IsNot Nothing Then Return
            Dim sym = TryCast(context.ContainingSymbol, IParameterSymbol)
            If sym Is Nothing Then Exit Sub
            If Not sym.IsOptional Then Exit Sub
            Dim [Default] = Parameter.Default.Value
            If [Default] Is Nothing Then Exit Sub
            If [Default].IsKind(SyntaxKind.NothingLiteralExpression, SyntaxKind.FalseLiteralExpression) OrElse
                [Default].ToString() = "0" Then
                context.ReportDiagnostic(Diagnostic.Create(S_DiagnosticDescriptor, location:=Parameter.Default.GetLocation))
            End If
        End Sub

    End Class

    <ExportCodeFixProvider(LanguageNames.VisualBasic, NameOf(UseDefaultOptionalParameter_CodeFix)), [Shared]>
    Friend NotInheritable Class UseDefaultOptionalParameter_CodeFix
        Inherits CodeFixProvider

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(UseDefaultOptionalParameter_Analyser.ID)
            End Get
        End Property

        Private Shared ReadOnly Title As String = VBFeaturesResources.Remove_the_default_value

        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim Diagnostic = context.Diagnostics.FirstOrDefault
            If Diagnostic Is Nothing Then Return
            Dim Parameter = root.FindToken(context.Span.Start).Parent.AncestorsAndSelf().OfType(Of ParameterSyntax)().FirstOrDefault()
            If Parameter Is Nothing Then Return
            Dim Fix = Async Function(c As CancellationToken)
                          Return context.Document.WithSyntaxRoot(Await Parameter.WithDefault(Nothing).SyntaxTree.GetRootAsync(c).ConfigureAwait(False))
                      End Function

            ' Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                New MyCodeAction(title:=Title, createChangedDocument:=Fix), diagnostic:=Diagnostic)
        End Function

        Private Class MyCodeAction
            Inherits CodeAction.DocumentChangeAction
            Public Sub New(title As String, createChangedDocument As Func(Of CancellationToken, Task(Of Document)))
                MyBase.New(title, createChangedDocument, title)
            End Sub

        End Class

    End Class


End Namespace
