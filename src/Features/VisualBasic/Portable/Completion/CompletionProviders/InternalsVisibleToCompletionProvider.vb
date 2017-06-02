' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    Friend NotInheritable Class InternalsVisibleToCompletionProvider
        Inherits AbstractInternalsVisibleToCompletionProvider

        Friend Overrides Function IsInsertionTrigger(text As SourceText, insertedCharacterPosition As Integer, options As OptionSet) As Boolean
            Dim ch = text(insertedCharacterPosition)
            Return ch = """"c
        End Function

        Protected Overrides Function IsPositionEntirelyWithinStringLiteral(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Return syntaxTree.IsEntirelyWithinStringLiteral(position, cancellationToken)
        End Function
    End Class
End Namespace
