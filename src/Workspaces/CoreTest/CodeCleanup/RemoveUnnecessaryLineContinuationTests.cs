// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeCleanup;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
public sealed class RemoveUnnecessaryLineContinuationTests
{
    [Fact]
    public Task ColonTrivia()
        => VerifyAsync(CreateMethod("""
            [|
                    ::: Console.WriteLine(")|]
            """), CreateMethod("""

                                                Console.WriteLine(")
                                        """));

    [Fact]
    public Task ColonTrivia_EndOfLine()
        => VerifyAsync(CreateMethod("""
            [|
                    ::: 

                    Console.WriteLine(")|]
            """), CreateMethod("""



                                                Console.WriteLine(")
                                        """));

    [Fact]
    public Task ColonTrivia_LineContinuation()
        => VerifyAsync(CreateMethod("""
            [|
                    ::: _
                    _
                    _
                    Console.WriteLine(")|]
            """), CreateMethod("""




                                                Console.WriteLine(")
                                        """));

    [Fact]
    public Task ColonTrivia_LineContinuation2()
        => VerifyAsync(CreateMethod("""
            [|
                    ::: 
                    _
                    _
                    Console.WriteLine(")|]
            """), CreateMethod("""




                                                Console.WriteLine(")
                                        """));

    [Fact]
    public Task ColonTrivia_LineContinuation3()
        => VerifyAsync(CreateMethod("""
            [|
                    ::: 
                    _
                    
                    Console.WriteLine(")|]
            """), CreateMethod("""




                                                Console.WriteLine(")
                                        """));

    [Fact]
    public Task ColonTrivia_LineContinuation_Comment()
        => VerifyAsync(CreateMethod("""
            [|
                    ::: 
                    _
                    ' test
                    Console.WriteLine(")|]
            """), CreateMethod("""


                                                               _
                                                ' test
                                                Console.WriteLine(")
                                        """));

    [Fact]
    public Task LineContinuation()
        => VerifyAsync(CreateMethod("""
            [|
                    Console.WriteLine("") _

                    Console.WriteLine("")|]
            """), CreateMethod("""

                                                Console.WriteLine("")

                                                Console.WriteLine("")
                                        """));

    [Fact]
    public Task LineContinuation_MultipleLines()
        => VerifyAsync(CreateMethod("""
            [|
                    Console.WriteLine("") _
                    _
                    _
                    Console.WriteLine("")|]
            """), CreateMethod("""

                                                Console.WriteLine("") _
                                                _
                                                _
                                                Console.WriteLine("")
                                        """));

    [Fact]
    public Task LineContinuation_MultipleLines2()
        => VerifyAsync(CreateMethod("""
            [|
                    Console.WriteLine("") _
                    _
                    _

                    Console.WriteLine("")|]
            """), CreateMethod("""

                                                Console.WriteLine("")



                                                Console.WriteLine("")
                                        """));

    [Fact]
    public Task LineContinuation_Invalid()
        => VerifyAsync(CreateMethod("""
            [|
                     Console.WriteLine() _             _ 
                    ' test 
                    : ' test
                    _
                    Console.WriteLine()|]
            """), CreateMethod("""

                                                Console.WriteLine() _             _ 
                                                ' test 
                                                 ' test
                                                _
                                                Console.WriteLine()
                                        """));

    [Fact]
    public Task ColonToken_SingleLine()
        => VerifyAsync(CreateMethod("""
            [|
                     Console.WriteLine() : Console.WriteLine()|]
            """), CreateMethod("""

                                                Console.WriteLine() : Console.WriteLine()
                                        """));

    [Fact]
    public Task ColonToken_SingleLine_MultipleColon()
        => VerifyAsync(CreateMethod("""
            [|
                     Console.WriteLine() :::: Console.WriteLine()|]
            """), CreateMethod("""

                                                Console.WriteLine() : Console.WriteLine()
                                        """));

    [Fact]
    public Task ColonToken_SingleLine_SkippedTokens()
        => VerifyAsync(CreateMethod("""
            [|
                     Console.WriteLine() _ : Console.WriteLine()|]
            """), CreateMethod("""

                                                Console.WriteLine() _ : Console.WriteLine()
                                        """));

    [Fact]
    public Task ColonToken_LineContinuation_BeforeColonToken()
        => VerifyAsync(CreateMethod("""
            [|
                     Console.WriteLine() _ 
                     : Console.WriteLine()|]
            """), CreateMethod("""

                                                Console.WriteLine()
                                                Console.WriteLine()
                                        """));

