' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.FindUsages
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.FindUsages
    <ExportLanguageService(GetType(IFindUsagesService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicFindUsagesService
        Inherits AbstractFindUsagesService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
