' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Reflection
Imports Microsoft.CodeAnalysis.Scripting.Hosting

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting

    Public Class VisualBasicObjectFormatter
        Inherits CommonObjectFormatter

        Public Shared ReadOnly Property Instance As New VisualBasicObjectFormatter()

        Friend Overrides ReadOnly Property TypeNameFormatter As CommonTypeNameFormatter
        Friend Overrides ReadOnly Property PrimitiveFormatter As CommonPrimitiveFormatter
        Friend Overrides ReadOnly Property Filter As MemberFilter

        Friend Sub New()
            PrimitiveFormatter = New VisualBasicPrimitiveFormatter()
            TypeNameFormatter = New VisualBasicTypeNameFormatter(PrimitiveFormatter)
            Filter = New VisualBasicMemberFilter()
        End Sub

        Friend Overrides Function FormatRefKind(parameter As ParameterInfo) As String
            Return If(parameter.IsOut, "ByRef", "")
        End Function
    End Class

End Namespace
