' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.CodeStyle
Imports System.Runtime.Serialization

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    <DataContract>
    Friend NotInheritable Class VisualBasicSimplifierOptions
        Inherits SimplifierOptions

        Public Sub New(Optional common As CommonOptions = Nothing)
            MyBase.New(common)
        End Sub

        Public Shared ReadOnly [Default] As New VisualBasicSimplifierOptions()

        Friend Overloads Shared Function Create(options As AnalyzerConfigOptions, fallbackOptions As VisualBasicSimplifierOptions) As VisualBasicSimplifierOptions
            fallbackOptions = If(fallbackOptions, VisualBasicSimplifierOptions.Default)
            Return New VisualBasicSimplifierOptions(CommonOptions.Create(options, fallbackOptions.Common))
        End Function
    End Class
End Namespace
