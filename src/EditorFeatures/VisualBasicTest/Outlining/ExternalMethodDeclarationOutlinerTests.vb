' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class ExternalMethodDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of DeclareStatementSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxStructureProvider
            Return New ExternalMethodDeclarationOutliner()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestExternalMethodDeclarationWithComments() As Task
            Const code = "
Class C
    {|span:'Hello
    'World|}
    Declare Ansi Sub $$ExternSub Lib ""ExternDll"" ()
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Function
    End Class
End Namespace