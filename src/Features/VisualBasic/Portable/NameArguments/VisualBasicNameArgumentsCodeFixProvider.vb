' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.NameArguments

Namespace Microsoft.CodeAnalysis.VisualBasic.NameArguments
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicNameArgumentsCodeFixProvider
        Inherits AbstractNameArgumentsCodeFixProvider

        Friend Overrides Function MakeNamedArgument(parameterName As String, node As SyntaxNode) As SyntaxNode
            Dim newArgument As SyntaxNode
            Select Case (node.Kind)
                Case SyntaxKind.SimpleArgument
                    'newArgument = DirectCast(node, ArgumentSyntax).WithoutTrivia().WithNameEqualsColon(SyntaxFactory.NameColon(parameterName)).WithTriviaFrom(argument)

            End Select

            '    Case ArgumentSyntax argument
            '        newArgument = argument.WithoutTrivia()
            '.WithNameColon(SyntaxFactory.NameColon(parameterName)).WithTriviaFrom(argument);
            '        break;
            '    Case AttributeArgumentSyntax argument
            '        newArgument = argument.WithoutTrivia()
            '.WithNameColon(SyntaxFactory.NameColon(parameterName)).WithTriviaFrom(argument);
            '        break;
            '    Default
            '        Throw ExceptionUtilities.UnexpectedValue(node.Kind());

            Return newArgument
        End Function
    End Class
End Namespace
