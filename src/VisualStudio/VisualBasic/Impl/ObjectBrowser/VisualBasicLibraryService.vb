' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ObjectBrowser
    Friend Class VisualBasicLibraryService
        Inherits AbstractLibraryService

        Public Sub New()
            MyBase.New(Guids.VisualBasicLibraryId, __SymbolToolLanguage.SymbolToolLanguage_VB)
        End Sub
    End Class
End Namespace
