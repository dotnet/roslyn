' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LanguageServices
    '<Export(GetType(VisualBasicSymbolDisplayService))>
    Friend Class VisualBasicSymbolDisplayService
        Inherits AbstractSymbolDisplayService

        Public Sub New(provider As HostLanguageServices)
            MyBase.New(provider.GetService(Of IAnonymousTypeDisplayService)())
        End Sub

        Public Overrides Function ToDisplayParts(symbol As ISymbol, Optional format As SymbolDisplayFormat = Nothing) As ImmutableArray(Of SymbolDisplayPart)
            Return Microsoft.CodeAnalysis.VisualBasic.SymbolDisplay.ToDisplayParts(symbol, format)
        End Function

        Public Overrides Function ToMinimalDisplayParts(semanticModel As SemanticModel,
                                                        position As Integer,
                                                        symbol As ISymbol,
                                                        format As SymbolDisplayFormat) As ImmutableArray(Of SymbolDisplayPart)
            Return symbol.ToMinimalDisplayParts(semanticModel, position, format)
        End Function

        Protected Overrides Function CreateDescriptionBuilder(workspace As Workspace,
                                                              semanticModel As SemanticModel,
                                                              position As Integer,
                                                              cancellationToken As CancellationToken) As AbstractSymbolDescriptionBuilder
            Return New SymbolDescriptionBuilder(Me, semanticModel, position, workspace, Me.AnonymousTypeDisplayService, cancellationToken)
        End Function
    End Class
End Namespace
