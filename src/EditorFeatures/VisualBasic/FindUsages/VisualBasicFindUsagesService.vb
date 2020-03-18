' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.FindUsages
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports VS.IntelliNav.Contracts

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.FindUsages
    <ExportLanguageService(GetType(IFindUsagesService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicFindUsagesService
        Inherits AbstractFindUsagesService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(threadingContext As IThreadingContext,
                       <Import(AllowDefault:=True)> codeIndexProvider As ICodeIndexProvider)
            MyBase.New(threadingContext, codeIndexProvider)
        End Sub
    End Class
End Namespace
