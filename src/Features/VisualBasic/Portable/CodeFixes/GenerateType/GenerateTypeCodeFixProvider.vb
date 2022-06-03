' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.GenerateMember
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.GenerateType
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateType
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.GenerateType), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.GenerateVariable)>
    Friend Class GenerateTypeCodeFixProvider
        Inherits AbstractGenerateMemberCodeFixProvider

        Friend Const BC30002 As String = "BC30002" ' error BC30002: Type 'Test' is not defined.
        Friend Const BC30182 As String = "BC30182" ' error BC30182: Type expected.
        Friend Const BC30451 As String = "BC30451" ' error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.
        Friend Const BC30456 As String = "BC30456" ' error BC32043: error BC30456: 'B' is not a member of 'A'.
        Friend Const BC32042 As String = "BC32042" ' error BC32042: Too few type arguments to 'AA(Of T)'.
        Friend Const BC32043 As String = "BC32043" ' error BC32043: Too many type arguments to 'AA(Of T)'.
        Friend Const BC32045 As String = "BC32045" ' error BC32045: 'Goo' has no type parameters and so cannot have type arguments.
        Friend Const BC40056 As String = "BC40056" ' error BC40056: Namespace or type specified in the Imports 'A' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30002, IDEDiagnosticIds.UnboundIdentifierId, BC30182, BC30451, BC30456, BC32042, BC32043, BC32045, BC40056)
            End Get
        End Property

        Protected Overrides Function GetCodeActionsAsync(document As Document, node As SyntaxNode, fallbackOptions As CleanCodeGenerationOptionsProvider, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of CodeAction))
            Dim service = document.GetLanguageService(Of IGenerateTypeService)()
            Return service.GenerateTypeAsync(document, node, fallbackOptions, cancellationToken)
        End Function

        Protected Overrides Function IsCandidate(node As SyntaxNode, token As SyntaxToken, diagnostic As Diagnostic) As Boolean
            Dim qualified = TryCast(node, QualifiedNameSyntax)
            If qualified IsNot Nothing Then
                Return True
            End If

            Dim simple = TryCast(node, SimpleNameSyntax)
            If simple IsNot Nothing Then
                Return Not simple.IsParentKind(SyntaxKind.QualifiedName)
            End If

            Dim memberAccess = TryCast(node, MemberAccessExpressionSyntax)
            If memberAccess IsNot Nothing Then
                Return True
            End If

            Return False
        End Function

        Protected Overrides Function GetTargetNode(node As SyntaxNode) As SyntaxNode
            Return (DirectCast(node, ExpressionSyntax)).GetRightmostName()
        End Function
    End Class
End Namespace