    [Fact]
    public Task ColonToken_LineContinuation_BeforeColonToken2()
        => VerifyAsync(CreateMethod("""
            [|
                     Console.WriteLine() _  _
                     : Console.WriteLine()|]
            """), CreateMethod("""

                                                Console.WriteLine() _  _
                                                  Console.WriteLine()
                                        """));

    [Fact]
    public Task ColonToken_LineContinuation_Comment_BeforeColonToken()
        => VerifyAsync(CreateMethod("""
            [|
                     Console.WriteLine() _ ' test
                     : Console.WriteLine()|]
            """), CreateMethod("""

                                                Console.WriteLine() _ ' test
                                                Console.WriteLine()
                                        """), LanguageVersion.VisualBasic15);

    [Fact]
    public Task ColonToken_LineContinuation_Comment_BeforeColonTokenV16()
        => VerifyAsync(CreateMethod("""
            [|
                     Console.WriteLine() _ ' test
                     : Console.WriteLine()|]
            """), CreateMethod("""

                                                Console.WriteLine() _ ' test
                                                Console.WriteLine()
                                        """), LanguageVersion.VisualBasic16);

    [Fact]
    public Task ColonToken_MultipleLine()
        => VerifyAsync(CreateMethod("""
            [|
                     Console.WriteLine() : 
                     Console.WriteLine()|]
            """), CreateMethod("""

                                                Console.WriteLine()
                                                Console.WriteLine()
                                        """));

    [Fact]
    public Task ColonToken_LineContinuation_AfterColonToken()
        => VerifyAsync(CreateMethod("""
            [|
                     Console.WriteLine() : _
                     Console.WriteLine()|]
            """), CreateMethod("""

                                                Console.WriteLine()
                                                Console.WriteLine()
                                        """));

    [Fact]
    public Task ColonToken_LineContinuation_AfterColonToken2()
        => VerifyAsync(CreateMethod("""
            [|
                     Console.WriteLine() : _
                     _
                     Console.WriteLine()|]
            """), CreateMethod("""

                                                Console.WriteLine()

                                                Console.WriteLine()
                                        """));

    [Fact]
    public Task ColonToken_LineContinuation_AfterColonToken_MultipleLine()
        => VerifyAsync(CreateMethod("""
            [|
                     Console.WriteLine() : _
                     _
                     _|]
            """), CreateMethod("""

                                                Console.WriteLine()


                                        """));

    [Fact]
    public Task ColonToken_LineContinuation_AfterColonToken_Mixed()
        => VerifyAsync(CreateMethod("""
            [|
                     Console.WriteLine() : _
                     _
                     :
                     _
                     Console.WriteLine()|]
            """), CreateMethod("""

                                                Console.WriteLine()



                                                Console.WriteLine()
                                        """));

    [Fact]
    public Task ColonToken_LineContinuation_AfterColonToken_Colon_Comment()
        => VerifyAsync(CreateMethod("""
            [|
                     Console.WriteLine() : _
                     _
                     : ' test
                     _
                     Console.WriteLine()|]
            """), CreateMethod("""

                                                Console.WriteLine() _
                                                                    _
                                                ' test
                                                _
                                                Console.WriteLine()
                                        """));

    [Fact]
    public Task ColonToken_LineContinuation_Mix()
        => VerifyAsync(CreateMethod("""
            [|
                     Console.WriteLine() _ : _
                     _
                     : ' test
                     _
                     Console.WriteLine()|]
            """), CreateMethod("""

                                                Console.WriteLine() _  _
                                                 _
                                                  ' test
                                                 _
                                                 Console.WriteLine()
                                        """));

    [Fact]
    public Task ColonToken_If()
        => VerifyAsync(CreateMethod("""
            [|
                    If True Then :
                    End If|]
            """), CreateMethod("""

                                                If True Then
                                                End If
                                        """));

    [Fact]
    public Task ImplicitLineContinuation()
        => VerifyAsync(CreateMethod("""
            [|
                    Dim i = _
                            1 + _
                            2|]
            """), CreateMethod("""

                                                Dim i =
                                                        1 +
                                                        2
                                        """));

    [Fact]
    public Task ImplicitLineContinuation_Multiple()
        => VerifyAsync(CreateMethod("""
            [|
                    Dim i = _
                            _
                            1 + _
                            2|]
            """), CreateMethod("""

                                                Dim i = _
                                                        _
                                                        1 +
                                                        2
                                        """));

