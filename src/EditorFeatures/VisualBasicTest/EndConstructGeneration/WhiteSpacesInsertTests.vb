' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    Public Class WhiteSpacesInsertTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
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
