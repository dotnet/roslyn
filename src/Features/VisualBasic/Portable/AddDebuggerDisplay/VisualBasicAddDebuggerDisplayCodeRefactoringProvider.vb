' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.AddDebuggerDisplay
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddDebuggerDisplay

    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicAddDebuggerDisplayCodeRefactoringProvider)), [Shared]>
    Friend NotInheritable Class VisualBasicAddDebuggerDisplayCodeRefactoringProvider
        Inherits AbstractAddDebuggerDisplayCodeRefactoringProvider(Of TypeBlockSyntax, MethodStatementSyntax)

        Protected Overrides Function IsToStringOverride(methodDeclaration As MethodStatementSyntax) As Boolean
            ' Purposely bails for efficiency if no "ToString" override is in the same syntax tree, regardless of whether
            ' it's declared in another partial class file. Since the DebuggerDisplay attribute will refer to it, it's
            ' nicer to have them both in the same file anyway.

            If methodDeclaration Is Nothing Then Return False
            If methodDeclaration.GetArity <> 0 Then Return False
            If methodDeclaration.ParameterList?.Parameters.Any Then Return False
            If Not methodDeclaration.Modifiers.Any(SyntaxKind.OverridesKeyword) Then Return False

            Return True
        End Function
    End Class
End Namespace
