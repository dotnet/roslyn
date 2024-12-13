' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Newtonsoft.Json

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Friend Class EnumIntegerToString
        Inherits MigrationType

        <JsonProperty(NameOf(Map))>
        Public ReadOnly Property Map As Map()

        Public Sub New(map As Map(), input As Input)
            MyBase.New(input)
            Me.Map = map
        End Sub
    End Class
End Namespace
