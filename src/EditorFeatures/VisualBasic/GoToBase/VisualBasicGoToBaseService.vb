' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.GoToBase
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.GoToBase
    <ExportLanguageService(GetType(IGoToBaseService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGoToBaseService
        Inherits AbstractGoToBaseService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(globalOptions As IGlobalOptionService)
            MyBase.New(globalOptions)
        End Sub
    End Class
End Namespace
