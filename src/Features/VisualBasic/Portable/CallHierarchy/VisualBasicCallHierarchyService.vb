' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CallHierarchy
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.CallHierarchy
    <ExportLanguageService(GetType(ICallHierarchyService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicCallHierarchyService
        Inherits AbstractCallHierarchyService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Async Function GetOperationRootSyntaxAsync(syntaxReference As SyntaxReference, cancellationToken As CancellationToken) As Task(Of SyntaxNode)
            Dim syntax = Await MyBase.GetOperationRootSyntaxAsync(syntaxReference, cancellationToken).ConfigureAwait(False)

            ' In VB, declaration syntax references point at statement nodes (for example,
            ' MethodStatement/PropertyStatement) instead of the enclosing block node. GetOperation
            ' returns null for the statement but non-null for the block, so we step to the parent.
            If syntax.Parent IsNot Nothing Then
                syntax = syntax.Parent
            End If

            Return syntax
        End Function
    End Class
End Namespace
