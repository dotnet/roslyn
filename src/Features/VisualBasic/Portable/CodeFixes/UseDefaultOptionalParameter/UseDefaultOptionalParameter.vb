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
        Private ReadOnly S_Kinds As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(Of SyntaxKind)(SyntaxKind.Parameter)

        Private Shared ReadOnly S_DiagnosticDescriptor As New DiagnosticDescriptor(ID,
                                                      VBFeaturesResources.DefaultOptionalParameter,
                                                      VBFeaturesResources.DefaultOptionalParameter,
                                                      "Style",
                                                      DiagnosticSeverity.Info, True, "", "", Array.Empty(Of String))

        Private Shared ReadOnly S_SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(S_DiagnosticDescriptor)

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return S_SupportedDiagnostics
            End Get
        End Property

        Private Function HasOptionalModifier(Modifiers As SyntaxTokenList) As Boolean
            If Modifiers.Count = 0 Then Return False
            For i = 0 To Modifiers.Count - 1
                If Modifiers(i).Kind = SyntaxKind.OptionalKeyword Then Return True
            Next
            Return False
        End Function

        Sub Analyse(context As SyntaxNodeAnalysisContext)
            Dim Parameter = TryCast(context.Node, ParameterSyntax)
            If Parameter Is Nothing Then Exit Sub
            Dim IsOptional = HasOptionalModifier(Parameter.Modifiers)
            If Not IsOptional Then Exit Sub
            Dim [Default] = Parameter?.Default?.Value
            If [Default] Is Nothing Then Exit Sub
            If [Default].IsKind(SyntaxKind.NothingLiteralExpression) OrElse [Default].IsKind(SyntaxKind.FalseLiteralExpression) OrElse
                [Default].ToString() = "0" Then
                context.ReportDiagnostic(Diagnostic.Create(S_DiagnosticDescriptor, location:=Parameter.Default.GetLocation))
            End If
        End Sub

    End Class

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(UseDefaultOptionalParameter_CodeFix)), [Shared]>
    Friend NotInheritable Class UseDefaultOptionalParameter_CodeFix
        Inherits CodeFixProvider
        Public Sub New()
            MyBase.New
        End Sub

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Private Shared ReadOnly _FixableIDS As ImmutableArray(Of String) = ImmutableArray.Create(UseDefaultOptionalParameter_Analyser.ID)

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return _FixableIDS
            End Get
        End Property

        Private Shared ReadOnly Title As String = VBFeaturesResources.Remove_the_default_value

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Return Task.Run(Sub()
                                Dim results =
                                context.Diagnostics.
                                                  WhereAsArray(Function(d) d.Id = UseDefaultOptionalParameter_Analyser.ID).
                                                  Select(Function(d) (TryCast(d.Location.FindNode(context.CancellationToken).Parent, ParameterSyntax), d))
                                For Each t As (Parameter As ParameterSyntax, d As Diagnostic) In results
                                    Dim Fix = Function(ct As CancellationToken) context.Document.ReplaceNodeAsync(t.Parameter, t.Parameter.WithDefault(Nothing), ct)

                                    context.RegisterCodeFix(New MyCodeAction(title:=Title, createChangedDocument:=Fix), t.d)
                                Next
                            End Sub)
        End Function

        Private Class MyCodeAction
            Inherits CodeAction.DocumentChangeAction
            Public Sub New(title As String, createChangedDocument As Func(Of CancellationToken, Task(Of Document)))
                MyBase.New(title, createChangedDocument, title)
            End Sub

        End Class


    End Class


End Namespace
