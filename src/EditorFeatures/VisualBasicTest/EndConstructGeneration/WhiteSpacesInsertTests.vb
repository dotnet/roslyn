' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    Public Class WhiteSpacesInsertTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInsertWhiteSpace() As Threading.Tasks.Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class X
  Sub y()
End Class",
                beforeCaret:={1, -1},
                 after:="Class X
  Sub y()

  End Sub
End Class",
                afterCaret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInsertTabSpace() As Threading.Tasks.Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class X
		Sub y()
End Class",
                beforeCaret:={1, -1},
                 after:="Class X
		Sub y()

		End Sub
End Class",
                afterCaret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInsertDoubleWideWhiteSpace() As Threading.Tasks.Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class X
 Sub y()
End Class",
                beforeCaret:={1, -1},
                 after:="Class X
 Sub y()

 End Sub
End Class",
                afterCaret:={2, -1})

        End Function
    End Class
End Namespace
