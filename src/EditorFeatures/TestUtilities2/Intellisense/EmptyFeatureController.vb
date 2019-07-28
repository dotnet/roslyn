' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend NotInheritable Class EmptyFeatureController
        Implements IFeatureController

        Friend Shared ReadOnly Instance As EmptyFeatureController = New EmptyFeatureController()

        Private Sub New()
        End Sub
    End Class
End Namespace
