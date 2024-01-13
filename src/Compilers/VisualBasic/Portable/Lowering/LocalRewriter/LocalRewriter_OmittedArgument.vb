' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitOmittedArgument(node As BoundOmittedArgument) As BoundNode
            Debug.Assert(node.Type.IsObjectType)

            Dim missingField As FieldSymbol = Nothing
            If Not TryGetWellknownMember(missingField, WellKnownMember.System_Reflection_Missing__Value, node.Syntax) Then
                Return node
            End If

            Dim fieldAccess = New BoundFieldAccess(node.Syntax, Nothing, missingField, isLValue:=False, type:=missingField.Type)
            Dim useSiteInfo = GetNewCompoundUseSiteInfo()
            Dim conversion = Conversions.ClassifyDirectCastConversion(fieldAccess.Type, node.Type, useSiteInfo)
            _diagnostics.Add(node, useSiteInfo)

            ' Return Directcast(Missing.Value, Object)
            Return New BoundDirectCast(node.Syntax, fieldAccess, conversion, node.Type)
        End Function

    End Class
End Namespace
