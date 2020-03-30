﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.OverloadBase
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddOverloads), [Shared]>
    Partial Friend Class OverloadBaseCodeFixProvider
        Inherits CodeFixProvider

        Friend Const BC40003 As String = "BC40003" ' '{0} '{1}' shadows an overloadable member declared in the base class '{2}'.  If you want to overload the base method, this method must be declared 'Overloads'.
        Friend Const BC40004 As String = "BC40004" ' '{0} '{1}' overloads an overloadable member declared in the base class '{2}'.  If you want to shadow the base method, this method must be declared 'Shadows'.

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC40003, BC40004)
            End Get
        End Property

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)

            Dim diagnostic = context.Diagnostics.First()
            Dim diagnosticSpan = diagnostic.Location.SourceSpan

            Dim token = root.FindToken(diagnosticSpan.Start)

            If TypeOf token.Parent IsNot PropertyStatementSyntax AndAlso TypeOf token.Parent IsNot MethodStatementSyntax Then
                Return
            End If

            If diagnostic.Descriptor.Id = BC40003 Then
                context.RegisterCodeFix(New AddKeywordAction(context.Document, token.Parent, VBFeaturesResources.Add_Overloads, SyntaxKind.OverloadsKeyword), context.Diagnostics)
            ElseIf diagnostic.Descriptor.Id = BC40004 Then
                context.RegisterCodeFix(New AddKeywordAction(context.Document, token.Parent, VBFeaturesResources.Add_Shadows, SyntaxKind.ShadowsKeyword), context.Diagnostics)
            End If
        End Function
    End Class
End Namespace
