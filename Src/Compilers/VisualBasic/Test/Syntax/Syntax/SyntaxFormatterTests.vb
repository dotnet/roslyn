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
            TestFormatBlock("module m1" + vbCrLf + _
                            "sub s1()" + vbCrLf + _
                            "Dim x as Integer()" + vbCrLf + _
                            "x(2)=23" + vbCrLf + _
                            "Dim s as string=""boo""&""ya""" + vbCrLf + _
                            "end sub" + vbCrLf + _
                            "end module", _
 _
                            "module m1" + vbCrLf + vbCrLf + _
                            "  sub s1()" + vbCrLf + _
                            "    Dim x as Integer()" + vbCrLf + _
                            "    x(2) = 23" + vbCrLf + _
                            "    Dim s as string = ""boo"" & ""ya""" + vbCrLf + _
                            "  end sub" + vbCrLf + _
                            "end module" + vbCrLf)

            TestFormatBlock("module m1" + vbCrLf + vbCrLf + _
                            "sub s1()" + vbCrLf + _
                            "Dim x as Integer" + vbCrLf + _
                            "x^=23" + vbCrLf + _
                            "x*=23" + vbCrLf + _
                            "x/=23" + vbCrLf + _
                            "x\=23" + vbCrLf + _
                            "x+=23" + vbCrLf + _
                            "x-=23" + vbCrLf + _
                            "x<<=23" + vbCrLf + _
                            "x>>=23" + vbCrLf + _
                            "Dim y as string" + vbCrLf + _
                            "y &=""a""" + vbCrLf + _
                            "end sub" + vbCrLf + _
                            "end module", _
 _
                            "module m1" + vbCrLf + vbCrLf + _
                            "  sub s1()" + vbCrLf + _
                            "    Dim x as Integer" + vbCrLf + _
                            "    x ^= 23" + vbCrLf + _
                            "    x *= 23" + vbCrLf + _
                            "    x /= 23" + vbCrLf + _
                            "    x \= 23" + vbCrLf + _
                            "    x += 23" + vbCrLf + _
                            "    x -= 23" + vbCrLf + _
                            "    x <<= 23" + vbCrLf + _
                            "    x >>= 23" + vbCrLf + _
                            "    Dim y as string" + vbCrLf + _
                            "    y &= ""a""" + vbCrLf + _
                            "  end sub" + vbCrLf + _
                            "end module" + vbCrLf)

            TestFormatBlock("module m1" + vbCrLf + _
                            "sub s1()" + vbCrLf + _
                            "Dim s1 As String=""a""" + vbCrLf + _
                            "Dim s2 As String=""b""" + vbCrLf + _
                            "Mid$(s1,3,3)=s2" + vbCrLf + _
                            "end sub" + vbCrLf + _
                            "end module", _
 _
                            "module m1" + vbCrLf + vbCrLf + _
                            "  sub s1()" + vbCrLf + _
                            "    Dim s1 As String = ""a""" + vbCrLf + _
                            "    Dim s2 As String = ""b""" + vbCrLf + _
                            "    Mid$(s1, 3, 3) = s2" + vbCrLf + _
                            "  end sub" + vbCrLf + _
                            "end module" + vbCrLf)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestCallStatements()
            TestFormatBlock("module m1" + vbCrLf + _
                            "sub s2()" + vbCrLf + _
                            "s1 ( 23 )" + vbCrLf + _
                            "s1 ( p1:=23 , p2:=23)" + vbCrLf + _
                            "end sub" + vbCrLf + _
                            "end module", _
 _
                            "module m1" + vbCrLf + vbCrLf + _
                            "  sub s2()" + vbCrLf + _
                            "    s1(23)" + vbCrLf + _
                            "    s1(p1:=23, p2:=23)" + vbCrLf + _
                            "  end sub" + vbCrLf + _
                            "end module" + vbCrLf)

            TestFormatBlock("module m1" + vbCrLf + vbCrLf + _
                            "sub s2 ( Of   T ) (   optional x As T=nothing  )" + vbCrLf + _
                            "N1.M2.S2 ( ) " + vbCrLf + _
                            "end sub" + vbCrLf + _
                            "end module", _
 _
                            "module m1" + vbCrLf + vbCrLf + _
                            "  sub s2(Of T)(optional x As T = nothing)" + vbCrLf + _
                            "    N1.M2.S2()" + vbCrLf + _
                            "  end sub" + vbCrLf + _
                            "end module" + vbCrLf)
        End Sub

        <Fact()>
        Public Sub TestNewStatements()
            TestFormatBlock("Dim zipState=New With {   Key .ZipCode=98112, .State=""WA""   }", _
                            "Dim zipState = New With {Key .ZipCode = 98112, .State = ""WA""}" + vbCrLf)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv"), WorkItem(546514, "DevDiv")>
        Public Sub TestXmlAccessStatements()
            TestFormatBlock("Imports <xmlns:db=""http://example.org/database"">" + vbCrLf + _
                            "Module Test" + vbCrLf + _
                            "Sub Main ( )" + vbCrLf + _
                            "Dim x=<db:customer><db:Name>Bob</db:Name></db:customer>" + vbCrLf + _
                            "Console . WriteLine ( x .<   db:Name  > )" + vbCrLf + _
                            "End Sub" + vbCrLf + _
                            "End Module",
 _
                            "Imports <xmlns:db=""http://example.org/database"">" + vbCrLf + _
                            "" + vbCrLf + _
                            "Module Test" + vbCrLf + vbCrLf + _
                            "  Sub Main()" + vbCrLf + _
                            "    Dim x = <db:customer><db:Name>Bob</db:Name></db:customer>" + vbCrLf + _
                            "    Console.WriteLine(x.<db:Name>)" + vbCrLf + _
                            "  End Sub" + vbCrLf + _
                            "End Module" + vbCrLf)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestNamespaceStatements()
            TestFormatBlock("Imports I1.I2" + vbCrLf + _
                            "Namespace N1" + vbCrLf + _
                            "Namespace N2.N3" + vbCrLf + _
                            "end Namespace" + vbCrLf + _
                            "end Namespace", _
 _
                            "Imports I1.I2" + vbCrLf + _
                            "" + vbCrLf + _
                            "Namespace N1" + vbCrLf + _
                            "  Namespace N2.N3" + vbCrLf + _
                            "  end Namespace" + vbCrLf + _
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
            TestFormatBlock("namespace N1" + vbCrLf + _
                            "Interface I1" + vbCrLf + _
                            "public Function F1() As Object" + vbCrLf + _
                            "End Interface" + vbCrLf + _
                            "Interface I2" + vbCrLf + _
                            "Function F2() As Integer" + vbCrLf + _
                            "End Interface" + vbCrLf + _
                            "Structure S1" + vbCrLf + _
                            "Implements I1,I2" + vbCrLf + _
                            "public Function F1() As Object" + vbCrLf + _
                            "Dim x as Integer=23" + vbCrLf + _
                            "return x" + vbCrLf + _
                            "end function" + vbCrLf + _
                            "End Structure" + vbCrLf + _
                            "End Namespace", _
 _
                            "namespace N1" + vbCrLf + vbCrLf + _
                            "  Interface I1" + vbCrLf + vbCrLf + _
                            "    public Function F1() As Object" + vbCrLf + vbCrLf + _
                            "  End Interface" + vbCrLf + vbCrLf + _
                            "  Interface I2" + vbCrLf + vbCrLf + _
                            "    Function F2() As Integer" + vbCrLf + vbCrLf + _
                            "  End Interface" + vbCrLf + vbCrLf + _
                            "  Structure S1" + vbCrLf + _
                            "    Implements I1, I2" + vbCrLf + vbCrLf + _
                            "    public Function F1() As Object" + vbCrLf + _
                            "      Dim x as Integer = 23" + vbCrLf + _
                            "      return x" + vbCrLf + _
                            "    end function" + vbCrLf + _
                            "  End Structure" + vbCrLf + _
                            "End Namespace" + vbCrLf)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestEnumStatements()
            TestFormatBlock("Module M1" + vbCrLf + vbCrLf + _
                            "ENUM E1 as long" + vbCrLf + _
                            "         foo=23" + vbCrLf + _
                            "bar        " + vbCrLf + _
                            "boo=foo" + vbCrLf + _
                            "booya=1.4" + vbCrLf + _
                            "end enum" + vbCrLf + _
                            "end         MODule", _
 _
                            "Module M1" + vbCrLf + vbCrLf + _
                            "  ENUM E1 as long" + vbCrLf + _
                            "    foo = 23" + vbCrLf + _
                            "    bar" + vbCrLf + _
                            "    boo = foo" + vbCrLf + _
                            "    booya = 1.4" + vbCrLf + _
                            "  end enum" + vbCrLf + _
                            "end MODule" + vbCrLf)

            TestFormatBlock("class c1" + vbCrLf + _
                            "ENUM E1 as long" + vbCrLf + _
                            "         foo=23" + vbCrLf + _
                            "bar        " + vbCrLf + _
                            "boo=foo" + vbCrLf + _
                            "booya=1.4" + vbCrLf + _
                            "end enum" + vbCrLf + _
                            "end         class", _
 _
                            "class c1" + vbCrLf + vbCrLf + _
                            "  ENUM E1 as long" + vbCrLf + _
                            "    foo = 23" + vbCrLf + _
                            "    bar" + vbCrLf + _
                            "    boo = foo" + vbCrLf + _
                            "    booya = 1.4" + vbCrLf + _
                            "  end enum" + vbCrLf + _
                            "end class" + vbCrLf)

            TestFormatBlock("public class c1" + vbCrLf + vbCrLf + _
                            "ENUM E1 as long" + vbCrLf + _
                            "         foo=23" + vbCrLf + _
                            "bar        " + vbCrLf + _
                            "boo=foo" + vbCrLf + _
                            "booya=1.4" + vbCrLf + _
                            "end enum" + vbCrLf + _
                            "end         class", _
 _
                            "public class c1" + vbCrLf + vbCrLf + _
                            "  ENUM E1 as long" + vbCrLf + _
                            "    foo = 23" + vbCrLf + _
                            "    bar" + vbCrLf + _
                            "    boo = foo" + vbCrLf + _
                            "    booya = 1.4" + vbCrLf + _
                            "  end enum" + vbCrLf + _
                            "end class" + vbCrLf)

            TestFormatBlock("class c1" + vbCrLf + _
                            "public     ENUM E1 as long" + vbCrLf + _
                            "         foo=23" + vbCrLf + _
                            "bar        " + vbCrLf + _
                            "boo=foo" + vbCrLf + _
                            "booya=1.4" + vbCrLf + _
                            "end enum" + vbCrLf + _
                            "end         class", _
 _
                            "class c1" + vbCrLf + vbCrLf + _
                            "  public ENUM E1 as long" + vbCrLf + _
                            "    foo = 23" + vbCrLf + _
                            "    bar" + vbCrLf + _
                            "    boo = foo" + vbCrLf + _
                            "    booya = 1.4" + vbCrLf + _
                            "  end enum" + vbCrLf + _
                            "end class" + vbCrLf)
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestDelegateStatements()

            TestFormatBlock("Module M1" + vbCrLf + _
                            "Dim x=Function( x ,y )x+y" + vbCrLf + _
                            "Dim y As Func ( Of Integer ,Integer ,Integer )=x" + vbCrLf + _
                            "end MODule", _
 _
                            "Module M1" + vbCrLf + vbCrLf + _
                            "  Dim x = Function(x, y) x + y" + vbCrLf + vbCrLf + _
                            "  Dim y As Func(Of Integer, Integer, Integer) = x" + vbCrLf + _
                            "end MODule" + vbCrLf)

            TestFormatBlock("Module M1" + vbCrLf + _
                            "Dim x=Function( x ,y )" + vbCrLf + _
                            "return    x+y" + vbCrLf + _
                            "end function" + vbCrLf + _
                            "Dim y As Func ( Of Integer ,Integer ,Integer )=x" + vbCrLf + _
                            "end MODule", _
 _
                            "Module M1" + vbCrLf + vbCrLf + _
                            "  Dim x = Function(x, y)" + vbCrLf + _
                            "    return x + y" + vbCrLf + _
                            "  end function" + vbCrLf + vbCrLf + _
                            "  Dim y As Func(Of Integer, Integer, Integer) = x" + vbCrLf + _
                            "end MODule" + vbCrLf)

            TestFormatBlock("Module M1" + vbCrLf + _
                            "Dim x=Sub( x ,y )" + vbCrLf + _
                            "dim x as integer" + vbCrLf + _
                            "end sub" + vbCrLf + _
                            "Dim y As Action ( Of Integer ,Integer)=x" + vbCrLf + _
                            "end MODule", _
 _
                            "Module M1" + vbCrLf + vbCrLf + _
                            "  Dim x = Sub(x, y)" + vbCrLf + _
                            "    dim x as integer" + vbCrLf + _
                            "  end sub" + vbCrLf + _
                            vbCrLf + _
                            "  Dim y As Action(Of Integer, Integer) = x" + vbCrLf + _
                            "end MODule" + vbCrLf)

        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestSelectStatements()

            TestFormatBlock("    Module M1" + vbCrLf + _
                            "sub s1()" + vbCrLf + _
                            "select case foo" + vbCrLf + _
                            "case    23 " + vbCrLf + _
                            "return    foo       " + vbCrLf + _
                            "case    42,11 " + vbCrLf + _
                            "return    foo       " + vbCrLf + _
                            "case    > 100 " + vbCrLf + _
                            "return    foo       " + vbCrLf + _
                            "case   200 to  300 " + vbCrLf + _
                            "return    foo       " + vbCrLf + _
                            "case    12," + vbCrLf + _
                            "13" + vbCrLf + _
                            "return    foo       " + vbCrLf + _
                            "case else" + vbCrLf + _
                            "return   foo       " + vbCrLf + _
                            "end   select  " + vbCrLf + _
                            "end   sub  " + vbCrLf + _
                            "end   module  ", _
 _
                            "Module M1" + vbCrLf + vbCrLf + _
                            "  sub s1()" + vbCrLf + _
                            "    select case foo" + vbCrLf + _
                            "      case 23" + vbCrLf + _
                            "        return foo" + vbCrLf + _
                            "      case 42, 11" + vbCrLf + _
                            "        return foo" + vbCrLf + _
                            "      case > 100" + vbCrLf + _
                            "        return foo" + vbCrLf + _
                            "      case 200 to 300" + vbCrLf + _
                            "        return foo" + vbCrLf + _
                            "      case 12, 13" + vbCrLf + _
                            "        return foo" + vbCrLf + _
                            "      case else" + vbCrLf + _
                            "        return foo" + vbCrLf + _
                            "    end select" + vbCrLf + _
                            "  end sub" + vbCrLf + _
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

            TestFormatStatement("DO until a(2)<>12" + vbCrLf + _
                            "Dim x = 12" + vbCrLf + _
                            "   loop", _
 _
                            "DO until a(2) <> 12" + vbCrLf + _
                            "  Dim x = 12" + vbCrLf + _
                            "loop")

            TestFormatStatement("DO while a(2)<>12" + vbCrLf + _
                            "Dim x = 12" + vbCrLf + _
                            "   loop", _
 _
                            "DO while a(2) <> 12" + vbCrLf + _
                            "  Dim x = 12" + vbCrLf + _
                            "loop")

            TestFormatStatement("DO               " + vbCrLf + _
                            "Dim x = 12" + vbCrLf + _
                            "   loop", _
 _
                            "DO" + vbCrLf + _
                            "  Dim x = 12" + vbCrLf + _
                            "loop")

            TestFormatStatement("DO               " + vbCrLf + _
                            "Dim x = 12" + vbCrLf + _
                            "   loop until a ( 2 )  <>    12   ", _
 _
                            "DO" + vbCrLf + _
                            "  Dim x = 12" + vbCrLf + _
                            "loop until a(2) <> 12")

            TestFormatStatement("For     Each   i  In   x" + vbCrLf + _
                            "Dim x = 12" + vbCrLf + _
                            "   next", _
 _
                            "For Each i In x" + vbCrLf + _
                            "  Dim x = 12" + vbCrLf + _
                            "next")

            TestFormatStatement("For     Each   i  In   x" + vbCrLf + _
                                "For     Each   j  In   x" + vbCrLf + _
                                "Dim x = 12" + vbCrLf + _
                                "   next j,i", _
 _
                            "For Each i In x" + vbCrLf + _
                            "  For Each j In x" + vbCrLf + _
                            "    Dim x = 12" + vbCrLf + _
                            "next j, i")
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestExceptionsStatements()

            TestFormatStatement("   try" + vbCrLf + _
                            "dim x =23" + vbCrLf + _
                            "Catch  e1 As Exception When 1>2" + vbCrLf + _
                            "dim x =23" + vbCrLf + _
                            "Catch" + vbCrLf + _
                            "dim x =23" + vbCrLf + _
                            "finally" + vbCrLf + _
                            "dim x =23" + vbCrLf + _
                            " end try", _
 _
                            "try" + vbCrLf + _
                            "  dim x = 23" + vbCrLf + _
                            "Catch e1 As Exception When 1 > 2" + vbCrLf + _
                            "  dim x = 23" + vbCrLf + _
                            "Catch" + vbCrLf + _
                            "  dim x = 23" + vbCrLf + _
                            "finally" + vbCrLf + _
                            "  dim x = 23" + vbCrLf + _
                            "end try")
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestUsingStatements()

            TestFormatStatement("  Using   r1  As  R =  New R ( ) ,   r2 As R = New R( )" + vbCrLf + _
                            "dim x =23" + vbCrLf + _
                            "end using", _
 _
                            "Using r1 As R = New R(), r2 As R = New R()" + vbCrLf + _
                            "  dim x = 23" + vbCrLf + _
                            "end using")
        End Sub

        <Fact()>
        Public Sub TestQueryExpressions()

            TestFormatStatement("  Dim waCusts = _" + vbCrLf + _
                            "From cust As Customer In Customers _" + vbCrLf + _
                            "Where    cust.State    =  ""WA""", _
 _
                            "Dim waCusts = From cust As Customer In Customers Where cust.State = ""WA""")
        End Sub

        <Fact(), WorkItem(546397, "DevDiv")>
        Public Sub TestDefaultCasingForKeywords()
            Dim expected = "Module m1" + vbCrLf + vbCrLf + _
                            "  Dim x = Function(x, y) x + y" + vbCrLf + vbCrLf + _
                            "  Dim y As func(Of Integer, Integer, Integer) = x" + vbCrLf + _
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
            Dim input = "Module m1" + vbCrLf + _
                            "  ' a nice comment" + vbCrLf + _
                            "  ''' even more comments" + vbCrLf + _
                            "' and more comments " + vbCrLf + _
                            "#if false   " + vbCrLf + _
                            " whatever? " + vbCrLf + _
                            "#end if" + vbCrLf + _
                            "' and more comments" + vbCrLf + _
                            " ''' structured trivia before code" + vbCrLf + _
                            "Dim x = Function(x, y) x + y      '  trivia after code" + vbCrLf + _
                            "End Module"

            Dim expected = "Module m1" + vbCrLf + vbCrLf + _
                            "  ' a nice comment" + vbCrLf + _
                            "  ''' even more comments" + vbCrLf + _
                            vbCrLf + _
                            "  ' and more comments " + vbCrLf + _
                            "#if false" + vbCrLf + _
                            " whatever? " + vbCrLf + _
                            "#end if" + vbCrLf + _
                            "  ' and more comments" + vbCrLf + _
                            "  ''' structured trivia before code" + vbCrLf + _
                            "  Dim x = Function(x, y) x + y '  trivia after code" + vbCrLf + _
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
            Dim input = "Module m1" + vbCrLf + _
                        "Sub Main(args As String())" + vbCrLf + _
                        "Sub1(Function(p As Integer)" + vbCrLf + _
                        "Sub2()" + vbCrLf + _
                        "End Function)" + vbCrLf + _
                        "End Sub" + vbCrLf + _
                        "End Module"

            Dim expected = "Module m1" + vbCrLf + vbCrLf + _
                            "  Sub Main(args As String())" + vbCrLf + _
                            "    Sub1(Function(p As Integer)" + vbCrLf + _
                            "      Sub2()" + vbCrLf + _
                            "    End Function)" + vbCrLf + _
                            "  End Sub" + vbCrLf + _
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
            Dim source = "Module m1" + vbCrLf + _
                            " '''<x>...</x>" + vbCrLf + _
                            "  <foo()>" + vbCrLf + _
                            "  sub a()" + vbCrLf + _
                            "  end sub" + vbCrLf + _
                            "End Module" + vbCrLf + vbCrLf

            Dim expected = "Module m1" + vbCrLf + vbCrLf + _
                            "  '''<x>...</x>" + vbCrLf + _
                            "  <foo()>" + vbCrLf + _
                            "  sub a()" + vbCrLf + _
                            "  end sub" + vbCrLf + _
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

    End Class

End Namespace
