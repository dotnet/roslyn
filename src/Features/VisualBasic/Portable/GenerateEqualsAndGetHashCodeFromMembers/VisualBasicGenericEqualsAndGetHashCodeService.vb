' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateEqualsAndGetHashCodeFromMembers
    <ExportLanguageService(GetType(IGenerateEqualsAndGetHashCodeService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGenericEqualsAndGetHashCodeService
        Inherits AbstractGenerateEqualsAndGetHashCodeService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function TryWrapWithUnchecked(statements As ImmutableArray(Of SyntaxNode), ByRef wrappedStatements As ImmutableArray(Of SyntaxNode)) As Boolean
            ' VB doesn't support 'unchecked' statements.
            Return False
        End Function
    End Class
End Namespace
