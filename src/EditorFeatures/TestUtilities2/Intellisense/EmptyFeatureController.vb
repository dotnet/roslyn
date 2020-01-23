' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend NotInheritable Class EmptyFeatureController
        Implements IFeatureController

        Friend Shared ReadOnly Instance As EmptyFeatureController = New EmptyFeatureController()

        Private Sub New()
        End Sub
    End Class
End Namespace
