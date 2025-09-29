' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundLoweredConditionalAccess

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(Me.PlaceholderId <> 0)
        End Sub
#End If
    End Class

End Namespace
