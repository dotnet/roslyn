' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class SyntaxFormatterTests

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestAllInVB()
            Dim allInVB As String = TestResource.AllInOneVisualBasicCode
            Dim expected As String = TestResource.AllInOneVisualBasicBaseline

            Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(allInVB)
            Dim actual = node.NormalizeWhitespace("    ").ToFullString()
            Assert.Equal(expected, actual)
        End Sub

        <Fact()>
        Public Sub TestFormatExpressions()
            TestFormatExpression("+1", "+1")
            TestFormatExpression("+a", "+a")
            TestFormatExpression("-a", "-a")

            TestFormatExpression("a", "a")
            TestFormatExpression("a+b", "a + b")
            TestFormatExpression("a-b", "a - b")
            TestFormatExpression("a*b", "a * b")
            TestFormatExpression("a/b", "a / b")
            TestFormatExpression("a mod b", "a mod b")
            TestFormatExpression("a xor b", "a xor b")
            TestFormatExpression("a or b", "a or b")
            TestFormatExpression("a and b", "a and b")
            TestFormatExpression("a orelse b", "a orelse b")
            TestFormatExpression("a andalso b", "a andalso b")
            TestFormatExpression("a<b", "a < b")
            TestFormatExpression("a<=b", "a <= b")
            TestFormatExpression("a>b", "a > b")
            TestFormatExpression("a>=b", "a >= b")
            TestFormatExpression("a=b", "a = b")
            TestFormatExpression("a<>b", "a <> b")
            TestFormatExpression("a<<b", "a << b")
            TestFormatExpression("a>>b", "a >> b")

            TestFormatExpression("(a+b)", "(a + b)")
            TestFormatExpression("((a)+(b))", "((a) + (b))")
            TestFormatExpression("(a)", "(a)")
            TestFormatExpression("(a)(b)", "(a)(b)")

            TestFormatExpression("m()", "m()")
            TestFormatExpression("m(a)", "m(a)")
            TestFormatExpression("m(a,b)", "m(a, b)")
            TestFormatExpression("m(a,b,c)", "m(a, b, c)")
            TestFormatExpression("m(a,b(c,d))", "m(a, b(c, d))")
            TestFormatExpression("m( , ,, )", "m(,,,)")
            TestFormatExpression("a(b(c(0)))", "a(b(c(0)))")

            TestFormatExpression("if(a,b,c)", "if(a, b, c)")

            TestFormatExpression("a().b().c()", "a().b().c()")

            TestFormatExpression("""aM5b""    Like ""a[L-P]#[!c-e]a?""", """aM5b"" Like ""a[L-P]#[!c-e]a?""")
        End Sub

        Private Sub TestFormatExpression(text As String, expected As String)
            Dim node = SyntaxFactory.ParseExpression(text)
            Dim actual = node.NormalizeWhitespace("  ").ToFullString()
            Assert.Equal(expected, actual)
        End Sub

        <Fact()>
        Public Sub TestTrailingComment()
            TestFormatBlock("Dim foo as Bar     ' it is a Bar", "Dim foo as Bar ' it is a Bar" + vbCrLf)
        End Sub


        <Fact()>
        Public Sub TestOptionStatements()
            TestFormatBlock("Option             Explicit  Off", "Option Explicit Off" + vbCrLf)
        End Sub

        <Fact()>
        Public Sub TestImportsStatements()
            TestFormatBlock("Imports           System", "Imports System" + vbCrLf)
            TestFormatBlock("Imports System.Foo.Bar", "Imports System.Foo.Bar" + vbCrLf)
            TestFormatBlock("Imports T2=System.String", "Imports T2 = System.String" + vbCrLf)
            TestFormatBlock("Imports          <xmlns:db=""http://example.org/database"">", "Imports <xmlns:db=""http://example.org/database"">" + vbCrLf)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestLabelStatements()
            TestFormatStatement("while a<b" + vbCrLf + "foo:" + vbCrLf + "c" + vbCrLf + "end while", "while a < b" + vbCrLf + "foo:" + vbCrLf + "  c" + vbCrLf + "end while")
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestMethodStatements()
            TestFormatBlock("Sub foo()" + vbCrLf + "a()" + vbCrLf + "end Sub", "Sub foo()" + vbCrLf + "  a()" + vbCrLf + "end Sub" + vbCrLf)
            TestFormatBlock("Function foo()         as   Integer" + vbCrLf + "return 23" + vbCrLf + "end function", "Function foo() as Integer" + vbCrLf + "  return 23" + vbCrLf + "end function" + vbCrLf)
            TestFormatBlock("Function foo(  x as   System.Int32,[Char] as Integer)         as   Integer" + vbCrLf + "return 23" + vbCrLf + "end function", "Function foo(x as System.Int32, [Char] as Integer) as Integer" + vbCrLf + "  return 23" + vbCrLf + "end function" + vbCrLf)
            TestFormatBlock("Sub foo()" + vbCrLf + "Dim a ( ) ( )=New Integer ( ) ( ) (   ){ }" + vbCrLf + "end Sub", "Sub foo()" + vbCrLf + "  Dim a()() = New Integer()()() {}" + vbCrLf + "end Sub" + vbCrLf)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestWithStatements()
            TestFormatBlock(
<code>
Sub foo()
with foo
.bar()
end with
end Sub</code>.Value, _
 _
<code>Sub foo()
  with foo
    .bar()
  end with
end Sub
</code>.Value)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestSyncLockStatements()
            TestFormatBlock("Sub foo()" + vbCrLf + "SyncLock me" + vbCrLf + "bar()" + vbCrLf + "end synclock" + vbCrLf + "end Sub",
                            "Sub foo()" + vbCrLf + "  SyncLock me" + vbCrLf + "    bar()" + vbCrLf + "  end synclock" + vbCrLf + "end Sub" + vbCrLf)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestEventStatements()
            TestFormatBlock(
<code>
module m1
private withevents x as y
private sub myhandler() Handles y.e1
end sub
end module
</code>.Value,
 _
<code>module m1

  private withevents x as y

  private sub myhandler() Handles y.e1
  end sub
end module
</code>.Value)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestAssignmentStatements()
            TestFormatBlock("module m1" + vbCrLf +
                            "sub s1()" + vbCrLf +
                            "Dim x as Integer()" + vbCrLf +
                            "x(2)=23" + vbCrLf +
                            "Dim s as string=""boo""&""ya""" + vbCrLf +
                            "end sub" + vbCrLf +
                            "end module", _
 _
                            "module m1" + vbCrLf + vbCrLf +
                            "  sub s1()" + vbCrLf +
                            "    Dim x as Integer()" + vbCrLf +
                            "    x(2) = 23" + vbCrLf +
                            "    Dim s as string = ""boo"" & ""ya""" + vbCrLf +
                            "  end sub" + vbCrLf +
                            "end module" + vbCrLf)

            TestFormatBlock("module m1" + vbCrLf + vbCrLf +
                            "sub s1()" + vbCrLf +
                            "Dim x as Integer" + vbCrLf +
                            "x^=23" + vbCrLf +
                            "x*=23" + vbCrLf +
                            "x/=23" + vbCrLf +
                            "x\=23" + vbCrLf +
                            "x+=23" + vbCrLf +
                            "x-=23" + vbCrLf +
                            "x<<=23" + vbCrLf +
                            "x>>=23" + vbCrLf +
                            "Dim y as string" + vbCrLf +
                            "y &=""a""" + vbCrLf +
                            "end sub" + vbCrLf +
                            "end module", _
 _
                            "module m1" + vbCrLf + vbCrLf +
                            "  sub s1()" + vbCrLf +
                            "    Dim x as Integer" + vbCrLf +
                            "    x ^= 23" + vbCrLf +
                            "    x *= 23" + vbCrLf +
                            "    x /= 23" + vbCrLf +
                            "    x \= 23" + vbCrLf +
                            "    x += 23" + vbCrLf +
                            "    x -= 23" + vbCrLf +
                            "    x <<= 23" + vbCrLf +
                            "    x >>= 23" + vbCrLf +
                            "    Dim y as string" + vbCrLf +
                            "    y &= ""a""" + vbCrLf +
                            "  end sub" + vbCrLf +
                            "end module" + vbCrLf)

            TestFormatBlock("module m1" + vbCrLf +
                            "sub s1()" + vbCrLf +
                            "Dim s1 As String=""a""" + vbCrLf +
                            "Dim s2 As String=""b""" + vbCrLf +
                            "Mid$(s1,3,3)=s2" + vbCrLf +
                            "end sub" + vbCrLf +
                            "end module", _
 _
                            "module m1" + vbCrLf + vbCrLf +
                            "  sub s1()" + vbCrLf +
                            "    Dim s1 As String = ""a""" + vbCrLf +
                            "    Dim s2 As String = ""b""" + vbCrLf +
                            "    Mid$(s1, 3, 3) = s2" + vbCrLf +
                            "  end sub" + vbCrLf +
                            "end module" + vbCrLf)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestCallStatements()
            TestFormatBlock("module m1" + vbCrLf +
                            "sub s2()" + vbCrLf +
                            "s1 ( 23 )" + vbCrLf +
                            "s1 ( p1:=23 , p2:=23)" + vbCrLf +
                            "end sub" + vbCrLf +
                            "end module", _
 _
                            "module m1" + vbCrLf + vbCrLf +
                            "  sub s2()" + vbCrLf +
                            "    s1(23)" + vbCrLf +
                            "    s1(p1:=23, p2:=23)" + vbCrLf +
                            "  end sub" + vbCrLf +
                            "end module" + vbCrLf)

            TestFormatBlock("module m1" + vbCrLf + vbCrLf +
                            "sub s2 ( Of   T ) (   optional x As T=nothing  )" + vbCrLf +
                            "N1.M2.S2 ( ) " + vbCrLf +
                            "end sub" + vbCrLf +
                            "end module", _
 _
                            "module m1" + vbCrLf + vbCrLf +
                            "  sub s2(Of T)(optional x As T = nothing)" + vbCrLf +
                            "    N1.M2.S2()" + vbCrLf +
                            "  end sub" + vbCrLf +
                            "end module" + vbCrLf)
        End Sub

        <Fact()>
        Public Sub TestNewStatements()
            TestFormatBlock("Dim zipState=New With {   Key .ZipCode=98112, .State=""WA""   }",
                            "Dim zipState = New With {Key .ZipCode = 98112, .State = ""WA""}" + vbCrLf)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv"), WorkItem(546514, "DevDiv")>
        Public Sub TestXmlAccessStatements()
            TestFormatBlock("Imports <xmlns:db=""http://example.org/database"">" + vbCrLf +
                            "Module Test" + vbCrLf +
                            "Sub Main ( )" + vbCrLf +
                            "Dim x=<db:customer><db:Name>Bob</db:Name></db:customer>" + vbCrLf +
                            "Console . WriteLine ( x .<   db:Name  > )" + vbCrLf +
                            "End Sub" + vbCrLf +
                            "End Module",
 _
                            "Imports <xmlns:db=""http://example.org/database"">" + vbCrLf +
                            "" + vbCrLf +
                            "Module Test" + vbCrLf + vbCrLf +
                            "  Sub Main()" + vbCrLf +
                            "    Dim x = <db:customer><db:Name>Bob</db:Name></db:customer>" + vbCrLf +
                            "    Console.WriteLine(x.<db:Name>)" + vbCrLf +
                            "  End Sub" + vbCrLf +
                            "End Module" + vbCrLf)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestNamespaceStatements()
            TestFormatBlock("Imports I1.I2" + vbCrLf +
                            "Namespace N1" + vbCrLf +
                            "Namespace N2.N3" + vbCrLf +
                            "end Namespace" + vbCrLf +
                            "end Namespace", _
 _
                            "Imports I1.I2" + vbCrLf +
                            "" + vbCrLf +
                            "Namespace N1" + vbCrLf +
                            "  Namespace N2.N3" + vbCrLf +
                            "  end Namespace" + vbCrLf +
                            "end Namespace" + vbCrLf)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestNullableStatements()
            TestFormatBlock(
<code>
module m1
Dim x as Integer?=nothing
end module</code>.Value, _
 _
<code>module m1

  Dim x as Integer? = nothing
end module
</code>.Value)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestInterfaceStatements()
            TestFormatBlock("namespace N1" + vbCrLf +
                            "Interface I1" + vbCrLf +
                            "public Function F1() As Object" + vbCrLf +
                            "End Interface" + vbCrLf +
                            "Interface I2" + vbCrLf +
                            "Function F2() As Integer" + vbCrLf +
                            "End Interface" + vbCrLf +
                            "Structure S1" + vbCrLf +
                            "Implements I1,I2" + vbCrLf +
                            "public Function F1() As Object" + vbCrLf +
                            "Dim x as Integer=23" + vbCrLf +
                            "return x" + vbCrLf +
                            "end function" + vbCrLf +
                            "End Structure" + vbCrLf +
                            "End Namespace", _
 _
                            "namespace N1" + vbCrLf + vbCrLf +
                            "  Interface I1" + vbCrLf + vbCrLf +
                            "    public Function F1() As Object" + vbCrLf + vbCrLf +
                            "  End Interface" + vbCrLf + vbCrLf +
                            "  Interface I2" + vbCrLf + vbCrLf +
                            "    Function F2() As Integer" + vbCrLf + vbCrLf +
                            "  End Interface" + vbCrLf + vbCrLf +
                            "  Structure S1" + vbCrLf +
                            "    Implements I1, I2" + vbCrLf + vbCrLf +
                            "    public Function F1() As Object" + vbCrLf +
                            "      Dim x as Integer = 23" + vbCrLf +
                            "      return x" + vbCrLf +
                            "    end function" + vbCrLf +
                            "  End Structure" + vbCrLf +
                            "End Namespace" + vbCrLf)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestEnumStatements()
            TestFormatBlock("Module M1" + vbCrLf + vbCrLf +
                            "ENUM E1 as long" + vbCrLf +
                            "         foo=23" + vbCrLf +
                            "bar        " + vbCrLf +
                            "boo=foo" + vbCrLf +
                            "booya=1.4" + vbCrLf +
                            "end enum" + vbCrLf +
                            "end         MODule", _
 _
                            "Module M1" + vbCrLf + vbCrLf +
                            "  ENUM E1 as long" + vbCrLf +
                            "    foo = 23" + vbCrLf +
                            "    bar" + vbCrLf +
                            "    boo = foo" + vbCrLf +
                            "    booya = 1.4" + vbCrLf +
                            "  end enum" + vbCrLf +
                            "end MODule" + vbCrLf)

            TestFormatBlock("class c1" + vbCrLf +
                            "ENUM E1 as long" + vbCrLf +
                            "         foo=23" + vbCrLf +
                            "bar        " + vbCrLf +
                            "boo=foo" + vbCrLf +
                            "booya=1.4" + vbCrLf +
                            "end enum" + vbCrLf +
                            "end         class", _
 _
                            "class c1" + vbCrLf + vbCrLf +
                            "  ENUM E1 as long" + vbCrLf +
                            "    foo = 23" + vbCrLf +
                            "    bar" + vbCrLf +
                            "    boo = foo" + vbCrLf +
                            "    booya = 1.4" + vbCrLf +
                            "  end enum" + vbCrLf +
                            "end class" + vbCrLf)

            TestFormatBlock("public class c1" + vbCrLf + vbCrLf +
                            "ENUM E1 as long" + vbCrLf +
                            "         foo=23" + vbCrLf +
                            "bar        " + vbCrLf +
                            "boo=foo" + vbCrLf +
                            "booya=1.4" + vbCrLf +
                            "end enum" + vbCrLf +
                            "end         class", _
 _
                            "public class c1" + vbCrLf + vbCrLf +
                            "  ENUM E1 as long" + vbCrLf +
                            "    foo = 23" + vbCrLf +
                            "    bar" + vbCrLf +
                            "    boo = foo" + vbCrLf +
                            "    booya = 1.4" + vbCrLf +
                            "  end enum" + vbCrLf +
                            "end class" + vbCrLf)

            TestFormatBlock("class c1" + vbCrLf +
                            "public     ENUM E1 as long" + vbCrLf +
                            "         foo=23" + vbCrLf +
                            "bar        " + vbCrLf +
                            "boo=foo" + vbCrLf +
                            "booya=1.4" + vbCrLf +
                            "end enum" + vbCrLf +
                            "end         class", _
 _
                            "class c1" + vbCrLf + vbCrLf +
                            "  public ENUM E1 as long" + vbCrLf +
                            "    foo = 23" + vbCrLf +
                            "    bar" + vbCrLf +
                            "    boo = foo" + vbCrLf +
                            "    booya = 1.4" + vbCrLf +
                            "  end enum" + vbCrLf +
                            "end class" + vbCrLf)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestDelegateStatements()

            TestFormatBlock("Module M1" + vbCrLf +
                            "Dim x=Function( x ,y )x+y" + vbCrLf +
                            "Dim y As Func ( Of Integer ,Integer ,Integer )=x" + vbCrLf +
                            "end MODule", _
 _
                            "Module M1" + vbCrLf + vbCrLf +
                            "  Dim x = Function(x, y) x + y" + vbCrLf + vbCrLf +
                            "  Dim y As Func(Of Integer, Integer, Integer) = x" + vbCrLf +
                            "end MODule" + vbCrLf)

            TestFormatBlock("Module M1" + vbCrLf +
                            "Dim x=Function( x ,y )" + vbCrLf +
                            "return    x+y" + vbCrLf +
                            "end function" + vbCrLf +
                            "Dim y As Func ( Of Integer ,Integer ,Integer )=x" + vbCrLf +
                            "end MODule", _
 _
                            "Module M1" + vbCrLf + vbCrLf +
                            "  Dim x = Function(x, y)" + vbCrLf +
                            "    return x + y" + vbCrLf +
                            "  end function" + vbCrLf + vbCrLf +
                            "  Dim y As Func(Of Integer, Integer, Integer) = x" + vbCrLf +
                            "end MODule" + vbCrLf)

            TestFormatBlock("Module M1" + vbCrLf +
                            "Dim x=Sub( x ,y )" + vbCrLf +
                            "dim x as integer" + vbCrLf +
                            "end sub" + vbCrLf +
                            "Dim y As Action ( Of Integer ,Integer)=x" + vbCrLf +
                            "end MODule", _
 _
                            "Module M1" + vbCrLf + vbCrLf +
                            "  Dim x = Sub(x, y)" + vbCrLf +
                            "    dim x as integer" + vbCrLf +
                            "  end sub" + vbCrLf +
                            vbCrLf +
                            "  Dim y As Action(Of Integer, Integer) = x" + vbCrLf +
                            "end MODule" + vbCrLf)

        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestSelectStatements()

            TestFormatBlock("    Module M1" + vbCrLf +
                            "sub s1()" + vbCrLf +
                            "select case foo" + vbCrLf +
                            "case    23 " + vbCrLf +
                            "return    foo       " + vbCrLf +
                            "case    42,11 " + vbCrLf +
                            "return    foo       " + vbCrLf +
                            "case    > 100 " + vbCrLf +
                            "return    foo       " + vbCrLf +
                            "case   200 to  300 " + vbCrLf +
                            "return    foo       " + vbCrLf +
                            "case    12," + vbCrLf +
                            "13" + vbCrLf +
                            "return    foo       " + vbCrLf +
                            "case else" + vbCrLf +
                            "return   foo       " + vbCrLf +
                            "end   select  " + vbCrLf +
                            "end   sub  " + vbCrLf +
                            "end   module  ", _
 _
                            "Module M1" + vbCrLf + vbCrLf +
                            "  sub s1()" + vbCrLf +
                            "    select case foo" + vbCrLf +
                            "      case 23" + vbCrLf +
                            "        return foo" + vbCrLf +
                            "      case 42, 11" + vbCrLf +
                            "        return foo" + vbCrLf +
                            "      case > 100" + vbCrLf +
                            "        return foo" + vbCrLf +
                            "      case 200 to 300" + vbCrLf +
                            "        return foo" + vbCrLf +
                            "      case 12, 13" + vbCrLf +
                            "        return foo" + vbCrLf +
                            "      case else" + vbCrLf +
                            "        return foo" + vbCrLf +
                            "    end select" + vbCrLf +
                            "  end sub" + vbCrLf +
                            "end module" + vbCrLf)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestFormatIfStatement()

            ' expressions
            TestFormatStatement("a", "a")

            ' if
            TestFormatStatement("if a then b", "if a then b")
            TestFormatStatement("if a then b else c", "if a then b else c")
            TestFormatStatement("if a then b else if c then d else e", "if a then b else if c then d else e")
            TestFormatStatement("if       a      then   b   else   if  c  then    d   else  e", "if a then b else if c then d else e")
            TestFormatStatement("if  " + vbTab + "     a      then   b   else   if  c  then    d   else  e", "if a then b else if c then d else e")
            TestFormatStatement("if a then" + vbCrLf + "b" + vbCrLf + "end if", "if a then" + vbCrLf + "  b" + vbCrLf + "end if")
            TestFormatStatement("if a then" + vbCrLf + vbCrLf + vbCrLf + "b" + vbCrLf + "end if", "if a then" + vbCrLf + "  b" + vbCrLf + "end if")
            TestFormatStatement("if   a   then" + vbCrLf + "if a then" + vbCrLf + "b" + vbCrLf + "end if" + vbCrLf + "else" + vbCrLf + "b" + vbCrLf + "end if",
                                "if a then" + vbCrLf + "  if a then" + vbCrLf + "    b" + vbCrLf + "  end if" + vbCrLf + "else" + vbCrLf + "  b" + vbCrLf + "end if")

            ' line continuation trivia will be removed
            TestFormatStatement("if a then _" + vbCrLf + "b _" + vbCrLf + "else       c", "if a then b else c")
            TestFormatStatement("if a then:b:end if", "if a then : b : end if")

            Dim generatedLeftLiteralToken = SyntaxFactory.IntegerLiteralToken("42", LiteralBase.Decimal, TypeCharacter.None, 42)
            Dim generatedRightLiteralToken = SyntaxFactory.IntegerLiteralToken("23", LiteralBase.Decimal, TypeCharacter.None, 23)
            Dim generatedLeftLiteralExpression = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, generatedLeftLiteralToken)
            Dim generatedRightLiteralExpression = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, generatedRightLiteralToken)
            Dim generatedRedLiteratalExpression = SyntaxFactory.GreaterThanExpression(generatedLeftLiteralExpression, SyntaxFactory.Token(SyntaxKind.GreaterThanToken), generatedRightLiteralExpression)
            Dim generatedRedIfStatement = SyntaxFactory.IfStatement(Nothing, SyntaxFactory.Token(SyntaxKind.IfKeyword), generatedRedLiteratalExpression, SyntaxFactory.Token(SyntaxKind.ThenKeyword, "THeN"))
            Dim expression As ExpressionSyntax = SyntaxFactory.StringLiteralExpression(SyntaxFactory.StringLiteralToken("foo", "foo"))
            Dim callexpression = SyntaxFactory.InvocationExpression(expression:=expression)
            Dim callstatement = SyntaxFactory.CallStatement(SyntaxFactory.Token(SyntaxKind.CallKeyword), callexpression)
            Dim stmtlist = SyntaxFactory.List(Of StatementSyntax)({CType(callstatement, StatementSyntax), CType(callstatement, StatementSyntax)})
            Dim generatedRedIfPart = SyntaxFactory.IfPart(generatedRedIfStatement, stmtlist)
            Dim generatedEndIfStatment = SyntaxFactory.EndIfStatement(SyntaxFactory.Token(SyntaxKind.EndKeyword), SyntaxFactory.Token(SyntaxKind.IfKeyword))

            Dim mlib = SyntaxFactory.MultiLineIfBlock(generatedRedIfPart, Nothing, Nothing, generatedEndIfStatment)
            Dim str = mlib.NormalizeWhitespace("  ").ToFullString()
            Assert.Equal("If 42 > 23 THeN" + vbCrLf + "  Call foo" + vbCrLf + "  Call foo" + vbCrLf + "End If", str)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestLoopStatements()
            TestFormatStatement("while a<b" + vbCrLf + "c                  " + vbCrLf + "end while", "while a < b" + vbCrLf + "  c" + vbCrLf + "end while")

            TestFormatStatement("DO until a(2)<>12" + vbCrLf +
                            "Dim x = 12" + vbCrLf +
                            "   loop", _
 _
                            "DO until a(2) <> 12" + vbCrLf +
                            "  Dim x = 12" + vbCrLf +
                            "loop")

            TestFormatStatement("DO while a(2)<>12" + vbCrLf +
                            "Dim x = 12" + vbCrLf +
                            "   loop", _
 _
                            "DO while a(2) <> 12" + vbCrLf +
                            "  Dim x = 12" + vbCrLf +
                            "loop")

            TestFormatStatement("DO               " + vbCrLf +
                            "Dim x = 12" + vbCrLf +
                            "   loop", _
 _
                            "DO" + vbCrLf +
                            "  Dim x = 12" + vbCrLf +
                            "loop")

            TestFormatStatement("DO               " + vbCrLf +
                            "Dim x = 12" + vbCrLf +
                            "   loop until a ( 2 )  <>    12   ", _
 _
                            "DO" + vbCrLf +
                            "  Dim x = 12" + vbCrLf +
                            "loop until a(2) <> 12")

            TestFormatStatement("For     Each   i  In   x" + vbCrLf +
                            "Dim x = 12" + vbCrLf +
                            "   next", _
 _
                            "For Each i In x" + vbCrLf +
                            "  Dim x = 12" + vbCrLf +
                            "next")

            TestFormatStatement("For     Each   i  In   x" + vbCrLf +
                                "For     Each   j  In   x" + vbCrLf +
                                "Dim x = 12" + vbCrLf +
                                "   next j,i", _
 _
                            "For Each i In x" + vbCrLf +
                            "  For Each j In x" + vbCrLf +
                            "    Dim x = 12" + vbCrLf +
                            "next j, i")
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestExceptionsStatements()

            TestFormatStatement("   try" + vbCrLf +
                            "dim x =23" + vbCrLf +
                            "Catch  e1 As Exception When 1>2" + vbCrLf +
                            "dim x =23" + vbCrLf +
                            "Catch" + vbCrLf +
                            "dim x =23" + vbCrLf +
                            "finally" + vbCrLf +
                            "dim x =23" + vbCrLf +
                            " end try", _
 _
                            "try" + vbCrLf +
                            "  dim x = 23" + vbCrLf +
                            "Catch e1 As Exception When 1 > 2" + vbCrLf +
                            "  dim x = 23" + vbCrLf +
                            "Catch" + vbCrLf +
                            "  dim x = 23" + vbCrLf +
                            "finally" + vbCrLf +
                            "  dim x = 23" + vbCrLf +
                            "end try")
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestUsingStatements()

            TestFormatStatement("  Using   r1  As  R =  New R ( ) ,   r2 As R = New R( )" + vbCrLf +
                            "dim x =23" + vbCrLf +
                            "end using", _
 _
                            "Using r1 As R = New R(), r2 As R = New R()" + vbCrLf +
                            "  dim x = 23" + vbCrLf +
                            "end using")
        End Sub

        <Fact()>
        Public Sub TestQueryExpressions()

            TestFormatStatement("  Dim waCusts = _" + vbCrLf +
                            "From cust As Customer In Customers _" + vbCrLf +
                            "Where    cust.State    =  ""WA""", _
 _
                            "Dim waCusts = From cust As Customer In Customers Where cust.State = ""WA""")
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestDefaultCasingForKeywords()
            Dim expected = "Module m1" + vbCrLf + vbCrLf +
                            "  Dim x = Function(x, y) x + y" + vbCrLf + vbCrLf +
                            "  Dim y As func(Of Integer, Integer, Integer) = x" + vbCrLf +
                            "End Module" + vbCrLf

            Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(expected.ToLowerInvariant)
            Dim actual = node.NormalizeWhitespace(indentation:="  ", useDefaultCasing:=True).ToFullString()
            Assert.Equal(expected, actual)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestComment()

            ' trailing whitespace will be aligned to the indent level (see second comment)
            ' when determining if there should be a separator between tokens, the current algorithm does not check if a token comes out 
            ' of structured trivia. Because of that there is now a space at the end of structured trivia before the next token
            ' whitespace before comments get reduced to one (see comment after code), whitespace in trivia is maintained (see same comment)
            ' xml doc comments somehow contain \n in the XmlTextLiterals ... will not spend time to work around
            Dim input = "Module m1" + vbCrLf +
                            "  ' a nice comment" + vbCrLf +
                            "  ''' even more comments" + vbCrLf +
                            "' and more comments " + vbCrLf +
                            "#if false   " + vbCrLf +
                            " whatever? " + vbCrLf +
                            "#end if" + vbCrLf +
                            "' and more comments" + vbCrLf +
                            " ''' structured trivia before code" + vbCrLf +
                            "Dim x = Function(x, y) x + y      '  trivia after code" + vbCrLf +
                            "End Module"

            Dim expected = "Module m1" + vbCrLf + vbCrLf +
                            "  ' a nice comment" + vbCrLf +
                            "  ''' even more comments" + vbCrLf +
                            vbCrLf +
                            "  ' and more comments " + vbCrLf +
                            "#if false" + vbCrLf +
                            " whatever? " + vbCrLf +
                            "#end if" + vbCrLf +
                            "  ' and more comments" + vbCrLf +
                            "  ''' structured trivia before code" + vbCrLf +
                            "  Dim x = Function(x, y) x + y '  trivia after code" + vbCrLf +
                            "End Module" + vbCrLf

            Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(input)
            Dim actual = node.NormalizeWhitespace("  ").ToFullString()
            Assert.Equal(expected, actual)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestMultilineLambdaFunctionsAsParameter()

            ' trailing whitespace will be aligned to the indent level (see second comment)
            ' when determining if there should be a separator between tokens, the current algorithm does not check if a token comes out 
            ' of structured trivia. Because of that there is now a space at the end of structured trivia before the next token
            ' whitespace before comments get reduced to one (see comment after code), whitespace in trivia is maintained (see same comment)
            ' xml doc comments somehow contain \n in the XmlTextLiterals ... will not spend time to work around
            Dim input = "Module m1" + vbCrLf +
                        "Sub Main(args As String())" + vbCrLf +
                        "Sub1(Function(p As Integer)" + vbCrLf +
                        "Sub2()" + vbCrLf +
                        "End Function)" + vbCrLf +
                        "End Sub" + vbCrLf +
                        "End Module"

            Dim expected = "Module m1" + vbCrLf + vbCrLf +
                            "  Sub Main(args As String())" + vbCrLf +
                            "    Sub1(Function(p As Integer)" + vbCrLf +
                            "      Sub2()" + vbCrLf +
                            "    End Function)" + vbCrLf +
                            "  End Sub" + vbCrLf +
                            "End Module" + vbCrLf

            Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(input)
            Dim actual = node.NormalizeWhitespace("  ").ToFullString()
            Assert.Equal(expected, actual)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestProperty()
            Dim input = <text>Property    p   As  Integer         
                Get
            End     Get

