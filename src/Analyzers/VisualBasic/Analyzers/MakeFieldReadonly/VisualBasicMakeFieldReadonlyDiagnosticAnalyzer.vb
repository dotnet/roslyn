' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.MakeFieldReadonly
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicMakeFieldReadonlyDiagnosticAnalyzer
        Inherits AbstractMakeFieldReadonlyDiagnosticAnalyzer(Of SyntaxKind, MeExpressionSyntax)

        Protected Overrides ReadOnly Property SyntaxKinds As ISyntaxKinds = VisualBasicSyntaxKinds.Instance

        Protected Overrides Function IsWrittenTo(semanticModel As SemanticModel, expression As MeExpressionSyntax, cancellationToken As CancellationToken) As Boolean
            Return expression.IsWrittenTo(semanticModel, cancellationToken)
        End Function

        Protected Overrides Function IsLanguageSpecificFieldWriteInConstructor(fieldReference As IFieldReferenceOperation, owningSymbol As ISymbol) As Boolean
            ' Legacy VB behavior.  If a special "feature:strict" (different from "option strict") flag Is on,
            ' then this write Is only ok if the containing types are the same.  *Not* simply the original
            ' definitions being the same (which the caller has already checked):
            ' https//github.com/dotnet/roslyn/blob/93d3aa1a2cf1790b1a0fe2d120f00987d50445c0/src/Compilers/VisualBasic/Portable/Binding/Binder_Expressions.vb#L1868-L1871

            If fieldReference.SemanticModel.SyntaxTree.Options.Features.ContainsKey("strict") Then
                Return Not fieldReference.Field.ContainingType.Equals(owningSymbol.ContainingType)
            End If

            Return False
        End Function
    End Class
End Namespace
