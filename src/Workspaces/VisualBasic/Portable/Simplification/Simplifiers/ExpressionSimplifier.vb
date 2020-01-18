' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification.Simplifiers
    Friend Class ExpressionSimplifier
        Inherits AbstractVisualBasicSimplifier(Of ExpressionSyntax, ExpressionSyntax)

        Public Overrides Function TrySimplify(syntax As ExpressionSyntax,
                                              semanticModel As SemanticModel,
                                              optionSet As OptionSet,
                                              ByRef replacementNode As ExpressionSyntax,
                                              ByRef issueSpan As TextSpan,
                                              cancellationToken As CancellationToken) As Boolean
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
