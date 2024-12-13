' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Friend Class Pass
        Inherits MigrationType

        Public Sub New(input As Input)
            MyBase.New(input)
        End Sub

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim pass = TryCast(obj, Pass)
            Return pass IsNot Nothing AndAlso
                   EqualityComparer(Of Input).Default.Equals(Input, pass.Input)
        End Function
    End Class
End Namespace
