﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.BraceCompletion
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion
    <ExportLanguageService(GetType(IBraceCompletionServiceFactory), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicBraceCompletionServiceFactory
        Inherits AbstractBraceCompletionServiceFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(
            <ImportMany(LanguageNames.VisualBasic)> braceCompletionServices As IEnumerable(Of IBraceCompletionService),
            threadingContext As IThreadingContext)
            MyBase.New(braceCompletionServices, threadingContext)
        End Sub
    End Class
End Namespace
