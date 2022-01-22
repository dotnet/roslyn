' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Rename

Namespace Microsoft.CodeAnalysis.VisualBasic.Rename
    <ExportLanguageServiceFactory(GetType(IRenameRewriterLanguageService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicRenameRewriterLanguageServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(provider As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return VisualBasicRenameRewriterLanguageService.Instance
        End Function
    End Class
End Namespace
