' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class NamespaceDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of NamespaceStatementSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New NamespaceDeclarationStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestNamespace() As Task
            Const code = "
{|span:$$Namespace N1
End Namespace|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Namespace N1 ...", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestNamespaceWithComments() As Task
            Const code = "
{|span1:'My
'Namespace|}
{|span2:$$Namespace N1
End Namespace|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Namespace N1 ...", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestNamespaceWithNestedComments() As Task
            Const code = "
{|span1:$$Namespace N1
{|span2:'My
'Namespace|}
End Namespace|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "Namespace N1 ...", autoCollapse:=False),
                Region("span2", "' My ...", autoCollapse:=True))
        End Function
    End Class
End Namespace