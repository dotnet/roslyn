' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.AddDebuggerDisplay
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddDebuggerDisplay

    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicAddDebuggerDisplayCodeRefactoringProvider)), [Shared]>
    Friend NotInheritable Class VisualBasicAddDebuggerDisplayCodeRefactoringProvider
        Inherits AbstractAddDebuggerDisplayCodeRefactoringProvider(Of TypeBlockSyntax, MethodStatementSyntax)

        Protected Overrides ReadOnly Property CanNameofAccessNonPublicMembersFromAttributeArgument As Boolean = False
    End Class
End Namespace
