' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
