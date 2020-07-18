' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    Public Class CastWithCommentSimplificationTests
        Inherits AbstractSimplificationTests
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_PreserveComment_Remove_redefinedCast() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        Dim b As Integer = _ ' Comment before predefined cast
        {|Simplify:CByte( _ ' Open Param trailing comment on predefined cast
 _ ' Leading on Expression
        0 _ ' Trailing on Expression
        )|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Sub M()
        Dim b As Integer = _ ' Comment before predefined cast
        _ ' Open Param trailing comment on predefined cast
 _ ' Leading on Expression
        0 _ ' Trailing on Expression
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_PreserveComment_Remove_Cast() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System

Class Program
    Private Shared Sub Main()
        Dim a As Action(Of Object) = AddressOf Console.WriteLine
        Dim b As Action(Of String) = {|Simplify:DirectCast( _ ' Open Param trailing on cast
 _ ' Leading on Expression
            a _ ' Trailing on Expression 
 _ ' Leading on Comma
            , ' Trailing on Comma
 _ ' Leading on Type
            Action (Of String) _ ' Trailing on Type
 _ ' Leading on )
        )|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System

Class Program
    Private Shared Sub Main()
        Dim a As Action(Of Object) = AddressOf Console.WriteLine
        Dim b As Action(Of String) = _ ' Open Param trailing on cast
 _ ' Leading on Expression
            a _ ' Trailing on Expression 
 _ ' Leading on Comma
            ' Trailing on Comma
 _ ' Leading on Type
 _          ' Trailing on Type
 _ ' Leading on )
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function
    End Class

End Namespace
