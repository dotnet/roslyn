' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.LanguageServices.Implementation

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    <Guid(Guids.VisualBasicCodePageEditorFactoryIdString)>
    Friend NotInheritable Class VisualBasicCodePageEditorFactory
        Inherits AbstractCodePageEditorFactory

        Public Sub New(editorFactory As AbstractEditorFactory)
            MyBase.New(editorFactory)
        End Sub
    End Class
End Namespace
