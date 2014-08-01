' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class BoundSpillSequence

        Public Overrides ReadOnly Property IsLValue As Boolean
            Get
                Return If(Me.ValueOpt IsNot Nothing, Me.ValueOpt.IsLValue, False)
            End Get
        End Property

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Throw ExceptionUtilities.Unreachable
        End Function

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(Me.ValueOpt Is Nothing OrElse Me.ValueOpt.Kind <> BoundKind.SpillSequence)
        End Sub
#End If

    End Class

End Namespace
