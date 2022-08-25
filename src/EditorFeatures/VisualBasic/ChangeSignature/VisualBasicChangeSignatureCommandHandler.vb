' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ChangeSignature
    <Export(GetType(ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(PredefinedCommandHandlerNames.ChangeSignature)>
    Friend Class VisualBasicChangeSignatureCommandHandler
        Inherits AbstractChangeSignatureCommandHandler

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New(threadingContext As IThreadingContext, globalOptions As IGlobalOptionService)
            MyBase.New(threadingContext, globalOptions)
        End Sub
    End Class
End Namespace
