' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ClassView
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ClassView
    Friend Class MockSyncClassViewCommandHandler
        Inherits AbstractSyncClassViewCommandHandler

        Public Sub New(threadingContext As IThreadingContext, serviceProvider As SVsServiceProvider)
            MyBase.New(threadingContext, serviceProvider)
        End Sub
    End Class
End Namespace
