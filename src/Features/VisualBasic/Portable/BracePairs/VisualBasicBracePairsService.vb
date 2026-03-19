' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.BracePairs
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.BracePairs
    <ExportLanguageService(GetType(IBracePairsService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicBracePairsService
        Inherits AbstractBracePairsService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(VisualBasicSyntaxKinds.Instance)
        End Sub
    End Class
End Namespace