Set (   value	As	Integer )
    End	Set
            End	Property</text>.Value.Replace(vbLf, vbCrLf)

            Dim expected = <text>Property p As Integer
  Get
  End Get

  Set(value As Integer)
  End Set
End Property
</text>.Value.Replace(vbLf, vbCrLf)
            TestFormatBlock(input, expected)
        End Sub

        Private Sub TestFormatStatement(text As String, expected As String)
            Dim node As StatementSyntax = SyntaxFactory.ParseExecutableStatement(text)
            Dim actual = node.NormalizeWhitespace("  ").ToFullString()
            Assert.Equal(expected, actual)
        End Sub

        Private Sub TestFormatBlock(text As String, expected As String)
            Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(text)
            Dim actual = node.NormalizeWhitespace("  ").ToFullString()
            expected = expected.Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf) ' in case tests use XML literals

            Assert.Equal(expected, actual)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestStructuredTriviaAndAttributes()
            Dim source = "Module m1" + vbCrLf +
                            " '''<x>...</x>" + vbCrLf +
                            "  <foo()>" + vbCrLf +
                            "  sub a()" + vbCrLf +
                            "  end sub" + vbCrLf +
                            "End Module" + vbCrLf + vbCrLf

            Dim expected = "Module m1" + vbCrLf + vbCrLf +
                            "  '''<x>...</x>" + vbCrLf +
                            "  <foo()>" + vbCrLf +
                            "  sub a()" + vbCrLf +
                            "  end sub" + vbCrLf +
                            "End Module" + vbCrLf

            Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(source)
            Dim actual = node.NormalizeWhitespace(indentation:="  ", useDefaultCasing:=False).ToFullString()
            Assert.Equal(expected, actual)
        End Sub

        <Fact(), WorkItem(531607, "DevDiv")>
        Public Sub TestNestedStructuredTrivia()
            Dim trivia = SyntaxFactory.TriviaList(
                SyntaxFactory.Trivia(
                    SyntaxFactory.ConstDirectiveTrivia(
                            "constant",
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(1).WithTrailingTrivia(SyntaxFactory.Trivia(SyntaxFactory.SkippedTokensTrivia(SyntaxFactory.TokenList(SyntaxFactory.Literal("A"c)))))))))

            Dim expected = "#Const constant = 1 ""A""c"

            Dim actual = SyntaxFormatter.Format(trivia, "  ", useElasticTrivia:=False, useDefaultCasing:=False).ToFullString()
            Assert.Equal(expected, actual)
        End Sub

        <Fact()>
        Public Sub TestCrefAttribute()
            Dim source = "''' <summary>" + vbCrLf +
                         "''' <see cref  = """"/>" + vbCrLf +
                         "''' <see cref   =""""/>" + vbCrLf +
                         "''' <see cref= """"/>" + vbCrLf +
                         "''' <see cref=""""/>" + vbCrLf +
                         "''' <see cref  = ""1""/>" + vbCrLf +
                         "''' <see cref   =""a""/>" + vbCrLf +
                         "''' <see cref= ""Integer()""/>" + vbCrLf +
                         "''' <see cref   = ""a()""/>" + vbCrLf +
                         "''' </summary>" + vbCrLf +
                         "Module Program" + vbCrLf +
                         "End Module" + vbCrLf

            Dim expected = "''' <summary>" + vbCrLf +
                           "''' <see cref=""""/> " + vbCrLf +
                           "''' <see cref=""""/> " + vbCrLf +
                           "''' <see cref=""""/> " + vbCrLf +
                           "''' <see cref=""""/> " + vbCrLf +
                           "''' <see cref=""1""/> " + vbCrLf +
                           "''' <see cref=""a""/> " + vbCrLf +
                           "''' <see cref=""Integer()""/> " + vbCrLf +
                           "''' <see cref=""a()""/> " + vbCrLf +
                           "''' </summary>" + vbCrLf +
                           "Module Program" + vbCrLf +
                           "End Module" + vbCrLf

            Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(source)
            Dim actual = node.NormalizeWhitespace(indentation:="  ", useDefaultCasing:=False).ToFullString()
            Assert.Equal(expected, actual)
        End Sub

        <Fact()>
        Public Sub TestNameAttribute()
            Dim source = "''' <summary>" + vbCrLf +
                         "''' <paramref name  = """"/>" + vbCrLf +
                         "''' <paramref name   =""""/>" + vbCrLf +
                         "''' <paramref name= """"/>" + vbCrLf +
                         "''' <paramref name=""""/>" + vbCrLf +
                         "''' <paramref name  = ""1""/>" + vbCrLf +
                         "''' <paramref name   =""a""/>" + vbCrLf +
                         "''' <paramref name= ""Integer()""/>" + vbCrLf +
                         "''' <paramref name   = ""a()""/>" + vbCrLf +
                         "''' </summary>" + vbCrLf +
                         "Module Program" + vbCrLf +
                         "End Module" + vbCrLf

            Dim expected = "''' <summary>" + vbCrLf +
                           "''' <paramref name=""""/> " + vbCrLf +
                           "''' <paramref name=""""/> " + vbCrLf +
                           "''' <paramref name=""""/> " + vbCrLf +
                           "''' <paramref name=""""/> " + vbCrLf +
                           "''' <paramref name=""1""/> " + vbCrLf +
                           "''' <paramref name=""a""/> " + vbCrLf +
                           "''' <paramref name=""Integer()""/> " + vbCrLf +
                           "''' <paramref name=""a()""/> " + vbCrLf +
                           "''' </summary>" + vbCrLf +
                           "Module Program" + vbCrLf +
                           "End Module" + vbCrLf

            Dim node As CompilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(source)
            Dim actual = node.NormalizeWhitespace(indentation:="  ", useDefaultCasing:=False).ToFullString()
            Assert.Equal(expected, actual)
        End Sub

        <Fact>
        Public Sub TestEnableWarningDirective()
            Dim text = <![CDATA[         #  enable           warning[BC000],Bc123,             BC456,_789'          comment
# enable   warning
# enable   warning ,]]>.Value.Replace(vbLf, vbCrLf)

            Dim root = Parse(text).GetRoot()
            Dim formattedRoot = SyntaxFormatter.Format(root, "    ", useElasticTrivia:=True, useDefaultCasing:=True)

            Dim expected = <![CDATA[#Enable Warning [BC000], Bc123, BC456, _789 '          comment
#Enable Warning
#Enable Warning ,
]]>.Value.Replace(vbLf, vbCrLf)

            Assert.Equal(expected, formattedRoot.ToFullString())
        End Sub

        <Fact>
        Public Sub TestDisableWarningDirective()
            Dim text = <![CDATA[Module Program
#   disable warning
    Sub Main()
        #disable       warning          bc123,            Bc456,BC789
    End Sub
#   disable   warning[BC123],   '   Comment
End Module]]>.Value.Replace(vbLf, vbCrLf)

            Dim root = Parse(text).GetRoot()
            Dim formattedRoot = SyntaxFormatter.Format(root, "    ", useElasticTrivia:=True, useDefaultCasing:=True)

            Dim expected = <![CDATA[Module Program

#Disable Warning
    Sub Main()
#Disable Warning bc123, Bc456, BC789
    End Sub
#Disable Warning [BC123], '   Comment
 End Module
]]>.Value.Replace(vbLf, vbCrLf)

            Assert.Equal(expected, formattedRoot.ToFullString())
        End Sub
    End Class
End Namespace