' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
Imports Microsoft.VisualStudio.ComponentModelHost

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ObjectBrowser
    Friend Class ObjectBrowserLibraryManager
        Inherits AbstractObjectBrowserLibraryManager

        Public Sub New(serviceProvider As IServiceProvider, componentModel As IComponentModel, workspace As VisualStudioWorkspace)
            MyBase.New(LanguageNames.VisualBasic, Guids.VisualBasicLibraryId, serviceProvider, componentModel, workspace)
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