    [Fact]
    public Task LineContinuation_Mix()
        => VerifyAsync("""
            [|Class _
             A
                Inherits _
                    System _
                    . _
                    Object

                Public _
                    Function _
                        Method _
                            ( _
                                i _
                                    As _
                                        Integer _
                                        , _
                                i2 _
                                    As _
                                        String _
                            ) _
                            As _
                            Integer

                    If _
                        i _
                            + _
                                i2 _
                                . _
                                Length _
                                    = _
                                        1 _
                                            Then
                        Console _
                            . _
                                WriteLine _
                                ( _
                                    vbCrLf _
                                )
                    End _
                        If

                    Return _
                        1
                End _
                    Function
            End _
                Class|]
            """, """
            Class _
             A
                Inherits _
                    System _
                    .
                    Object

                Public _
                    Function _
                        Method _
                            (
                                i _
                                    As _
                                        Integer _
                                        ,
                                i2 _
                                    As _
                                        String
                            ) _
                            As _
                            Integer

                    If _
                        i _
                            +
                                i2 _
                                .
                                Length _
                                    =
                                        1 _
                                            Then
                        Console _
                            .
                                WriteLine _
                                (
                                    vbCrLf
                                )
                    End _
                        If

                    Return _
                        1
                End _
                    Function
            End _
                Class
            """);

