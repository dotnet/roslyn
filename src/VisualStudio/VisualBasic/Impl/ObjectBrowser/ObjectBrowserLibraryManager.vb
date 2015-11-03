' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ObjectBrowser
    Friend Class ObjectBrowserLibraryManager
        Inherits AbstractObjectBrowserLibraryManager

        Public Sub New(serviceProvider As IServiceProvider)
            MyBase.New(LanguageNames.VisualBasic, Guids.VisualBasicLibraryId, __SymbolToolLanguage.SymbolToolLanguage_VB, serviceProvider)
        End Sub

        Friend Overrides Function CreateDescriptionBuilder(
            description As IVsObjectBrowserDescription3,
            listItem As ObjectListItem,
            project As Project
        ) As AbstractDescriptionBuilder

            Return New DescriptionBuilder(description, Me, listItem, project)
        End Function

        Friend Overrides Function CreateListItemFactory() As AbstractListItemFactory
            Return New ListItemFactory()
        End Function

    End Class
End Namespace
