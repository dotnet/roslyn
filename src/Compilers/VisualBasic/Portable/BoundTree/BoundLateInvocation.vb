' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Diagnostics

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class BoundLateInvocation
        ''' <summary>
        ''' Updates access kind. To clear the access kind,
        ''' 'newAccessKind' should be Unknown. Otherwise, the current
        ''' access kind should be Unknown or equal to 'newAccessKind'.
        ''' </summary>
        Public Function SetAccessKind(newAccessKind As LateBoundAccessKind) As BoundLateInvocation
            Debug.Assert(newAccessKind = LateBoundAccessKind.Unknown OrElse
                    Me.AccessKind = LateBoundAccessKind.Unknown OrElse
                    Me.AccessKind = newAccessKind)

            Dim member As BoundExpression = Me.Member

            If member.Kind = BoundKind.LateMemberAccess Then
                member = DirectCast(member, BoundLateMemberAccess).SetAccessKind(newAccessKind)
            End If

            Return Me.Update(member, Me.ArgumentsOpt, Me.ArgumentNamesOpt, newAccessKind, Me.MethodOrPropertyGroupOpt, Me.Type)
        End Function

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert((AccessKind And LateBoundAccessKind.Call) = 0 OrElse (AccessKind And Not LateBoundAccessKind.Call) = 0)

            If Member.Kind = BoundKind.LateMemberAccess Then
                Debug.Assert(DirectCast(Member, BoundLateMemberAccess).AccessKind = Me.AccessKind)
            End If

            Debug.Assert(Type.IsObjectType())
        End Sub
#End If
    End Class

End Namespace
