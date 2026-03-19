' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.LanguageService

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LanguageServices
    '<Export(GetType(VisualBasicSymbolDisplayService))>
    Friend Class VisualBasicSymbolDisplayService
        Inherits AbstractSymbolDisplayService

        Public Sub New(provider As Host.LanguageServices)
            MyBase.New(provider)
        End Sub

        Protected Overrides Function CreateDescriptionBuilder(semanticModel As SemanticModel,
                                                              position As Integer,
                                                              options As SymbolDescriptionOptions,
                                                              cancellationToken As CancellationToken) As AbstractSymbolDescriptionBuilder
            Return New SymbolDescriptionBuilder(semanticModel, position, LanguageServices, options, cancellationToken)
        End Function
    End Class
End Namespace
