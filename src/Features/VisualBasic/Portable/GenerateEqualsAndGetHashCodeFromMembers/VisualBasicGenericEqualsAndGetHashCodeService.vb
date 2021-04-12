' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateEqualsAndGetHashCodeFromMembers
    <ExportLanguageService(GetType(IGenerateEqualsAndGetHashCodeService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGenericEqualsAndGetHashCodeService
        Inherits AbstractGenerateEqualsAndGetHashCodeService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function TryWrapWithUnchecked(statements As ImmutableArray(Of SyntaxNode), ByRef wrappedStatements As ImmutableArray(Of SyntaxNode)) As Boolean
            ' VB doesn't support 'unchecked' statements.
            Return False
        End Function
    End Class
End Namespace
