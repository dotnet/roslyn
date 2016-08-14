' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Friend Class SymbolCompletionFormat
        Inherits AbstractSymbolCompletionFormat

        Public Shared ReadOnly [Default] As SymbolCompletionFormat = New SymbolCompletionFormat(
            displayFormat:=SymbolDisplayFormat.MinimallyQualifiedFormat _
                                              .RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers),
            insertionFormat:=SymbolDisplayFormat.MinimallyQualifiedFormat)

        Public Sub New(displayFormat As SymbolDisplayFormat, insertionFormat As SymbolDisplayFormat)
            MyBase.New(displayFormat, insertionFormat, "("c)
        End Sub

        Protected NotOverridable Overrides Function Escape(identifier As String, context As SyntaxContext) As String
            Return identifier.EscapeIdentifier(context)
        End Function
    End Class
End Namespace
