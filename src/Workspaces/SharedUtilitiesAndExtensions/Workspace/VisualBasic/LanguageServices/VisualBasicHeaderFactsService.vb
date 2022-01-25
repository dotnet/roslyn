﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageServices
    <ExportLanguageService(GetType(IHeaderFactsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicHeaderFactsService
        Inherits VisualBasicHeaderFacts
        Implements IHeaderFactsService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
