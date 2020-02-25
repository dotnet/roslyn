﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundLValueToRValueWrapper

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(UnderlyingLValue.IsLValue)
            Debug.Assert(UnderlyingLValue.Type.IsSameTypeIgnoringAll(Type))
        End Sub
#End If

    End Class
End Namespace
