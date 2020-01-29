' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.ImplementAbstractClass
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.ImplementAbstractClass
    <ExportLanguageService(GetType(IImplementAbstractClassService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicImplementAbstractClassService
        Inherits AbstractImplementAbstractClassService(Of ClassBlockSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function TryInitializeState(
                document As Document, model As SemanticModel, classBlock As ClassBlockSyntax, cancellationToken As CancellationToken,
                ByRef classType As INamedTypeSymbol, ByRef abstractClassType As INamedTypeSymbol) As Boolean
            classType = model.GetDeclaredSymbol(classBlock.BlockStatement)
            abstractClassType = classType?.BaseType

            Return classType IsNot Nothing AndAlso
                   abstractClassType IsNot Nothing AndAlso
                   abstractClassType.IsAbstractClass()
        End Function
    End Class
End Namespace
