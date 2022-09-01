' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Completion
    <ExportCompletionProvider(NameOf(CSharpMockCompletionProvider), LanguageNames.CSharp)>
    <[Shared]>
    <PartNotDiscoverable>
    Friend Class CSharpMockCompletionProvider
        Inherits MockCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace
