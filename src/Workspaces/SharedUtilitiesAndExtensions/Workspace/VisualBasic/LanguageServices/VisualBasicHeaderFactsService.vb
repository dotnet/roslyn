' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageService
    <ExportLanguageService(GetType(IHeaderFactsService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicHeaderFactsService
        Inherits VisualBasicHeaderFacts
        Implements IHeaderFactsService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
