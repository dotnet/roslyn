' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Friend NotInheritable Class VisualBasicSimplification
        Inherits AbstractSimplification

        Public Shared ReadOnly Instance As New VisualBasicSimplification()

        Public Overrides ReadOnly Property DefaultOptions As SimplifierStyleOptions
            Get
                Return VisualBasicSimplifierStyleOptions.Default
            End Get
        End Property

        Public Overrides Function GetSimplifierOptions(options As IOptionsReader, fallbackOptions As SimplifierStyleOptions) As SimplifierStyleOptions
            Return New VisualBasicSimplifierStyleOptions(options, If(DirectCast(fallbackOptions, VisualBasicSimplifierStyleOptions), VisualBasicSimplifierStyleOptions.Default))
        End Function
    End Class
End Namespace
