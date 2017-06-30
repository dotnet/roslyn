' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Structure

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public MustInherit Class AbstractVisualBasicSyntaxNodeStructureProviderTests(Of TSyntaxNode As SyntaxNode)
        Inherits AbstractSyntaxNodeStructureProviderTests(Of TSyntaxNode)

        Protected NotOverridable Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