    [Fact]
    public Task ImplicitLineContinuation_Invalid()
        => VerifyAsync(CreateMethod("""
            [|
                    Dim i = _ _
                            _ _
                            1 + _ _
                            2|]
            """), CreateMethod("""

                                                Dim i = _ _
                                                        _ _
                                                        1 + _ _
                                                        2
                                        """));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544470")]
    public Task AttributeTargetColon()
        => VerifyAsync("""
            [|<Assembly: _
            CLSCompliant>|]
            """, """
            <Assembly: _
            CLSCompliant>
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529428")]
    public Task LineContinuationInImport()
        => VerifyAsync("""
            [|Imports System _

            |]
            """, """
            Imports System


            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529425")]
    public Task ColonInOption()
        => VerifyAsync(@"[|Option Infer On :: Option Explicit Off|]", @"Option Infer On : Option Explicit Off");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544524")]
    public Task LineContinuationInNamedFieldInitializer()
        => VerifyAsync("""
            [|Class C
                Sub S()
                    Dim o = New With
                        {
                            . _
                            a = 2
                        }
                End Sub
            End Class|]
            """, """
            Class C
                Sub S()
                    Dim o = New With
                        {
                            . _
                            a = 2
                        }
                End Sub
            End Class
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544523")]
    public Task IfPart_Colon1()
        => VerifyAsync("""
            [|Module M
                Sub S()
                    If True Then
                        : Return : End If
                End Sub
            End Module|]
            """, """
            Module M
                Sub S()
                    If True Then
                        Return : End If
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544523")]
    public Task IfPart_Colon2()
        => VerifyAsync("""
            [|Module M
                Sub S()
                    If True Then : 
                        Return : End If
                End Sub
            End Module|]
            """, """
            Module M
                Sub S()
                    If True Then
                        Return : End If
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544523")]
    public Task IfPart_Colon3()
        => VerifyAsync("""
            [|Module M
                Sub S()
                    If True Then : Return
                    : End If
                End Sub
            End Module|]
            """, """
            Module M
                Sub S()
                    If True Then : Return
                    End If
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544523")]
    public Task IfPart_Colon4()
        => VerifyAsync("""
            [|Module M
                Sub S()
                    If True Then : Return : 
                    End If
                End Sub
            End Module|]
            """, """
            Module M
                Sub S()
                    If True Then : Return
                    End If
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544521")]
    public Task LabelColon()
        => VerifyAsync("""
            [|Module Program
                Sub S()
                    L: 
                End Sub
            End Module|]
            """, """
            Module Program
                Sub S()
            L:
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544521")]
    public Task LabelColon_ColonTrivia()
        => VerifyAsync("""
            [|Module Program
                Sub S()
                    L:::::::::  
                End Sub
            End Module|]
            """, """
            Module Program
                Sub S()
            L:
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544520")]
    public Task LineContinuation_MixedWithImplicitLineContinuation()
        => VerifyAsync("""
            [|Module Program
                Sub Main(
             _
                    args _
                    As String)
                End Sub
            End Module|]
            """, """
            Module Program
                Sub Main(
                         _
                    args _
                    As String)
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544549")]
    public Task ColonTrivia_EndOfFile()
        => VerifyAsync("""
            [|:::::::
            |]
            """, """


            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545538")]
    public Task ColonTriviaBeforeCommentTrivia()
        => VerifyAsync("""
            [|Module M
                Sub Main()
                    Dim b = <x/>.@x : '
                End Sub
            End Module|]
            """, """
            Module M
                Sub Main()
                    Dim b = <x/>.@x  '
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545540")]
    public Task InsideWithStatementWithMemberCall()
        => VerifyAsync("""
            [|Module Program
                Sub Main()
                    With "
                        Dim y = From x In " Distinct
                        : .ToLower()
                    End With
                End Sub
            End Module|]
            """, """
            Module Program
                Sub Main()
                    With "
                        Dim y = From x In " Distinct
                        : .ToLower()
                    End With
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545540")]
    public Task InsideWithStatementWithMemberCall2()
        => VerifyAsync("""
            [|Module Program
                Sub Main()
                    With "
                        Dim y = From x In "" Distinct :
                        .ToLower()
                    End With
                End Sub
            End Module|]
            """, """
            Module Program
                Sub Main()
                    With "
                        Dim y = From x In "" Distinct :
                        .ToLower()
                    End With
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545540")]
    public Task InsideWithStatementWithMemberCall3()
        => VerifyAsync("""
            [|Module Program
                Sub Main()
                    With "
                        .ToLower()
                        : .ToLower()
                    End With
                End Sub
            End Module|]
            """, """
            Module Program
                Sub Main()
                    With "
                        .ToLower()
                        : .ToLower()
                    End With
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545540")]
    public Task InsideWithStatementWithMemberCall4()
        => VerifyAsync("""
            [|Module Program
                Sub Main()
                    With ""
                        .ToLower() :
                        .ToLower()
                    End With
                End Sub
            End Module|]
            """, """
            Module Program
                Sub Main()
                    With ""
                        .ToLower()
                        .ToLower()
                    End With
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607791")]
    public Task InsideWithStatementWithDictionaryAccess()
        => VerifyAsync("""
            [|Imports System.Collections
            Module Program
                Sub Main()
                    With New Hashtable
                        Dim x = From c In "" Distinct
                        :!A = !B
                    End With
                End Sub
            End Module
            |]
            """, """
            Imports System.Collections
            Module Program
                Sub Main()
                    With New Hashtable
                        Dim x = From c In "" Distinct
                        : !A = !B
                    End With
                End Sub
            End Module

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607791")]
    public Task InsideWithStatementWithDictionaryAccess2()
        => VerifyAsync("""
            [|Imports System.Collections
            Module Program
                Sub Main()
                    With New Hashtable
                        Dim x = From c In "" Distinct :
                          !A = !B
                    End With
                End Sub
            End Module|]
            """, """
            Imports System.Collections
            Module Program
                Sub Main()
                    With New Hashtable
                        Dim x = From c In "" Distinct :
                        !A = !B
                    End With
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529821")]
    public Task InsideObjectInitializer()
        => VerifyAsync("""
            [|Imports System.Runtime.CompilerServices
             
            Module Program
                Sub Main()
                    Dim s = New StrongBox(Of Object) With {
                    .Value = Sub()
                                 Dim y = From x In " Distinct
                                 : .Value.ToString()
                             End Sub}
                End Sub
            End Module|]
            """, """
            Imports System.Runtime.CompilerServices

            Module Program
                Sub Main()
                    Dim s = New StrongBox(Of Object) With {
                    .Value = Sub()
                                 Dim y = From x In " Distinct
                                 : .Value.ToString()
                             End Sub}
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545545")]
    public Task LineContinuationBetweenXmlAndDot()
        => VerifyAsync("""
            [|Module Program
                Sub Main()
                    Dim y = <?xml version="1.0"?><root/> _
                    .ToString()
                End Sub
            End Module|]
            """, """
            Module Program
                Sub Main()
                    Dim y = <?xml version="1.0"?><root/> _
                    .ToString()
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545545")]
    public Task LineContinuationBetweenXmlAndDot1()
        => VerifyAsync("""
            [|Module Program
                Sub Main()
                    Dim x = <x/>.. _
                        .<x>
                End Sub
            End Module|]
            """, """
            Module Program
                Sub Main()
                    Dim x = <x/>.. _
                        .<x>
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545565")]
    public Task LineContinuationBeforeFromQueryExpression()
        => VerifyAsync("""
            [|Class C
                Sub Main()
                    Call _
                    From x In "" Distinct.ToString()
                End Sub
            End Class|]
            """, """
            Class C
                Sub Main()
                    Call _
                    From x In "" Distinct.ToString()
                End Sub
            End Class
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545565")]
    public Task LineContinuationBeforeFromAggregateExpression()
        => VerifyAsync("""
            [|Class C
                Sub Main()
                    Call _
                        Aggregate x In {1} Into Count().ToString()
                End Sub
            End Class|]
            """, """
            Class C
                Sub Main()
                    Call _
                        Aggregate x In {1} Into Count().ToString()
                End Sub
            End Class
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530635")]
    public Task LineContinuationAtEndOfLambdaExpression1()
        => VerifyAsync("""
            [|Interface I
                Property A As Action
            End Interface
             
            Class C
                Implements I
                Property A As Action = Sub() Return _
                Implements I.A
            End Class|]
            """, """
            Interface I
                Property A As Action
            End Interface

            Class C
                Implements I
                Property A As Action = Sub() Return _
                Implements I.A
            End Class
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530635")]
    public Task LineContinuationAtEndOfLambdaExpression2()
        => VerifyAsync("""
            [|Interface I
                Property A As Action
            End Interface
             
            Class C
                Implements I
                Property A As Action = Sub()
                                           Return
                                       End Sub _
                Implements I.A
            End Class|]
            """, """
            Interface I
                Property A As Action
            End Interface

            Class C
                Implements I
                Property A As Action = Sub()
                                           Return
                                       End Sub _
                Implements I.A
            End Class
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546798")]
    public Task LineContinuationAfterDot()
        => VerifyAsync(CreateMethod("""
            [|
                    System.Diagnostics. _
                        Debug.Assert(True)|]
            """), CreateMethod("""

                                                System.Diagnostics.
                                                    Debug.Assert(True)
                                        """));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530621")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/631933")]
    public Task DoNotRemoveLineContinuationAfterColonInSingleLineIfStatement()
        => VerifyAsync("""
            [|Module Program
                Dim x = Sub() If True Then Dim y : _
                                           Exit Sub
            End Module|]
            """, """
            Module Program
                Dim x = Sub() If True Then Dim y : _
                                           Exit Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609481")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/631933")]
    public Task DoNotRemoveLineContinuationInSingleLineIfStatement()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main()
                    ' Single Line If with explicit line continuations
                    If True Then _
                        Return _
                    Else _
                    Return
                End Sub
            End Module
            |]
            """, """

            Module Program
                Sub Main()
                    ' Single Line If with explicit line continuations
                    If True Then _
                        Return _
                    Else _
                    Return
                End Sub
            End Module

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609481")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/631933")]
    public Task DoNotRemoveLineContinuationInNestedSingleLineIfStatement()
        => VerifyAsync("""
            [|
            Module Program
                Sub Main()
                    ' Nested Single Line If with explicit line continuations
                    If True Then _
                        If True Then _
                        Return _
                        Else _
                        Return _
                    Else _
                    Return

                    If True Then _
                        If True Then _
                        Return _
                        Else _
                        Return _
                    Else _
                        If True Then _
                        Return _
                        Else Return

                End Sub
            End Module
            |]
            """, """

            Module Program
                Sub Main()
                    ' Nested Single Line If with explicit line continuations
                    If True Then _
                        If True Then _
                        Return _
                        Else _
                        Return _
                    Else _
                    Return

                    If True Then _
                        If True Then _
                        Return _
                        Else _
                        Return _
                    Else _
                        If True Then _
                        Return _
                        Else Return

                End Sub
            End Module

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/710")]
    public Task DoNotRemoveLineContinuationInStringInterpolation1()
        => VerifyAsync("""
            [|
            Module Program
                Dim x = $"{ _
                        1}"
            End Module
            |]
            """, """

            Module Program
                Dim x = $"{ _
                        1}"
            End Module

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/710")]
    public Task DoNotRemoveLineContinuationInStringInterpolation2()
        => VerifyAsync("""
            [|
            Module Program
                Dim x = $"{1 _
                           }"
            End Module
            |]
            """, """

            Module Program
                Dim x = $"{1 _
                           }"
            End Module

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/710")]
    public Task DoNotRemoveLineContinuationInStringInterpolation3()
        => VerifyAsync("""
            [|
            Module Program
                Dim x = $"{ _

            1 _

            }"
            End Module
            |]
            """, """

            Module Program
                Dim x = $"{ _

            1 _

            }"
            End Module

            """);

    [Theory]
    [InlineData("_")]
    [InlineData("_ ' Comment")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69696")]
    public Task LineContinuationInString1(string continuation)
        => VerifyAsync($"""
            [|
            Module Program
                Dim x = "1" {continuation}
                        & "2" {continuation}
                        & "3"
            End Module
            |]
            """, $"""

            Module Program
                Dim x = "1" {continuation}
                        & "2" {continuation}
                        & "3"
            End Module

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69696")]
    public Task LineContinuationInString2()
        => VerifyAsync($"""
            [|
            Module Program
                Dim x = "1" & _
                        "2" & _
                        "3"
            End Module
            |]
            """, $"""

            Module Program
                Dim x = "1" &
                        "2" &
                        "3"
            End Module

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69696")]
    public Task LineContinuationInString3()
        => VerifyAsync($"""
            [|
            Module Program
                Dim x = "1" & ' Comment
                        "2" & ' Comment
                        "3"
            End Module
            |]
            """, $"""

            Module Program
                Dim x = "1" & ' Comment
                        "2" & ' Comment
                        "3"
            End Module

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1085887")]
    public Task DoNotRemoveLineContinuationInVisualBasic9()
        => VerifyAsync("""
            [|
            Module Program
                Function Add( _
                    i As Integer, _
                    j As Integer, _
                ) As Integer

                    Return i + j
                End Function
            End Module
            |]
            """, """

            Module Program
                Function Add( _
                    i As Integer, _
                    j As Integer, _
                ) As Integer

                    Return i + j
                End Function
            End Module

            """, langVersion: LanguageVersion.VisualBasic9);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1085887")]
    public async Task RemoveLineContinuationInVisualBasic10_11_12_And_14()
    {
        var code = """
            [|
            Module Program
                Function Add( _
                    i As Integer, _
                    j As Integer, _
                ) As Integer

                    Return i + j
                End Function
            End Module
            |]
            """;

        var expected = """

            Module Program
                Function Add(
                    i As Integer,
                    j As Integer,
                ) As Integer

                    Return i + j
                End Function
            End Module

            """;

        await VerifyAsync(code, expected, langVersion: LanguageVersion.VisualBasic10);
        await VerifyAsync(code, expected, langVersion: LanguageVersion.VisualBasic11);
        await VerifyAsync(code, expected, langVersion: LanguageVersion.VisualBasic12);
        await VerifyAsync(code, expected);
    }

    private static string CreateMethod(string body)
    {
        return """
            Imports System
            Class C
                Public Sub Method()
            """ + body + """

                End Sub
            End Class
            """;
    }

    private static async Task VerifyAsync(string codeWithMarker, string expectedResult, LanguageVersion langVersion = LanguageVersion.VisualBasic14)
    {
        MarkupTestFile.GetSpans(codeWithMarker, out var codeWithoutMarker, out var textSpans);

        var document = CreateDocument(codeWithoutMarker, LanguageNames.VisualBasic, langVersion);
        var codeCleanups = CodeCleaner.GetDefaultProviders(document).WhereAsArray(p => p.Name is PredefinedCodeCleanupProviderNames.RemoveUnnecessaryLineContinuation or PredefinedCodeCleanupProviderNames.Format);

        var cleanDocument = await CodeCleaner.CleanupAsync(document, textSpans[0], await document.GetCodeCleanupOptionsAsync(CancellationToken.None), codeCleanups);

        var actualResult = (await cleanDocument.GetRequiredSyntaxRootAsync(CancellationToken.None)).ToFullString();
        AssertEx.EqualOrDiff(expectedResult, actualResult);
    }

    private static Document CreateDocument(string code, string language, LanguageVersion langVersion)
    {
        var solution = new AdhocWorkspace().CurrentSolution;
        var projectId = ProjectId.CreateNewId();
        var project = solution
            .AddProject(projectId, "Project", "Project.dll", language)
            .GetRequiredProject(projectId);

        AssertEx.NotNull(project.ParseOptions);
        var parseOptions = (VisualBasicParseOptions)project.ParseOptions;
        parseOptions = parseOptions.WithLanguageVersion(langVersion);
        project = project.WithParseOptions(parseOptions);

        return project.AddDocument("Document", SourceText.From(code));
    }
}
