' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.MakeMethodAsynchronous

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeMethodAsynchronous
    <ExportLanguageService(GetType(IMakeMethodAsynchronousService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicMakeMethodAsynchronousService
        Implements IMakeMethodAsynchronousService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function IsAsyncReturnType(type As ITypeSymbol, knownTaskTypes As KnownTaskTypes) As Boolean Implements IMakeMethodAsynchronousService.IsAsyncReturnType
            Return knownTaskTypes.IsTaskLikeType(type)
        End Function
    End Class
End Namespace
