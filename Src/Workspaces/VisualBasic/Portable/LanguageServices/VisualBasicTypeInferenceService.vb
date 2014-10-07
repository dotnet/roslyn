' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageService(GetType(ITypeInferenceService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicTypeInferenceService
        Implements ITypeInferenceService

        Public Function InferTypes(semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As IEnumerable(Of ITypeSymbol) Implements ITypeInferenceService.InferTypes
            Return New TypeInferrer(semanticModel, cancellationToken).InferTypes(position)
        End Function

        Public Function InferTypes(semanticModel As SemanticModel, expression As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of ITypeSymbol) Implements ITypeInferenceService.InferTypes
            Return New TypeInferrer(semanticModel, cancellationToken).InferTypes(TryCast(expression, ExpressionSyntax))
        End Function
    End Class
End Namespace
