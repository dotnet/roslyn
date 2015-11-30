' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundBadExpression
        Inherits BoundExpression

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(HasErrors)
        End Sub
#End If

    End Class

End Namespace
