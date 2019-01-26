' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
