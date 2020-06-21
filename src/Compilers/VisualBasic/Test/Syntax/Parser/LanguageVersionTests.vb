' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Public Class LanguageVersionTests
    <Fact>
    Public Sub CurrentVersion()
        Dim highest = System.Enum.
            GetValues(GetType(LanguageVersion)).
            Cast(Of LanguageVersion).
            Where(Function(x) x <> LanguageVersion.Latest).
            Max()

        Assert.Equal(LanguageVersionFacts.CurrentVersion, highest)
    End Sub
End Class
