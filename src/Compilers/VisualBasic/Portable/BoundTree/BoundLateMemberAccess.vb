' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    <Flags()>
    Friend Enum LateBoundAccessKind
        Unknown = 0
        [Get] = &H1
        [Set] = &H2
        [Call] = &H4 ' Result is not needed and we are not going to assign to the member. Cannot be combined with [Get] or [Set]
    End Enum

    Partial Friend Class BoundLateMemberAccess
        ''' <summary>
        ''' Updates property access kind. To clear the access kind,
        ''' 'newAccessKind' should be Unknown. Otherwise, the current
        ''' access kind should be Unknown or equal to 'newAccessKind'.
        ''' </summary>
        Public Function SetAccessKind(newAccessKind As LateBoundAccessKind) As BoundLateMemberAccess
            Debug.Assert(newAccessKind = LateBoundAccessKind.Unknown OrElse
                    Me.AccessKind = LateBoundAccessKind.Unknown OrElse
                    Me.AccessKind = newAccessKind)

            Return Me.Update(Me.NameOpt, Me.ContainerTypeOpt, Me.ReceiverOpt, Me.TypeArgumentsOpt, newAccessKind, Me.Type)
        End Function

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert((AccessKind And LateBoundAccessKind.Call) = 0 OrElse (AccessKind And Not LateBoundAccessKind.Call) = 0)
            Debug.Assert(Type.IsObjectType())
            Debug.Assert(ReceiverOpt Is Nothing OrElse ReceiverOpt.Kind <> BoundKind.TypeExpression)
        End Sub
#End If
    End Class

End Namespace
