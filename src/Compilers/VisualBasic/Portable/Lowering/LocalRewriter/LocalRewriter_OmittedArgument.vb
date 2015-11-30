' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitOmittedArgument(node As BoundOmittedArgument) As BoundNode
            Debug.Assert(node.Type.IsObjectType)

            Dim missingField As FieldSymbol = Nothing
            If Not TryGetWellknownMember(missingField, WellKnownMember.System_Reflection_Missing__Value, node.Syntax) Then
                Return node
            End If

            Dim fieldAccess = New BoundFieldAccess(node.Syntax, Nothing, missingField, isLValue:=False, type:=missingField.Type)
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            Dim conversion = Conversions.ClassifyDirectCastConversion(fieldAccess.Type, node.Type, useSiteDiagnostics)
            _diagnostics.Add(node, useSiteDiagnostics)

            ' Return Directcast(Missing.Value, Object)
            Return New BoundDirectCast(node.Syntax, fieldAccess, conversion, node.Type)
        End Function

    End Class
End Namespace
