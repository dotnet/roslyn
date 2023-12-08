' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class WhiteSpacesInsertTests
        <WpfFact>
        Public Sub VerifyInsertWhiteSpace()
            VerifyStatementEndConstructApplied(
                before:="Class X
  Sub y()
End Class",
                beforeCaret:={1, -1},
                 after:="Class X
  Sub y()

  End Sub
End Class",
                afterCaret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyInsertTabSpace()
            VerifyStatementEndConstructApplied(
                before:="Class X
		Sub y()
End Class",
                beforeCaret:={1, -1},
                 after:="Class X
		Sub y()

		End Sub
End Class",
                afterCaret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyInsertDoubleWideWhiteSpace()
            VerifyStatementEndConstructApplied(
                before:="Class X
 Sub y()
End Class",
                beforeCaret:={1, -1},
                 after:="Class X
 Sub y()

 End Sub
End Class",
                afterCaret:={2, -1})

        End Sub
    End Class
End Namespace
