' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageService
    <ExportLanguageService(GetType(IFileBannerFactsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicFileBannerFactsService
        Inherits VisualBasicFileBannerFacts
        Implements IFileBannerFactsService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
