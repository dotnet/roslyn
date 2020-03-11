' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection
Imports Microsoft.CodeAnalysis.Scripting.Hosting
Imports MemberFilter = Microsoft.CodeAnalysis.Scripting.Hosting.MemberFilter

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting

    Friend Class VisualBasicObjectFormatterImpl
        Inherits CommonObjectFormatter

        Protected Overrides ReadOnly Property TypeNameFormatter As CommonTypeNameFormatter
        Protected Overrides ReadOnly Property PrimitiveFormatter As CommonPrimitiveFormatter
        Protected Overrides ReadOnly Property Filter As MemberFilter

        Friend Sub New()
            PrimitiveFormatter = New VisualBasicPrimitiveFormatter()
            TypeNameFormatter = New VisualBasicTypeNameFormatter(PrimitiveFormatter)
            Filter = New VisualBasicMemberFilter()
        End Sub

        Protected Overrides Function FormatRefKind(parameter As ParameterInfo) As String
            Return If(parameter.IsOut, "<Out> ByRef", "ByRef")
        End Function
    End Class

End Namespace
