' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class DelegateDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of DelegateStatementSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New DelegateDeclarationStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestDelegateWithComments() As Task
            Const code = "
{|span:'Hello
'World|}
Delegate Sub $$Goo()
"
            Await VerifyBlockSpansAsync(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Function
    End Class
End Namespace
