' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
