// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeCleanup
{
    public class RemoveUnnecessaryLineContinuationTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonTrivia()
        {
            var code = @"[|
        ::: Console.WriteLine("")|]";

            var expected = @"
        Console.WriteLine("")";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonTrivia_EndOfLine()
        {
            var code = @"[|
        ::: 

        Console.WriteLine("")|]";

            var expected = @"


        Console.WriteLine("")";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonTrivia_LineContinuation()
        {
            var code = @"[|
        ::: _
        _
        _
        Console.WriteLine("")|]";

            var expected = @"



        Console.WriteLine("")";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonTrivia_LineContinuation2()
        {
            var code = @"[|
        ::: 
        _
        _
        Console.WriteLine("")|]";

            var expected = @"



        Console.WriteLine("")";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonTrivia_LineContinuation3()
        {
            var code = @"[|
        ::: 
        _
        
        Console.WriteLine("")|]";

            var expected = @"



        Console.WriteLine("")";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonTrivia_LineContinuation_Comment()
        {
            var code = @"[|
        ::: 
        _
        ' test
        Console.WriteLine("")|]";

            var expected = @"

 _
        ' test
        Console.WriteLine("")";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LineContinuation()
        {
            var code = @"[|
        Console.WriteLine("""") _

        Console.WriteLine("""")|]";

            var expected = @"
        Console.WriteLine("""")

        Console.WriteLine("""")";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LineContinuation_MultipleLines()
        {
            var code = @"[|
        Console.WriteLine("""") _
        _
        _
        Console.WriteLine("""")|]";

            var expected = @"
        Console.WriteLine("""") _
        _
        _
        Console.WriteLine("""")";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LineContinuation_MultipleLines2()
        {
            var code = @"[|
        Console.WriteLine("""") _
        _
        _

        Console.WriteLine("""")|]";

            var expected = @"
        Console.WriteLine("""")



        Console.WriteLine("""")";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LineContinuation_Invalid()
        {
            var code = @"[|
         Console.WriteLine() _             _ 
        ' test 
        : ' test
        _
        Console.WriteLine()|]";

            var expected = @"
        Console.WriteLine() _             _ 
        ' test 
         ' test
        _
        Console.WriteLine()";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonToken_SingleLine()
        {
            var code = @"[|
         Console.WriteLine() : Console.WriteLine()|]";

            var expected = @"
        Console.WriteLine() : Console.WriteLine()";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonToken_SingleLine_MultipleColon()
        {
            var code = @"[|
         Console.WriteLine() :::: Console.WriteLine()|]";

            var expected = @"
        Console.WriteLine() : Console.WriteLine()";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonToken_SingleLine_SkippedTokens()
        {
            var code = @"[|
         Console.WriteLine() _ : Console.WriteLine()|]";

            var expected = @"
        Console.WriteLine() _ : Console.WriteLine()";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonToken_LineContinuation_BeforeColonToken()
        {
            var code = @"[|
         Console.WriteLine() _ 
         : Console.WriteLine()|]";

            var expected = @"
        Console.WriteLine()
        Console.WriteLine()";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonToken_LineContinuation_BeforeColonToken2()
        {
            var code = @"[|
         Console.WriteLine() _  _
         : Console.WriteLine()|]";

            var expected = @"
        Console.WriteLine() _  _
          Console.WriteLine()";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonToken_LineContinuation_Comment_BeforeColonToken()
        {
            var code = @"[|
         Console.WriteLine() _ ' test
         : Console.WriteLine()|]";

            var expected = @"
        Console.WriteLine() _ ' test
          Console.WriteLine()";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonToken_MultipleLine()
        {
            var code = @"[|
         Console.WriteLine() : 
         Console.WriteLine()|]";

            var expected = @"
        Console.WriteLine()
        Console.WriteLine()";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonToken_LineContinuation_AfterColonToken()
        {
            var code = @"[|
         Console.WriteLine() : _
         Console.WriteLine()|]";

            var expected = @"
        Console.WriteLine()
        Console.WriteLine()";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonToken_LineContinuation_AfterColonToken2()
        {
            var code = @"[|
         Console.WriteLine() : _
         _
         Console.WriteLine()|]";

            var expected = @"
        Console.WriteLine()

        Console.WriteLine()";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonToken_LineContinuation_AfterColonToken_MultipleLine()
        {
            var code = @"[|
         Console.WriteLine() : _
         _
         _|]";

            var expected = @"
        Console.WriteLine()

";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonToken_LineContinuation_AfterColonToken_Mixed()
        {
            var code = @"[|
         Console.WriteLine() : _
         _
         :
         _
         Console.WriteLine()|]";

            var expected = @"
        Console.WriteLine()



        Console.WriteLine()";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonToken_LineContinuation_AfterColonToken_Colon_Comment()
        {
            var code = @"[|
         Console.WriteLine() : _
         _
         : ' test
         _
         Console.WriteLine()|]";

            var expected = @"
        Console.WriteLine() _
 _
        ' test
 _
        Console.WriteLine()";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonToken_LineContinuation_Mix()
        {
            var code = @"[|
         Console.WriteLine() _ : _
         _
         : ' test
         _
         Console.WriteLine()|]";

            var expected = @"
        Console.WriteLine() _  _
         _
          ' test
         _
         Console.WriteLine()";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonToken_If()
        {
            var code = @"[|
        If True Then :
        End If|]";

            var expected = @"
        If True Then
        End If";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ImplicitLineContinuation()
        {
            var code = @"[|
        Dim i = _
                1 + _
                2|]";

            var expected = @"
        Dim i =
                1 +
                2";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ImplicitLineContinuation_Multiple()
        {
            var code = @"[|
        Dim i = _
                _
                1 + _
                2|]";

            var expected = @"
        Dim i = _
 _
                1 +
                2";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LineContinuation_Mix()
        {
            var code = @"[|Class _
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
    Class|]";

            var expected = @"Class _
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
    Class";

            Verify(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ImplicitLineContinuation_Invalid()
        {
            var code = @"[|
        Dim i = _ _
                _ _
                1 + _ _
                2|]";

            var expected = @"
        Dim i = _ _
                _ _
                1 + _ _
                2";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [WorkItem(544470, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void AttributeTargetColon()
        {
            var code = @"[|<Assembly: _
CLSCompliant>|]";

            var expected = @"<Assembly: _
CLSCompliant>";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(529428, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LineContinuationInImport()
        {
            var code = @"[|Imports System _

|]";

            var expected = @"Imports System

";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(529425, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonInOption()
        {
            var code = @"[|Option Infer On :: Option Explicit Off|]";

            var expected = @"Option Infer On : Option Explicit Off";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(544524, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LineContinuationInNamedFieldInitializer()
        {
            var code = @"[|Class C
    Sub S()
        Dim o = New With
            {
                . _
                a = 2
            }
    End Sub
End Class|]";

            var expected = @"Class C
    Sub S()
        Dim o = New With
            {
                . _
                a = 2
            }
    End Sub
End Class";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(544523, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void IfPart_Colon1()
        {
            var code = @"[|Module M
    Sub S()
        If True Then
            : Return : End If
    End Sub
End Module|]";

            var expected = @"Module M
    Sub S()
        If True Then
            Return : End If
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(544523, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void IfPart_Colon2()
        {
            var code = @"[|Module M
    Sub S()
        If True Then : 
            Return : End If
    End Sub
End Module|]";

            var expected = @"Module M
    Sub S()
        If True Then
            Return : End If
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(544523, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void IfPart_Colon3()
        {
            var code = @"[|Module M
    Sub S()
        If True Then : Return
        : End If
    End Sub
End Module|]";

            var expected = @"Module M
    Sub S()
        If True Then : Return
        End If
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(544523, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void IfPart_Colon4()
        {
            var code = @"[|Module M
    Sub S()
        If True Then : Return : 
        End If
    End Sub
End Module|]";

            var expected = @"Module M
    Sub S()
        If True Then : Return
        End If
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(544521, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LabelColon()
        {
            var code = @"[|Module Program
    Sub S()
        L: 
    End Sub
End Module|]";

            var expected = @"Module Program
    Sub S()
L:
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(544521, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LabelColon_ColonTrivia()
        {
            var code = @"[|Module Program
    Sub S()
        L:::::::::  
    End Sub
End Module|]";

            var expected = @"Module Program
    Sub S()
L:
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(544520, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LineContinuation_MixedWithImplicitLineContinuation()
        {
            var code = @"[|Module Program
    Sub Main(
 _
        args _
        As String)
    End Sub
End Module|]";

            var expected = @"Module Program
    Sub Main(
 _
        args _
        As String)
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(544549, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonTrivia_EndOfFile()
        {
            var code = @"[|:::::::
|]";

            var expected = @"
";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(545538, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void ColonTriviaBeforeCommentTrivia()
        {
            var code = @"[|Module M
    Sub Main()
        Dim b = <x/>.@x : '
    End Sub
End Module|]";

            var expected = @"Module M
    Sub Main()
        Dim b = <x/>.@x  '
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(545540, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void InsideWithStatementWithMemberCall()
        {
            var code = @"[|Module Program
    Sub Main()
        With ""
            Dim y = From x In "" Distinct
            : .ToLower()
        End With
    End Sub
End Module|]";

            var expected = @"Module Program
    Sub Main()
        With ""
            Dim y = From x In "" Distinct
            : .ToLower()
        End With
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(545540, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void InsideWithStatementWithMemberCall2()
        {
            var code = @"[|Module Program
    Sub Main()
        With ""
            Dim y = From x In """" Distinct :
            .ToLower()
        End With
    End Sub
End Module|]";

            var expected = @"Module Program
    Sub Main()
        With ""
            Dim y = From x In """" Distinct :
            .ToLower()
        End With
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(545540, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void InsideWithStatementWithMemberCall3()
        {
            var code = @"[|Module Program
    Sub Main()
        With ""
            .ToLower()
            : .ToLower()
        End With
    End Sub
End Module|]";

            var expected = @"Module Program
    Sub Main()
        With ""
            .ToLower()
            : .ToLower()
        End With
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(545540, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void InsideWithStatementWithMemberCall4()
        {
            var code = @"[|Module Program
    Sub Main()
        With """"
            .ToLower() :
            .ToLower()
        End With
    End Sub
End Module|]";

            var expected = @"Module Program
    Sub Main()
        With """"
            .ToLower()
            .ToLower()
        End With
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(607791, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void InsideWithStatementWithDictionaryAccess()
        {
            var code = @"[|Imports System.Collections
Module Program
    Sub Main()
        With New Hashtable
            Dim x = From c In """" Distinct
            :!A = !B
        End With
    End Sub
End Module
|]";

            var expected = @"Imports System.Collections
Module Program
    Sub Main()
        With New Hashtable
            Dim x = From c In """" Distinct
            : !A = !B
        End With
    End Sub
End Module
";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(607791, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void InsideWithStatementWithDictionaryAccess2()
        {
            var code = @"[|Imports System.Collections
Module Program
    Sub Main()
        With New Hashtable
            Dim x = From c In """" Distinct :
              !A = !B
        End With
    End Sub
End Module|]";

            var expected = @"Imports System.Collections
Module Program
    Sub Main()
        With New Hashtable
            Dim x = From c In """" Distinct :
            !A = !B
        End With
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(529821, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void InsideObjectInitializer()
        {
            var code = @"[|Imports System.Runtime.CompilerServices
 
Module Program
    Sub Main()
        Dim s = New StrongBox(Of Object) With {
        .Value = Sub()
                     Dim y = From x In "" Distinct
                     : .Value.ToString()
                 End Sub}
    End Sub
End Module|]";

            var expected = @"Imports System.Runtime.CompilerServices

Module Program
    Sub Main()
        Dim s = New StrongBox(Of Object) With {
        .Value = Sub()
                     Dim y = From x In "" Distinct
                     : .Value.ToString()
                 End Sub}
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(545545, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LineContinuationBetweenXmlAndDot()
        {
            var code = @"[|Module Program
    Sub Main()
        Dim y = <?xml version=""1.0""?><root/> _
        .ToString()
    End Sub
End Module|]";

            var expected = @"Module Program
    Sub Main()
        Dim y = <?xml version=""1.0""?><root/> _
        .ToString()
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(545545, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LineContinuationBetweenXmlAndDot1()
        {
            var code = @"[|Module Program
    Sub Main()
        Dim x = <x/>.. _
            .<x>
    End Sub
End Module|]";

            var expected = @"Module Program
    Sub Main()
        Dim x = <x/>.. _
            .<x>
    End Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(545565, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LineContinuationBeforeFromQueryExpression()
        {
            var code = @"[|Class C
    Sub Main()
        Call _
        From x In """" Distinct.ToString()
    End Sub
End Class|]";

            var expected = @"Class C
    Sub Main()
        Call _
        From x In """" Distinct.ToString()
    End Sub
End Class";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(545565, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LineContinuationBeforeFromAggregateExpression()
        {
            var code = @"[|Class C
    Sub Main()
        Call _
            Aggregate x In {1} Into Count().ToString()
    End Sub
End Class|]";

            var expected = @"Class C
    Sub Main()
        Call _
            Aggregate x In {1} Into Count().ToString()
    End Sub
End Class";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(530635, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LineContinuationAtEndOfLambdaExpression1()
        {
            var code = @"[|Interface I
    Property A As Action
End Interface
 
Class C
    Implements I
    Property A As Action = Sub() Return _
    Implements I.A
End Class|]";

            var expected = @"Interface I
    Property A As Action
End Interface

Class C
    Implements I
    Property A As Action = Sub() Return _
    Implements I.A
End Class";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(530635, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LineContinuationAtEndOfLambdaExpression2()
        {
            var code = @"[|Interface I
    Property A As Action
End Interface
 
Class C
    Implements I
    Property A As Action = Sub()
                               Return
                           End Sub _
    Implements I.A
End Class|]";

            var expected = @"Interface I
    Property A As Action
End Interface

Class C
    Implements I
    Property A As Action = Sub()
                               Return
                           End Sub _
    Implements I.A
End Class";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(546798, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void LineContinuationAfterDot()
        {
            var code = @"[|
        System.Diagnostics. _
            Debug.Assert(True)|]";

            var expected = @"
        System.Diagnostics.
            Debug.Assert(True)";

            Verify(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [WorkItem(530621, "DevDiv")]
        [WorkItem(631933, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void DontRemoveLineContinuationAfterColonInSingleLineIfStatement()
        {
            var code = @"[|Module Program
    Dim x = Sub() If True Then Dim y : _
                               Exit Sub
End Module|]";

            var expected = @"Module Program
    Dim x = Sub() If True Then Dim y : _
                               Exit Sub
End Module";

            Verify(code, expected);
        }

        [Fact]
        [WorkItem(609481, "DevDiv")]
        [WorkItem(631933, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void DontRemoveLineContinuationInSingleLineIfStatement()
        {
            var code = @"[|
Module Program
    Sub Main()
        ' Single Line If with explicit line continuations
        If True Then _
            Return _
        Else _
        Return
    End Sub
End Module
|]";

            var expected = @"
Module Program
    Sub Main()
        ' Single Line If with explicit line continuations
        If True Then _
            Return _
        Else _
        Return
    End Sub
End Module
";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(609481, "DevDiv")]
        [WorkItem(631933, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void DontRemoveLineContinuationInNestedSingleLineIfStatement()
        {
            var code = @"[|
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
|]";

            var expected = @"
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
";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(710, "#710")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void DontRemoveLineContinuationInStringInterpolation1()
        {
            var code = @"[|
Module Program
    Dim x = $""{ _
            1}""
End Module
|]";

            var expected = @"
Module Program
    Dim x = $""{ _
            1}""
End Module
";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(710, "#710")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void DontRemoveLineContinuationInStringInterpolation2()
        {
            var code = @"[|
Module Program
    Dim x = $""{1 _
               }""
End Module
|]";

            var expected = @"
Module Program
    Dim x = $""{1 _
               }""
End Module
";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(710, "#710")]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void DontRemoveLineContinuationInStringInterpolation3()
        {
            var code = @"[|
Module Program
    Dim x = $""{ _

1 _

}""
End Module
|]";

            var expected = @"
Module Program
    Dim x = $""{ _

1 _

}""
End Module
";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(1085887)]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void DontRemoveLineContinuationInVisualBasic9()
        {
            var code = @"[|
Module Program
    Function Add( _
        i As Integer, _
        j As Integer, _
    ) As Integer

        Return i + j
    End Function
End Module
|]";

            var expected = @"
Module Program
    Function Add( _
        i As Integer, _
        j As Integer, _
    ) As Integer

        Return i + j
    End Function
End Module
";
            Verify(code, expected, langVersion: LanguageVersion.VisualBasic9);
        }

        [Fact]
        [WorkItem(1085887)]
        [Trait(Traits.Feature, Traits.Features.RemoveUnnecessaryLineContinuation)]
        public void RemoveLineContinuationInVisualBasic10_11_12_And_14()
        {
            var code = @"[|
Module Program
    Function Add( _
        i As Integer, _
        j As Integer, _
    ) As Integer

        Return i + j
    End Function
End Module
|]";

            var expected = @"
Module Program
    Function Add(
        i As Integer,
        j As Integer,
    ) As Integer

        Return i + j
    End Function
End Module
";

            Verify(code, expected, langVersion: LanguageVersion.VisualBasic10);
            Verify(code, expected, langVersion: LanguageVersion.VisualBasic11);
            Verify(code, expected, langVersion: LanguageVersion.VisualBasic12);
            Verify(code, expected);
        }

        private string CreateMethod(string body)
        {
            return @"Imports System
Class C
    Public Sub Method()" + body + @"
    End Sub
End Class";
        }

        private void Verify(string codeWithMarker, string expectedResult, LanguageVersion langVersion = LanguageVersion.VisualBasic14)
        {
            var codeWithoutMarker = default(string);
            var textSpans = (IList<TextSpan>)new List<TextSpan>();
            MarkupTestFile.GetSpans(codeWithMarker, out codeWithoutMarker, out textSpans);

            var document = CreateDocument(codeWithoutMarker, LanguageNames.VisualBasic, langVersion);
            var codeCleanups = CodeCleaner.GetDefaultProviders(document).Where(p => p.Name == PredefinedCodeCleanupProviderNames.RemoveUnnecessaryLineContinuation || p.Name == PredefinedCodeCleanupProviderNames.Format);

            var cleanDocument = CodeCleaner.CleanupAsync(document, textSpans[0], codeCleanups).Result;

            Assert.Equal(expectedResult, cleanDocument.GetSyntaxRootAsync().Result.ToFullString());
        }

        private static Document CreateDocument(string code, string language, LanguageVersion langVersion)
        {
            var solution = new AdhocWorkspace().CurrentSolution;
            var projectId = ProjectId.CreateNewId();
            var project = solution
                .AddProject(projectId, "Project", "Project.dll", language)
                .GetProject(projectId);

            var parseOptions = (VisualBasicParseOptions)project.ParseOptions;
            parseOptions = parseOptions.WithLanguageVersion(langVersion);
            project = project.WithParseOptions(parseOptions);

            return project.AddDocument("Document", SourceText.From(code));
        }
    }
}
