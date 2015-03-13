' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Partial Class MethodSymbol

        Protected Function ValidateGenericConstraintsOnExtensionMethodDefinition() As Boolean

            If Me.Arity = 0 Then
                Return True
            End If

            Dim firstParam As ParameterSymbol = Me.Parameters(0)

            Dim typeParameters As New HashSet(Of TypeParameterSymbol)

            firstParam.Type.CollectReferencedTypeParameters(typeParameters)

            If typeParameters.Count > 0 Then
                For Each typeParameter In typeParameters
                    For Each constraintType As TypeSymbol In typeParameter.ConstraintTypesNoUseSiteDiagnostics
                        If constraintType.ReferencesTypeParameterNotInTheSet(typeParameters) Then
                            Return False
                        End If
                    Next
                Next
            End If

            Return True
        End Function

    End Class

End Namespace
