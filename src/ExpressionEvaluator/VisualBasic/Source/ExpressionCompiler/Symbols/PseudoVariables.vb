' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend Module PseudoVariables

        ' TODO: Set local type to runtime type. (See #1064306.)
        Friend Function GetVariableType(name As String) As Func(Of VisualBasicCompilation, TypeSymbol)
            Dim comparison = StringComparison.OrdinalIgnoreCase

            If String.Equals(name, "$exception", comparison) OrElse String.Equals(name, "$stowedexception", comparison) Then
                Return Function(c) c.GetWellKnownType(WellKnownType.System_Exception)
            End If

            If name.StartsWith("$ReturnValue", comparison) Then
                Dim suffix = name.Substring(12)
                Dim index As Integer = 0
                If suffix.Length = 0 OrElse Integer.TryParse(suffix, index) Then
                    Debug.Assert(index >= 0)
                    Return Function(c) c.GetSpecialType(SpecialType.System_Object)
                End If
            End If

            Return Nothing
        End Function

    End Module

End Namespace

