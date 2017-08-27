﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class ConstructorDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of SubNewStatementSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New ConstructorDeclarationStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestConstructor1() As Task
            Const code = "
Class C1
    {|span:Sub $$New()
    End Sub|}
End Class
"
            Await VerifyBlockSpansAsync(code,
                Region("span", "Sub New() ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestConstructor2() As Task
            Const code = "
Class C1
    {|span:Sub $$New()
    End Sub|}                     
End Class
"
            Await VerifyBlockSpansAsync(code,
                Region("span", "Sub New() ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestConstructor3() As Task
            Const code = "
Class C1
    {|span:Sub $$New()
    End Sub|} ' .ctor
End Class
"
            Await VerifyBlockSpansAsync(code,
                Region("span", "Sub New() ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestPrivateConstructor() As Task
            Const code = "
Class C1
    {|span:Private Sub $$New()
    End Sub|}
End Class
"
            Await VerifyBlockSpansAsync(code,
                Region("span", "Private Sub New() ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestConstructorWithComments() As Task
            Const code = "
Class C1
    {|span1:'My
    'Constructor|}
    {|span2:Sub $$New()
    End Sub|}
End Class
"
            Await VerifyBlockSpansAsync(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Sub New() ...", autoCollapse:=True))
        End Function
    End Class
End Namespace
