' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.UnitTests
    Partial Public Class NamingStyleTests
        Private Function CreateNamingStyle(
            Optional prefix As String = "",
            Optional suffix As String = "",
            Optional wordSeparator As String = "",
            Optional capitalizationScheme As Capitalization = Capitalization.PascalCase) As MutableNamingStyle

            Return New MutableNamingStyle With
                {
                    .Prefix = prefix,
                    .Suffix = suffix,
                    .WordSeparator = wordSeparator,
                    .CapitalizationScheme = capitalizationScheme
                }
        End Function

        Private Sub TestNameCreation(namingStyle As MutableNamingStyle, expectedName As String, ParamArray words As String())
            Assert.Equal(expectedName, namingStyle.NamingStyle.CreateName(words.ToImmutableArray()))
        End Sub

        Private Sub TestNameCompliance(namingStyle As MutableNamingStyle, candidateName As String)
            Dim reason As String = Nothing
            Assert.True(namingStyle.NamingStyle.IsNameCompliant(candidateName, reason))
        End Sub

        Private Sub TestNameNoncomplianceAndFixedNames(namingStyle As MutableNamingStyle, candidateName As String, ParamArray expectedFixedNames As String())
            Dim reason As String = Nothing
            Assert.False(namingStyle.NamingStyle.IsNameCompliant(candidateName, reason))

            Dim actualFixedNames = namingStyle.NamingStyle.MakeCompliant(candidateName).ToList()

            Assert.Equal(expectedFixedNames.Length, actualFixedNames.Count)
            For i = 0 To expectedFixedNames.Length - 1
                Assert.Equal(expectedFixedNames(i), actualFixedNames(i))
            Next
        End Sub
    End Class
End Namespace
