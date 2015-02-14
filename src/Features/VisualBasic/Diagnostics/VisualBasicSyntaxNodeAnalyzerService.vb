' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Diagnostics.EngineV1
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics
    <ExportLanguageService(GetType(ISyntaxNodeAnalyzerService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicSyntaxNodeAnalyzerService
        Inherits AbstractSyntaxNodeAnalyzerService(Of SyntaxKind)

        Public Sub New()
        End Sub

        Protected Overrides Function GetSyntaxKindEqualityComparer() As IEqualityComparer(Of SyntaxKind)
            Return SyntaxFacts.EqualityComparer
        End Function

        Protected Overrides Function GetKind(node As SyntaxNode) As SyntaxKind
            Return node.Kind
        End Function
    End Class
End Namespace
