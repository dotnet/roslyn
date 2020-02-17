' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    Public Class ITextSnapshotExtensionsTests
        <Fact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ThrowsWithNullSnapshot()
            Assert.Throws(Of ArgumentNullException)(Sub()
                                                        EndConstructExtensions.GetAligningWhitespace(Nothing, 0)
                                                    End Sub)
        End Sub
    End Class
End Namespace
