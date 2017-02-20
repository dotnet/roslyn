' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Global.Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Namespace Syntax.Normalizer

        Public Class SyntaxNormalizerTests

            <WorkItem(546397, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            <ConditionalFact(GetType(WindowsOnly))>
            Public Sub TestAllInVB()
                Dim allInVB As String = TestResource.AllInOneVisualBasicCode
                Dim expected As String = TestResource.AllInOneVisualBasicBaseline

                Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(allInVB)
                Dim actual = node.NormalizeWhitespace("    ").ToFullString()
                Assert.Equal(expected, actual)
            End Sub

            <Theory()>
            <InlineData("+1", "+1"), InlineData("+a", "+a"), InlineData("-a", "-a"), InlineData("a", "a"),
         InlineData("a+b", "a + b"), InlineData("a-b", "a - b"), InlineData("a*b", "a * b"), InlineData("a/b", "a / b"), InlineData("a mod b", "a mod b"),
         InlineData("a xor b", "a xor b"), InlineData("a or b", "a or b"), InlineData("a and b", "a and b"),
         InlineData("a orelse b", "a orelse b"), InlineData("a andalso b", "a andalso b"),
         InlineData("a<b", "a < b"), InlineData("a<=b", "a <= b"),
         InlineData("a>b", "a > b"), InlineData("a>=b", "a >= b"),
         InlineData("a=b", "a = b"), InlineData("a<>b", "a <> b"),
         InlineData("a<<b", "a << b"), InlineData("a>>b", "a >> b"),
         InlineData("(a+b)", "(a + b)"), InlineData("((a)+(b))", "((a) + (b))"), InlineData("(a)", "(a)"), InlineData("(a)(b)", "(a)(b)"),
         InlineData("m()", "m()"), InlineData("m(a)", "m(a)"), InlineData("m(a,b)", "m(a, b)"),
         InlineData("m(a,b,c)", "m(a, b, c)"), InlineData("m(a,b(c,d))", "m(a, b(c, d))"), InlineData("m( , ,, )", "m(,,,)"),
         InlineData("a(b(c(0)))", "a(b(c(0)))"), InlineData("if(a,b,c)", "if(a, b, c)"), InlineData("a().b().c()", "a().b().c()"),
         InlineData("""aM5b""    Like ""a[L-P]#[!c-e]a?""", """aM5b"" Like ""a[L-P]#[!c-e]a?""")>
            Public Sub Theory_NormalizeExpressions(code As String, expected As String)
                TestNormalizeExpression(code, expected)
            End Sub

            Private Sub TestNormalizeExpression(text As String, expected As String)
                Dim node = SyntaxFactory.ParseExpression(text)
                Dim actual = node.NormalizeWhitespace("  ").ToFullString()
                Assert.Equal(expected, actual)
            End Sub

            <Fact()>
            Public Sub TestTrailingComment()
                TestNormalizeBlock(
"Dim foo as Bar     ' it is a Bar",
"Dim foo as Bar ' it is a Bar
")
            End Sub

            <Fact()>
            Public Sub TestOptionStatements()
                TestNormalizeBlock(
"Option             Explicit  Off",
"Option Explicit Off
")
            End Sub

#Region "Theory: ImportsStatements"
            <Theory>
            <InlineData("Imports           System", "Imports System
"), InlineData("Imports System.Foo.Bar", "Imports System.Foo.Bar
"), InlineData("Imports T2=System.String", "Imports T2 = System.String
"), InlineData("Imports          <xmlns:db=""http://example.org/database"">", "Imports <xmlns:db=""http://example.org/database"">
")>
            Private Sub Theory_ImportsStatements(Code As String, Expected As String)
                TestNormalizeBlock(Code, Expected)
            End Sub
#End Region

            <Fact(), WorkItem(546397, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestLabelStatements()
                TestNormalizeStatement(
                "while a<b
foo:
c
end while",
"while a < b
foo:
  c
end while")
            End Sub

            <WorkItem(546397, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            <Theory>
            <InlineData("Sub foo()
a()
end Sub",
"Sub foo()
  a()
end Sub
"), InlineData("Function foo()         as   Integer
return 23
end function",
"Function foo() as Integer
  return 23
end function
"), InlineData("Function foo(  x as   System.Int32,[Char] as Integer)         as   Integer
return 23
end function",
"Function foo(x as System.Int32, [Char] as Integer) as Integer
  return 23
end function
"), InlineData("Sub foo()
Dim a ( ) ( )=New Integer ( ) ( ) (   ){ }
end Sub",
"Sub foo()
  Dim a()() = New Integer()()() {}
end Sub
")>
            Private Sub Theory_MethodStatements(Code As String, Expected As String)
                TestNormalizeBlock(Code, Expected)
            End Sub

            <Fact(), WorkItem(546397, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestWithStatements()
                TestNormalizeBlock(
"
Sub foo()
with foo
.bar()
end with
end Sub",
"Sub foo()
  with foo
    .bar()
  end with
end Sub
")
            End Sub

            <Fact(), WorkItem(546397, "http: //vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestSyncLockStatements()
                TestNormalizeBlock(
"Sub foo()
SyncLock me
bar()
end synclock
end Sub
",
"Sub foo()
  SyncLock me
    bar()
  end synclock
end Sub
")
            End Sub

            <Fact(), WorkItem(546397, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestEventStatements()
                TestNormalizeBlock(
"module m1
private withevents x as y
private sub myhandler() Handles y.e1
end sub
end module
",
"module m1

  private withevents x as y

  private sub myhandler() Handles y.e1
  end sub
end module
")
            End Sub

#Region "Theory: AssignmentStatements"
            <WorkItem(546397, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            <Theory>
            <InlineData("module m1
sub s1()
Dim x as Integer()
x(2)=23
Dim s As String = ""boo""&""ya""
End Sub
End Module",
"module m1

  sub s1()
    Dim x as Integer()
    x(2) = 23
    Dim s As String = ""boo"" & ""ya""
  End Sub
End Module
"), InlineData("Module m1

Sub s1()
Dim x As Integer
x ^=23
x *= 23
x /=23
x \= 23
x +=23
x -= 23
x <<=23
x >>= 23
Dim y As String
y &= ""a""
End Sub
End Module
",
"Module m1

  Sub s1()
    Dim x As Integer
    x ^= 23
    x *= 23
    x /= 23
    x \= 23
    x += 23
    x -= 23
    x <<= 23
    x >>= 23
    Dim y As String
    y &= ""a""
  End Sub
End Module
"), InlineData("Module m1
Sub s1()
Dim s1 As String= ""a""
Dim s2 As String=""b""
Mid$(s1, 3, 3) = s2
End Sub
End Module",
"Module m1

  Sub s1()
    Dim s1 As String = ""a""
    Dim s2 As String = ""b""
    Mid$(s1, 3, 3) = s2
  End Sub
End Module
")>
            Private Sub Theory_AssignmentStatements(Code As String, Expected As String)
                TestNormalizeBlock(Code, Expected)
            End Sub
#End Region

#Region "Theory: CallStatements"
            <WorkItem(546397, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            <Theory>
            <InlineData("module m1
Sub s2()
s1 ( 23 )
s1 ( p1:=23 , p2:=23)
End Sub
End Module",
"module m1

  Sub s2()
    s1(23)
    s1(p1:=23, p2:=23)
  End Sub
End Module
")>
            <InlineData("Module m1

Sub s2 ( Of   T )(   Optional x As T= Nothing  )
N1.M2.S2 ( ) 
End Sub
End Module",
"Module m1

  Sub s2(Of T)(Optional x As T = Nothing)
    N1.M2.S2()
  End Sub
End Module
")>
            Private Sub Theory_CallStatements(Code As String, Expected As String)
                TestNormalizeBlock(Code, Expected)
            End Sub

#End Region

            <Fact()>
            Public Sub TestNewStatements()
                TestNormalizeBlock(
"Dim zipState= New With {Key .ZipCode = 98112, .State = ""WA""   }", "Dim zipState = New With {Key .ZipCode = 98112, .State = ""WA""}
")
            End Sub

            <Fact(), WorkItem(546397, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397"), WorkItem(546514, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546514")>
            Public Sub TestXmlAccessStatements()
                TestNormalizeBlock(
"Imports <xmlns:db=""http://example.org/database"">
Module Test
Sub() Main()
Dim x= <db:customer><db:Name>Bob</db:Name></db:customer>
Console.WriteLine(x.<db:Name>)
        End Sub
End Module",
"Imports <xmlns:db=""http://example.org/database"">

Module Test

  Sub () Main() 
    Dim x = <db:customer><db:Name>Bob</db:Name></db:customer>
    Console.WriteLine(x.<db:Name>)
  End Sub
End Module
")
            End Sub

            <Fact(), WorkItem(546397, "http//vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestNamespaceStatements()
                TestNormalizeBlock(
"Imports I1.I2
Namespace N1
Namespace N2.N3
end Namespace
end Namespace",
"Imports I1.I2

Namespace N1
  Namespace N2.N3
  end Namespace
end Namespace
")
            End Sub

            <Fact(), WorkItem(546397, "http//vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestNullableStatements()
                TestNormalizeBlock(
"
module m1
Dim x as Integer?=nothing
end module",
"module m1

  Dim x as Integer? = nothing
end module
")
            End Sub

            <Fact(), WorkItem(546397, "http//vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestInterfaceStatements()
                TestNormalizeBlock(
"namespace N1
Interface I1
public Function F1() As Object
End Interface
Interface I2
Function F2() As Integer
End Interface
Structure S1
Implements I1,I2
public Function F1() As Object
Dim x as Integer=23
return x
end function
End Structure
End Namespace",
"namespace N1

  Interface I1

    public Function F1() As Object

  End Interface

  Interface I2

    Function F2() As Integer

  End Interface

  Structure S1
    Implements I1, I2

    public Function F1() As Object
      Dim x as Integer = 23
      return x
    end function
  End Structure
End Namespace
")
            End Sub

#Region "Theory: EnumStatements"

            <WorkItem(546397, "http//vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            <Theory>
            <InlineData("Module M1

ENUM E1 as long
         foo=23 
bar         
boo=foo
booya=1.4 
end enum
end         MODule",
"Module M1

  ENUM E1 as long
    foo = 23
    bar
    boo = foo
    booya = 1.4
  end enum
end MODule
")>
            <InlineData("class c1
ENUM E1 as long
         foo=23
bar        
boo=foo
booya=1.4
end enum
end         class",
"class c1

  ENUM E1 as long
    foo = 23
    bar
    boo = foo
    booya = 1.4
  end enum
end class
")>
            <InlineData("public class c1

ENUM E1 as long
         foo=23
bar        
boo=foo
booya=1.4
end enum
end         class",
"public class c1

  ENUM E1 as long
    foo = 23
    bar
    boo = foo
    booya = 1.4
  end enum
end class
")>
            <InlineData("class c1
public     ENUM E1 as long
         foo=23
bar        
boo=foo
booya=1.4
end enum
end         class",
"class c1

  public ENUM E1 as long
    foo = 23
    bar
    boo = foo
    booya = 1.4
  end enum
end class
")>
            Private Sub Theory_EnumStatements(Code As String, Expected As String)
                TestNormalizeBlock(Code, Expected)
            End Sub
#End Region

#Region "Theory: DelegateStatement"
            Private Shared Function DataFor_Theory_DelegateStatements() As IEnumerable(Of Object)
                Return {
             ({"Module M1
Dim x=Function( x ,y )x+y
Dim y As Func ( Of Integer ,Integer ,Integer )=x
end MODule",
"Module M1

  Dim x = Function(x, y) x + y

  Dim y As Func(Of Integer, Integer, Integer) = x
end MODule
"}),
({"Module M1
Dim x=Function( x ,y )
return    x+y
end function
Dim y As Func ( Of Integer ,Integer ,Integer )=x
end MODule",
"Module M1

  Dim x = Function(x, y)
    return x + y
  end function

  Dim y As Func(Of Integer, Integer, Integer) = x
end MODule
"}),
({"Module M1
Dim x=Sub( x ,y )
dim x as integer
End sub
Dim y As Action(Of Integer, Integer) = x
            End Module",
"Module M1

  Dim x = Sub(x, y)
    dim x as integer
  End sub

  Dim y As Action(Of Integer, Integer) = x
End Module
"})
            }
            End Function

            <WorkItem(546397, "http//vstfdevdiv:  8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            <Theory, MemberData(NameOf(DataFor_Theory_DelegateStatements))>
            Private Sub Theory_DelegateStatements(code As String, expected As String)
                TestNormalizeBlock(code, expected)
            End Sub

#End Region

            <Fact(), WorkItem(546397, "http//vstfdevdiv: 8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestSelectStatements()

                TestNormalizeBlock(
"    Module M1
sub s1()
select case foo
case    23 
return    foo       
case    42,11 
return    foo       
case    > 100 
return    foo       
case   200 to  300 
return    foo       
case    12,
13
return    foo       
case else
return   foo       
end   select  
end   sub  
end   module  ",
"Module M1

  sub s1()
    select case foo
      case 23
        return foo
      case 42, 11
        return foo
      case > 100
        return foo
      case 200 to 300
        return foo
      case 12, 13
        return foo
      case else
        return foo
    end select
  end sub
end module
")
            End Sub

#Region "Theory: IfStatement"
            Private Shared Function Theory_IfStatement_Data() As IEnumerable(Of Object)
                Return {
({"a", "a"}),
({"if a then b", "if a then b"}),
({"if a then b else c", "if a then b else c"}),
({"if a then b else if c then d else e", "if a then b else if c then d else e"}),
({"if       a      then   b   else   if  c  then    d   else  e", "if a then b else if c then d else e"}),
({"if  " + vbTab + "     a      then   b   else   if  c  then    d   else  e", "if a then b else if c then d else e"}),
({"if a then
b
end if",
"if a then
  b
end if"})}
            End Function

            <WorkItem(546397, "http//vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            <Theory, MemberData(NameOf(Theory_IfStatement_Data), DisableDiscoveryEnumeration:=False)>
            Public Sub Theory_IfStatement(code As String, expected As String)
                TestNormalizeStatement(code, expected)
            End Sub

            <Fact, WorkItem(546397, "http//vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestIfStatement()
                Dim generatedLeftLiteralToken = SyntaxFactory.IntegerLiteralToken("42", LiteralBase.Decimal, TypeCharacter.None, 42)
                Dim generatedRightLiteralToken = SyntaxFactory.IntegerLiteralToken("23", LiteralBase.Decimal, TypeCharacter.None, 23)
                Dim generatedLeftLiteralExpression = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, generatedLeftLiteralToken)
                Dim generatedRightLiteralExpression = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, generatedRightLiteralToken)
                Dim generatedRedLiteralExpression = SyntaxFactory.GreaterThanExpression(generatedLeftLiteralExpression, SyntaxFactory.Token(SyntaxKind.GreaterThanToken), generatedRightLiteralExpression)
                Dim generatedRedIfStatement = SyntaxFactory.IfStatement(SyntaxFactory.Token(SyntaxKind.IfKeyword), generatedRedLiteralExpression, SyntaxFactory.Token(SyntaxKind.ThenKeyword, "Then"))
                Dim expression As ExpressionSyntax = SyntaxFactory.StringLiteralExpression(SyntaxFactory.StringLiteralToken("foo", "foo"))
                Dim callexpression = SyntaxFactory.InvocationExpression(expression:=expression)
                Dim callstatement = SyntaxFactory.CallStatement(SyntaxFactory.Token(SyntaxKind.CallKeyword), callexpression)
                Dim stmtlist = SyntaxFactory.List(Of StatementSyntax)({CType(callstatement, StatementSyntax), CType(callstatement, StatementSyntax)})
                Dim generatedEndIfStatement = SyntaxFactory.EndIfStatement(SyntaxFactory.Token(SyntaxKind.EndKeyword), SyntaxFactory.Token(SyntaxKind.IfKeyword))

                Dim mlib = SyntaxFactory.MultiLineIfBlock(generatedRedIfStatement, stmtlist, Nothing, Nothing, generatedEndIfStatement)
                Dim str = mlib.NormalizeWhitespace("  ").ToFullString()
                Assert.Equal(
"If 42 > 23 Then
  Call foo
  Call foo
End If", str)
            End Sub
#End Region

#Region "Theory: LoopStatements"
            Private Shared Function LoopStatements_TestData() As IEnumerable(Of Object)
                Return {
({"while a<b
c                  
end while",
"while a < b
  c
end while"}),
({"DO until a(2)<>12
Dim x = 12
loop",
"DO until a(2) <> 12
  Dim x = 12
loop"}),
({
"DO while a(2)<>12
Dim x = 12
   loop",
"DO while a(2) <> 12
  Dim x = 12
loop"}),
({"DO               
Dim x = 12
   loop",
"DO
  Dim x = 12
loop"}),
({"DO               
Dim x = 12
    loop until a ( 2 )  <>    12   ",
"DO
  Dim x = 12
loop until a(2) <> 12"}),
({"For     Each   i  In   x
Dim x = 12
   next",
"For Each i In x
  Dim x = 12
next"}),
({"For     Each   i  In   x
For     Each   j  In   x
Dim x = 12
   next j,i",
"For Each i In x
  For Each j In x
    Dim x = 12
next j, i"})
            }
            End Function

            <WorkItem(546397, "http//vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            <Theory, MemberData(NameOf(LoopStatements_TestData), DisableDiscoveryEnumeration:=False)>
            Private Sub TestLoopStatements(code As String, expected As String)
                TestNormalizeStatement(code, expected)
            End Sub
#End Region

            <Fact(), WorkItem(546397, "http//vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestExceptionsStatements()

                TestNormalizeStatement(
"   try
dim x =23
Catch  e1 As Exception When 1>2
Dim x = 23
Catch
Dim x = 23
finally
Dim x = 23
 end try",
"try
  dim x = 23
Catch e1 As Exception When 1 > 2
  Dim x = 23
Catch
  Dim x = 23
finally
  Dim x = 23
end try")
            End Sub

            <Fact(), WorkItem(546397, "http//vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestUsingStatements()

                TestNormalizeStatement(
"  Using   r1  As  R =  New R ( ) ,   r2 As R = New R( )
dim x =23
end using",
"Using r1 As R = New R(), r2 As R = New R()
  dim x = 23
end using")
            End Sub

            <Fact()>
            Public Sub TestQueryExpressions()

                TestNormalizeStatement(
"  Dim waCusts = _
From cust As Customer In Customers _
Where    cust.State    =  ""WA""",
"Dim waCusts = From cust As Customer In Customers Where cust.State = ""WA""")
            End Sub

            <Fact(), WorkItem(546397, "http//vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestDefaultCasingForKeywords()
                Dim expected =
"Module m1

  Dim x = Function(x, y) x + y

  Dim y As func(Of Integer, Integer, Integer) = x
End Module
"

                Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(expected.ToLowerInvariant)
                Dim actual = node.NormalizeWhitespace(indentation:="  ", useDefaultCasing:=True).ToFullString()
                Assert.Equal(expected, actual)
            End Sub

            <Fact(), WorkItem(546397, "http//vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestComment()

                ' trailing whitespace will be aligned to the indent level (see second comment)
                ' when determining if there should be a separator between tokens, the current algorithm does not check if a token comes out 
                ' of structured trivia. Because of that there is now a space at the end of structured trivia before the next token
                ' whitespace before comments get reduced to one (see comment after code), whitespace in trivia is maintained (see same comment)
                ' xml doc comments somehow contain \n in the XmlTextLiterals ... will not spend time to work around
                Dim input =
"Module m1
  ' a nice comment
  ''' even more comments
' and more comments 
#if false   
 whatever? 
#end if
' and more comments
 ''' structured trivia before code
Dim x = Function(x, y) x + y      '  trivia after code
End Module"

                Dim expected =
"Module m1

  ' a nice comment
  ''' even more comments

  ' and more comments 
#if false
 whatever? 
#end if
  ' and more comments
  ''' structured trivia before code
  Dim x = Function(x, y) x + y '  trivia after code
End Module
"

                Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(input)
                Dim actual = node.NormalizeWhitespace("  ").ToFullString()
                Assert.Equal(expected, actual)
            End Sub

            <Fact(), WorkItem(546397, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestMultilineLambdaFunctionsAsParameter()

                ' trailing whitespace will be aligned to the indent level (see second comment)
                ' when determining if there should be a separator between tokens, the current algorithm does not check if a token comes out 
                ' of structured trivia. Because of that there is now a space at the end of structured trivia before the next token
                ' whitespace before comments get reduced to one (see comment after code), whitespace in trivia is maintained (see same comment)
                ' xml doc comments somehow contain \n in the XmlTextLiterals ... will not spend time to work around
                Dim input =
"Module m1
Sub Main(args As String())
Sub1(Function(p As Integer)
Sub2()
End Function)
End Sub
End Module"
                Dim expected =
"Module m1

  Sub Main(args As String())
    Sub1(Function(p As Integer)
      Sub2()
    End Function)
  End Sub
End Module
"

                Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(input)
                Dim actual = node.NormalizeWhitespace("  ").ToFullString()
                Assert.Equal(expected, actual)
            End Sub

            <Fact(), WorkItem(546397, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestProperty()
                Dim input = "Property    p   As  Integer         
                Get
            End     Get

Set (   value	As	Integer )
    End	Set
            End	Property"

                Dim expected =
"Property p As Integer
  Get
  End Get

  Set(value As Integer)
  End Set
End Property
"
                TestNormalizeBlock(input, expected)
            End Sub

            Private Sub TestNormalizeStatement(text As String, expected As String)
                Dim node As StatementSyntax = SyntaxFactory.ParseExecutableStatement(text)
                Dim actual = node.NormalizeWhitespace("  ").ToFullString()
                Assert.Equal(expected, actual)
            End Sub

            Private Sub TestNormalizeBlock(text As String, expected As String)
                Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(text)
                Dim actual = node.NormalizeWhitespace("  ").ToFullString()
                expected = expected.Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf) ' in case tests use XML literals

                Assert.Equal(expected, actual)
            End Sub

            <Fact(), WorkItem(546397, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
            Public Sub TestStructuredTriviaAndAttributes()
                Dim source =
"Module m1
 '''<x>...</x>
  <foo()>
  Sub a()
  End Sub
End Module 
"

                Dim expected =
"Module m1

  '''<x>...</x>
  <foo()>
  Sub a()
  End Sub
End Module
"

                Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(source)
                Dim actual = node.NormalizeWhitespace(indentation:="  ", useDefaultCasing:=False).ToFullString()
                Assert.Equal(expected, actual)
            End Sub

            <Fact(), WorkItem(531607, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531607")>
            Public Sub TestNestedStructuredTrivia()
                Dim trivia = SyntaxFactory.TriviaList(
                SyntaxFactory.Trivia(
                    SyntaxFactory.ConstDirectiveTrivia("constant",
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal(1).WithTrailingTrivia(SyntaxFactory.Trivia(SyntaxFactory.SkippedTokensTrivia(SyntaxFactory.TokenList(SyntaxFactory.Literal("A"c)))))))))

                Dim expected = "#Const constant = 1 ""A""c"

                Dim actual = trivia.NormalizeWhitespace(indentation:="  ", elasticTrivia:=False, useDefaultCasing:=False).ToFullString()
                Assert.Equal(expected, actual)
            End Sub

            <Fact()>
            Public Sub TestCrefAttribute()
                Dim source =
"''' <summary>
''' <see cref  = """"/>
''' <see cref   =""""/>
''' <see cref= """"/>
''' <see cref=""""/>
''' <see cref  = ""1""/>
''' <see cref   =""a""/>
''' <see cref= ""Integer()""/>
''' <see cref   = ""a()""/>
''' </summary>
Module Program
End Module
"
                Dim expected =
"''' <summary>
''' <see cref=""""/> 
''' <see cref=""""/> 
''' <see cref=""""/> 
''' <see cref=""""/> 
''' <see cref=""1""/> 
''' <see cref=""a""/> 
''' <see cref=""Integer()""/> 
''' <see cref=""a()""/> 
''' </summary>
Module Program
End Module
"

                Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(source)
                Dim actual = node.NormalizeWhitespace(indentation:="  ", useDefaultCasing:=False).ToFullString()
                Assert.Equal(expected, actual)
            End Sub

            <Fact()>
            Public Sub TestNameAttribute()
                Dim source =
"''' <summary>
''' <paramref name  = """"/>
''' <paramref name   =""""/>
''' <paramref name= """"/>
''' <paramref name=""""/>
''' <paramref name  = ""1""/>
''' <paramref name   =""a""/>
''' <paramref name= ""Integer()""/>
''' <paramref name   = ""a()""/>
''' </summary>
Module Program
End Module
"

                Dim expected =
"''' <summary>
''' <paramref name=""""/> 
''' <paramref name=""""/> 
''' <paramref name=""""/> 
''' <paramref name=""""/> 
''' <paramref name=""1""/> 
''' <paramref name=""a""/> 
''' <paramref name=""Integer()""/> 
''' <paramref name=""a()""/> 
''' </summary>
Module Program
End Module
"
                Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(source)
                Dim actual = node.NormalizeWhitespace(indentation:="  ", useDefaultCasing:=False).ToFullString()
                Assert.Equal(expected, actual)
            End Sub

            <Fact>
            Public Sub TestEnableWarningDirective()
                Dim text = "         #  enable           warning[BC000],Bc123,             BC456,_789'          comment
# enable   warning
# enable   warning ,"

                Dim root = Parse(text).GetRoot()
                Dim normalizedRoot = root.NormalizeWhitespace(indentation:="    ", elasticTrivia:=True, useDefaultCasing:=True)

                Dim expected = "#Enable Warning [BC000], Bc123, BC456, _789 '          comment
#Enable Warning
#Enable Warning ,
"

                Assert.Equal(expected, normalizedRoot.ToFullString())
            End Sub

            <Fact>
            Public Sub TestDisableWarningDirective()
                Dim text = "Module Program
#   disable warning
    Sub Main()
        #disable       warning          bc123,            Bc456,BC789
    End Sub
#   disable   warning[BC123],   '   Comment
End Module"

                Dim root = Parse(text).GetRoot()
                Dim normalizedRoot = root.NormalizeWhitespace(indentation:="    ", elasticTrivia:=True, useDefaultCasing:=True)

                Dim expected = "Module Program

#Disable Warning
    Sub Main()
#Disable Warning bc123, Bc456, BC789
    End Sub
#Disable Warning [BC123], '   Comment
 End Module
"
                Assert.Equal(expected, normalizedRoot.ToFullString())
            End Sub

            <Fact>
            Public Sub TestNormalizeEOL()
                Dim code =
"Class C
End Class"
                Dim expected =
"Class C
End Class
"
                Dim actual = SyntaxFactory.ParseCompilationUnit(code).NormalizeWhitespace("  ").ToFullString()
                Assert.Equal(expected, actual)
            End Sub

            <Fact>
            Public Sub TestNormalizeTabs()
                Dim code =
"Class C
Sub M()
End Sub
End Class"
                Dim expected =
"Class C

" & vbTab & "Sub M()
" & vbTab & "End Sub
End Class
"
                Dim actual = SyntaxFactory.ParseCompilationUnit(code).NormalizeWhitespace(vbTab).ToFullString()
                Assert.Equal(expected, actual)
            End Sub

        End Class
    End Namespace
End Namespace