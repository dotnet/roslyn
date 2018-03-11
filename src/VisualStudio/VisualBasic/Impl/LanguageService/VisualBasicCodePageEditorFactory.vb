' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.LanguageServices.Implementation

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    <Guid(Guids.VisualBasicCodePageEditorFactoryIdString)>
    Friend Class VisualBasicCodePageEditorFactory
        Inherits AbstractCodePageEditorFactory

        Public Sub New(editorFactory As AbstractEditorFactory)
            MyBase.New(editorFactory)
        End Sub
    End Class
End Namespace
