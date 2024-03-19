' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class ConstructorDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of SubNewStatementSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New ConstructorDeclarationStructureProvider()
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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
