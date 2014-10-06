' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Partial Public Class FlowAnalysisTests
        Inherits FlowTestBase

        <Fact()>
        Public Sub NullArgsToFlowAnalysisMethods()
            Dim compilation = CreateCompilationWithMscorlib(
        <compilation name="TestEntryPoints01">
            <file name="a.b">
class C 
    public sub F()
    end sub
end class
</file>
        </compilation>)

            Dim semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim statement = compilation.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodesAndSelf().OfType(Of StatementSyntax)().First()

            Assert.Throws(Of ArgumentNullException)(Sub() semanticModel.AnalyzeControlFlow(statement, Nothing))
            Assert.Throws(Of ArgumentNullException)(Sub() semanticModel.AnalyzeControlFlow(Nothing, statement))
            Assert.Throws(Of ArgumentNullException)(Sub() semanticModel.AnalyzeDataFlow(statement, Nothing))
            Assert.Throws(Of ArgumentNullException)(Sub() semanticModel.AnalyzeDataFlow(Nothing, statement))
            Assert.Throws(Of ArgumentNullException)(Sub() semanticModel.AnalyzeDataFlow(CType(Nothing, ExecutableStatementSyntax)))
        End Sub

        <Fact()>
        Public Sub TestEntryPoints01()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestEntryPoints01">
          <file name="a.b">
class C 
    public sub F()
        goto L1 ' 1
[|
        L1: 
|]
        goto L1 ' 2
    end sub
end class
</file>
      </compilation>)
            Assert.Equal(1, analysis.EntryPoints.Count())
        End Sub

        <Fact()>
        Public Sub ByRefExtensionMethod()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation>
          <file name="a.b">
Option Strict Off
Imports System
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Module Module1
    Public Sub Main(args As String())
        Dim s As String = "b"
        [|s.EM()|]
        Console.Write(s)
    End Sub

    &lt;Extension()>
    Public Sub EM(ByRef c As String)
        c = "a"
    End Sub
End Module

Public Class Clazz
End Class
</file>
      </compilation>)

            Assert.True(analysis.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(768095, "DevDiv")>
        <Fact()>
        Public Sub Bug768095()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation>
          <file name="a.b">
Public Class RichTextBox2

    Private Sub UndoRestorableItem(ByVal restorableItem As RestorableItem)
        With [|restorableItem|]
        End With
    End Sub

    Private Structure RestorableItem
        Public Property EditType As String
        Public Property Position As Integer
        Public Property Text As String
    End Structure
End Class
</file>
      </compilation>)
            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(531223, "DevDiv")>
        <Fact()>
        Public Sub Bug17780a()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation>
          <file name="a.b">
Class Test
    Public Shared Sub Main()
        On Error GoTo [| 0 |]
    End Sub
End Class
</file>
      </compilation>)
            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(531223, "DevDiv")>
        <Fact()>
        Public Sub Bug17780b()
            ' TODO: Rewrite the test when Yield is supported
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation>
          <file name="a.b">
Imports System
Imports System.Collections.Generic

Friend Class SourceFileScope
        Public Iterator Function GetTypesToSearchIn() As IEnumerable(Of Object)
            Yield [| Nothing |]
        End Function
    End Class
</file>
      </compilation>)
            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(543362, "DevDiv")>
        <Fact()>
        Public Sub Bug11067()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug11067">
          <file name="a.b">
Class Test
    Public Shared Sub Main()
        Dim y(,) = New Integer(,) {{[|From|]}}
    End Sub
End Class
</file>
      </compilation>)
            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(529967, "DevDiv")>
        <Fact()>
        Public Sub Bug14894a()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation>
          <file name="a.b">
Imports System
Module Program
    Sub Main(args As String())
        Dim o3 As Object = "hi"
     [| Dim col1 = {o3, o3} |]
    End Sub
End Module
</file>
      </compilation>)
            Assert.True(analysis.Succeeded)
            Assert.Equal("o3", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal("args, o3", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
            Assert.Equal("col1", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("o3", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <WorkItem(529967, "DevDiv")>
        <Fact()>
        Public Sub Bug14894b()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation>
          <file name="a.b">
Imports System
Module Program
    Sub Main(args As String())
        Dim o3 As Object = "hi"
        Dim col1 = [| {o3, o3} |]
    End Sub
End Module
</file>
      </compilation>)
            Assert.True(analysis.Succeeded)
            Assert.Equal("o3", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
        End Sub

        <WorkItem(544602, "DevDiv")>
        <Fact()>
        Public Sub Bug13053a()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug13053a">
          <file name="a.b">
Class Test
    Public Shared Sub Main()
        Dim i As Integer = 1
        Dim o = New MyObject With { .A = [| i |] }
    End Sub
End Class
</file>
      </compilation>)
            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(545069, "DevDiv")>
        <Fact()>
        Public Sub ParameterNameAsAnInvalidRegion()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="ParameterNameAsAnInvalidRegion">
          <file name="a.b">
Module Module1
    Sub S(par As Integer)
        S([| par |]:=12)
    End Sub
End Module
</file>
      </compilation>)
            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(545443, "DevDiv")>
        <Fact()>
        Public Sub XmlNameInsideEndTag()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="XmlNameInsideEndTag">
          <file name="a.b">
Module Module1
    Sub S(par As Integer)
        Dim a = &lt;tag&gt; &lt;/ [| tag |] &gt;
    End Sub
End Module
</file>
      </compilation>)
            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(545077, "DevDiv")>
        <Fact()>
        Public Sub ExpressionsInAttributeValues()
            Dim analysis = CompileAndAnalyzeDataFlow(
<compilation name="ExpressionsInAttributeValues">
    <file name="a.b">
Imports System
Imports System.Reflection
&lt;Assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute([| "Microsoft.CodeAnalysis.Workspaces, PublicKey=002400000480000094000000060200000024000052534131000400"&amp; _ 
"000100010055e0217eb635f69281051f9a823e0c7edd90f28063eb6c7a742a19b4f6139778ee0af4"&amp; _ 
"38f47aed3b6e9f99838aa8dba689c7a71ddb860c96d923830b57bbd5cd6119406ddb9b002cf1c723"&amp; _ 
"bf272d6acbb7129e9d6dd5a5309c94e0ff4b2c884d45a55f475cd7dba59198086f61f5a8c8b5e601"&amp; _ 
"c0edbf269733f6f578fc8579c2" |])&gt;
    </file>
</compilation>)
            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(545077, "DevDiv")>
        <Fact()>
        Public Sub ExpressionsInAttributeValues2()
            Dim analysis = CompileAndAnalyzeDataFlow(
<compilation name="ExpressionsInAttributeValues2">
    <file name="a.b">
Imports System
Imports System.Reflection
Public Class MyAttribute
    Public Sub New(p As Object)
    End Sub
End Class

&lt;MyAttribute(p:=Sub()
                        [|Dim a As Integer = 1
                        While a &lt; 110
                            a += 1
                        End While|]
                   End Sub)&gt;
Module Program
    Sub Main(args As String())
    End Sub
End Module
    </file>
</compilation>)
            Assert.False(analysis.Succeeded)
        End Sub

        <Fact()>
        Public Sub OptionalParameterDefaultValue()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="OptionalParameterDefaultValue">
          <file name="a.b">
Class Test
    Public Shared Sub S(Optional x As Integer = [| 1 |])
    End Sub
End Class
</file>
      </compilation>)
            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(545432, "DevDiv")>
        <Fact()>
        Public Sub LowerBoundOfArrayDefinitionSize()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="LowerBoundOfArrayDefinitionSize">
          <file name="a.b">
Class Test
    Public Shared Sub S(x As Integer)
        Dim newTypeArguments([|0|] To x - 1) As String
    End Sub
End Class
</file>
      </compilation>)
            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(544602, "DevDiv")>
        <Fact()>
        Public Sub Bug13053b()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug13053b">
          <file name="a.b">
Imports System
Class Test
    Public Shared Sub Main()
        Console.Write(GetXmlNamespace([| ns |]))
    End Sub
End Class
</file>
      </compilation>)
            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(679765, "DevDiv")>
        <Fact()>
        Public Sub Bug679765a()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug13053b">
          <file name="a.b">
Imports System
Class Test
    Public Shared Sub Main()
        Console.Write([| "A" |] + "B" + "C" + "D")
    End Sub
End Class
</file>
      </compilation>)
            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(679765, "DevDiv")>
        <Fact()>
        Public Sub Bug679765b()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug13053b">
          <file name="a.b">
Imports System
Class Test
    Public Shared Sub Main()
        Console.Write([| "A" + "B" + "C" + "D" |] )
    End Sub
End Class
</file>
      </compilation>)
            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(679765, "DevDiv")>
        <Fact()>
        Public Sub Bug679765c()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug13053b">
          <file name="a.b">
Imports System
Class Test
    Public Shared Sub Main()
        Console.Write([| "A" + "B" + "C" |] + "D" )
    End Sub
End Class
</file>
      </compilation>)
            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(543570, "DevDiv")>
        <Fact()>
        Public Sub Bug11428()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug11428">
          <file name="a.b">
Module Program
    Sub Main(args As String())
    End Sub
End Module
Class C
    Public Sub Foo()
    End Sub
End Class
Class M
    Inherits C
    Public r As Double
    Public Overrides Sub Foo()
        Return [|MyBase.Total|] * r
    End Sub
End Class
          </file>
      </compilation>)
            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(543581, "DevDiv")>
        <Fact()>
        Public Sub Bug11440a()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug11440">
          <file name="a.b">
Imports System
Module Program    
    Sub Main(args As String())
        Dim lambda = Function(ByRef arg As Integer)
                         Return Function(ByRef arg1 As Integer)
                                    GoTo Label
                                    Dim arg2 As Integer = 2
Label:
                                    Return [| arg2 * arg1 |]
                                End Function                     
                     End Function
    End Sub
End Module
          </file>
      </compilation>)

            Assert.True(analysis.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.Captured))
            Assert.Equal("arg1, arg2", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("arg1, arg2", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("arg, arg1", GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("arg, arg1, arg2, args, lambda", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(543581, "DevDiv")>
        <Fact()>
        Public Sub Bug11440b()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug11440">
          <file name="a.b">
Imports System
Module Program    
    Sub Main(args As String())
        GoTo Label
        Dim arg2 As Integer = 2
Label:
        dim y = [| arg2 |]
    End Sub
End Module
          </file>
      </compilation>)

            Assert.True(analysis.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.Captured))
            Assert.Equal("arg2", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("arg2", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("arg2, args, y", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(544330, "DevDiv")>
        <Fact()>
        Public Sub Bug12609()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug12609">
          <file name="a.b">
Class A    
    Sub Foo(Optional i As Integer = [|1|])
    End Sub
End Class
          </file>
      </compilation>)
            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(542231, "DevDiv")>
        <Fact()>
        Public Sub TestUnreachableRegion()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation>
          <file name="a.b">
Class A    
    Sub Foo()
        Dim i As Integer
        Return
        [| i = i + 1 |]
        Dim j As Integer = i
    End Sub
End Class
          </file>
      </compilation>)

            Assert.True(analysis.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <WorkItem(542231, "DevDiv")>
        <Fact()>
        Public Sub TestUnreachableRegion2()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation>
          <file name="a.b">
Class A    
    Sub Foo()
        Dim i As Integer = 0
        Dim j As Integer = 0
        Dim k As Integer = 0
        Dim l As Integer = 0
        GoTo l1

        [|
        Console.WriteLine(i)
        j = 1
l1:
        Console.WriteLine(j)
        k = 1
        GoTo l2

        Console.WriteLine(k)
        l = 1
l3:
        Console.WriteLine(l)
        i = 1
        |]

l2:
        Console.WriteLine(i + j + k + l)
        GoTo l3
    End Sub
End Class
          </file>
      </compilation>)

            Assert.True(analysis.Succeeded)
            Assert.Equal("j, l", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("i, k", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <WorkItem(542231, "DevDiv")>
        <Fact()>
        Public Sub TestUnreachableRegionInExpression()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation>
          <file name="a.b">
Class A    
    Function Foo() As Boolean
        Dim i As Boolean = True
        Dim j As Boolean = False
        dim ext as external = new external
        Return False AndAlso [| ((i = ext.M1(i)) Or (i = ext.M1(j))) |]
    End Function 
End Class
          </file>
      </compilation>, customIL)

            Assert.True(analysis.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <WorkItem(545445, "DevDiv")>
        <Fact()>
        Public Sub ExpressionInside()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug12609">
          <file name="a.b">
Class A    
    Sub Foo()
        Dim outputAuthoringDocument = &lt;?xml version=[|"1.0"|]?&gt;
              &lt;wix:Wix&gt;
                  &lt;wix:Fragment&gt;
                      &lt;wix:DirectoryRef Id="VisualStudio11Extensions"&gt;
                      &lt;/wix:DirectoryRef&gt;
                  &lt;/wix:Fragment&gt;
              &lt;/wix:Wix&gt;
    End Sub
End Class
          </file>
      </compilation>)
            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(544201, "DevDiv")>
        <Fact()>
        Public Sub Bug12423a()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug12423a">
          <file name="a.b">
Class A    
    Sub Foo()
        Dim x = { [| New B (abc) |] }
    End Sub
End Class
Class B
    Public Sub New(i As Integer)
    End Sub
End Class
          </file>
      </compilation>)
            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(544201, "DevDiv")>
        <Fact()>
        Public Sub Bug12423b()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug12423b">
          <file name="a.b">
Class A    
    Sub Foo(i As Integer)
        Dim x = New B([| i |] ) { New B (abc) }
    End Sub
End Class
Class B
    Public Sub New(i As Integer)
    End Sub
End Class
          </file>
      </compilation>)
            Assert.True(analysis.Succeeded)
        End Sub

        <Fact()>
        Public Sub TestDataFlowForValueTypes()

            ' WARNING: test matches the same test in C# (TestDataFlowForValueTypes)
            '          Keep the two tests in synch!

            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowForValueTypes">
          <file name="a.b">
Imports System

Class Tst
    Shared Sub Tst()
        Dim a As S0
        Dim b As S1
        Dim c As S2
        Dim d As S3
        Dim e As E0
        Dim f As E1

[|
        Console.WriteLine(a)
        Console.WriteLine(b)
        Console.WriteLine(c)
        Console.WriteLine(d)
        Console.WriteLine(e)
        Console.WriteLine(f)
|]
    End Sub
End Class


Structure S0
End Structure

Structure S1
    Public s0 As S0
End Structure

Structure S2
    Public s0 As S0
    Public s1 As Integer
End Structure

Structure S3
    Public s0 As S0
    Public s1 As Object
End Structure

Enum E0
End Enum

Enum E1
    V1
End Enum
</file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.Captured))
            Assert.Equal("c, d, e, f", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("a, b, c, d, e, f", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(768094, "DevDiv")>
        <Fact()>
        Public Sub Bug768094a()
            Dim program =
<compilation>
    <file name="a.b">
        <![CDATA[
class C 
    public sub F1(dim x as integer)
    end sub

        ' During the search for the best function definition, this procedure
    ** is called to test how well the function passed as the first argument
    ** matches the request for a function with nArg arguments in a system
    ** that uses encoding enc. The value returned indicates how well the
    ** request is matched. A higher value indicates a better match.
    **
    ** The returned value is always between 0 and 6, as follows:
    **
    ** 0: Not a match, or if nArg<0 and the function is has no implementation.
    ** 1: A variable arguments function that prefers UTF-8 when a UTF-16
    **    encoding is requested, or vice versa.
    ** 2: A variable arguments function that uses UTF-16BE when UTF-16LE is
    **    requested, or vice versa.
    ** 3: A variable arguments function using the same text encoding.
    ** 4: A function with the exact number of arguments requested that
    **    prefers UTF-8 when a UTF-16 encoding is requested, or vice versa.
    ** 5: [|A function|] with the exact number of arguments requested that
    **    prefers UTF-16LE when UTF-16BE is requested, or vice versa.
    ** 6: An exact match.
    **
    */
    public sub F(dim x as integer)
    end sub
end class
]]>
    </file>
</compilation>

            Dim startNodes As New List(Of VBSyntaxNode)
            Dim endNodes As New List(Of VBSyntaxNode)
            Dim comp = CompileAndGetModelAndSpan(program, startNodes, endNodes, Nothing, Nothing)

            Assert.Equal(3, startNodes.Count)
            Assert.Equal(SyntaxKind.ExpressionStatement, startNodes(2).Kind)

            Dim expr = DirectCast(startNodes(2), ExpressionStatementSyntax)
            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim analysis = model.AnalyzeControlFlow(expr) ' NO THROW

            Assert.Equal(0, analysis.EntryPoints.Count())
        End Sub

        <WorkItem(768094, "DevDiv")>
        <Fact()>
        Public Sub Bug768094b()
            Dim program =
<compilation>
    <file name="a.b">
        <![CDATA[
'CONVERTED BY ROSLYN
Imports System.Diagnostics
Imports System.Text
Imports i64 = System.Int64
Imports u8 = System.Byte
Imports u16 = System.UInt16

Namespace Community.CsharpSqlite

    Partial Public Class Sqlite3


#If SQLITE_DEBUG Then
    /*
** Write a nice string representation of the contents of cell pMem
** into buffer zBuf, length nBuf.
*/
    static StringBuilder zCsr = new StringBuilder( 100 );
    static void sqlite3VdbeMemPrettyPrint( Mem pMem, StringBuilder zBuf )
    {
      zBuf.Length = 0;
      zCsr.Length = 0;
      int f = pMem.flags;

      string[] encnames = new string[] { "(X)", "(8)", "(16LE)", "(16BE)" };

      if ( ( f & MEM_Blob ) != 0 )
      {
        int i;
        char c;
        if ( ( f & MEM_Dyn ) != 0 )
        {
          c = 'z';
          Debug.Assert( ( f & ( MEM_Static | MEM_Ephem ) ) == 0 );
        }
        else if ( ( f & MEM_Static ) != 0 )
        {
          c = 't';
          Debug.Assert( ( f & ( MEM_Dyn | MEM_Ephem ) ) == 0 );
        }
        else if ( ( f & MEM_Ephem ) != 0 )
        {
          c = 'e';
          Debug.Assert( ( f & ( MEM_Static | MEM_Dyn ) ) == 0 );
        }
        else
        {
          c = 's';
        }

        sqlite3_snprintf( 100, zCsr, "%c", c );
        zBuf.Append( zCsr );//zCsr += sqlite3Strlen30(zCsr);
        sqlite3_snprintf( 100, zCsr, "%d[", pMem.n );
        zBuf.Append( zCsr );//zCsr += sqlite3Strlen30(zCsr);
        for ( i = 0; i < 16 && i < pMem.n; i++ )
        {
          sqlite3_snprintf( 100, zCsr, "%02X", ( (int)pMem.zBLOB[i] & 0xFF ) );
          zBuf.Append( zCsr );//zCsr += sqlite3Strlen30(zCsr);
        }
        for ( i = 0; i < 16 && i < pMem.n; i++ )
        {
          char z = (char)pMem.zBLOB[i];
          if ( z < 32 || z > 126 )
            zBuf.Append( '.' );//*zCsr++ = '.';
          else
            zBuf.Append( z );//*zCsr++ = z;
        }

        sqlite3_snprintf( 100, zCsr, "]%s", encnames[pMem.enc] );
        zBuf.Append( zCsr );//zCsr += sqlite3Strlen30(zCsr);
        if ( ( f & MEM_Zero ) != 0 )
        {
          sqlite3_snprintf( 100, zCsr, "+%dz", pMem.u.nZero );
          zBuf.Append( zCsr );//zCsr += sqlite3Strlen30(zCsr);
        }
        //*zCsr = '\0';
      }
      else if ( ( f & MEM_Str ) != 0 )
      {
        int j;//, k;
        zBuf.Append( ' ' );
        if ( ( f & MEM_Dyn ) != 0 )
        {
          zBuf.Append( 'z' );
          Debug.Assert( ( f & ( MEM_Static | MEM_Ephem ) ) == 0 );
        }
        else if ( ( f & MEM_Static ) != 0 )
        {
          zBuf.Append( 't' );
          Debug.Assert( ( f & ( MEM_Dyn | MEM_Ephem ) ) == 0 );
        }
        else if ( ( f & MEM_Ephem ) != 0 )
        {
          zBuf.Append( 's' ); //zBuf.Append( 'e' );
          Debug.Assert( ( f & ( MEM_Static | MEM_Dyn ) ) == 0 );
        }
        else
        {
          zBuf.Append( 's' );
        }
        //k = 2;
        sqlite3_snprintf( 100, zCsr, "%d", pMem.n );//zBuf[k], "%d", pMem.n );
        zBuf.Append( zCsr );
        //k += sqlite3Strlen30( &zBuf[k] );
        zBuf.Append( '[' );// zBuf[k++] = '[';
        for ( j = 0; j < 15 && j < pMem.n; j++ )
        {
          u8 c = [|pMem.z|] != null ? (u8)pMem.z[j] : pMem.zBLOB[j];
          if ( c >= 0x20 && c < 0x7f )
          {
            zBuf.Append( (char)c );//zBuf[k++] = c;
          }
          else
          {
            zBuf.Append( '.' );//zBuf[k++] = '.';
          }
        }
        zBuf.Append( ']' );//zBuf[k++] = ']';
        sqlite3_snprintf( 100, zCsr, encnames[pMem.enc] );//& zBuf[k], encnames[pMem.enc] );
        zBuf.Append( zCsr );
        //k += sqlite3Strlen30( &zBuf[k] );
        //zBuf[k++] = 0;
      }
    }
#End If
    End Class
End Namespace
]]>
    </file>
</compilation>

            Dim startNodes As New List(Of VBSyntaxNode)
            Dim endNodes As New List(Of VBSyntaxNode)
            Dim comp = CompileAndGetModelAndSpan(program, startNodes, endNodes, Nothing, Nothing,
                                                 parseOptions:=
                                                    VBParseOptions.Default.WithPreprocessorSymbols(
                                                        KeyValuePair.Create("SQLITE_DEBUG", CObj(True))))

            Assert.Equal(4, startNodes.Count)
            Assert.Equal(SyntaxKind.DictionaryAccessExpression, startNodes(2).Kind)

            Dim expr = DirectCast(startNodes(2), MemberAccessExpressionSyntax)
            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim analysis = model.AnalyzeDataFlow(expr) ' NO THROW
            analysis = model.AnalyzeDataFlow(expr.Expression) ' NO THROW
            analysis = model.AnalyzeDataFlow(expr.Name) ' NO THROW
        End Sub

        <WorkItem(768094, "DevDiv")>
        <Fact()>
        Public Sub Bug768094c()
            Dim program =
<compilation>
    <file name="a.b">
        <![CDATA[
Imports u32 = System.UInt32

Class Clazz
    u32 aFrameCksum([|2|]) = {0, 0}
End Class
]]>
    </file>
</compilation>

            Dim startNodes As New List(Of VBSyntaxNode)
            Dim endNodes As New List(Of VBSyntaxNode)
            Dim comp = CompileAndGetModelAndSpan(program, startNodes, endNodes, Nothing, Nothing)

            Assert.Equal(2, startNodes.Count)
            Assert.Equal(SyntaxKind.NumericLiteralExpression, startNodes(0).Kind)

            Dim expr = DirectCast(startNodes(0), ExpressionSyntax)
            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim analysis = model.AnalyzeDataFlow(expr) ' NO THROW
        End Sub

        <WorkItem(768094, "DevDiv")>
        <Fact()>
        Public Sub Bug768094d()
            Dim program =
<compilation>
    <file name="a.b">
        <![CDATA[
Imports u32 = System.UInt32

Class Clazz
    u32 aFrameCksum([|2|])
End Class
]]>
    </file>
</compilation>

            Dim startNodes As New List(Of VBSyntaxNode)
            Dim endNodes As New List(Of VBSyntaxNode)
            Dim comp = CompileAndGetModelAndSpan(program, startNodes, endNodes, Nothing, Nothing)

            Assert.Equal(2, startNodes.Count)
            Assert.Equal(SyntaxKind.NumericLiteralExpression, startNodes(0).Kind)

            Dim expr = DirectCast(startNodes(0), ExpressionSyntax)
            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim analysis = model.AnalyzeDataFlow(expr) ' NO THROW
        End Sub

        <Fact()>
        Public Sub TestExitPoints01()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestExitPoints01">
          <file name="a.b">                
class C 
    public sub F(dim x as integer)
        L1:  ' 1
[|
        if x = 0 then goto L1
        if x = 1 then goto L2
        if x = 3 then goto L3
        L3: 
|]
        L2:  ' 2
    end sub
end class
</file>
      </compilation>)
            Assert.Equal(2, analysis.ExitPoints.Count())
        End Sub

        <Fact()>
        Public Sub TestRegionCompletesNormally01()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestRegionCompletesNormally01">
          <file name="a.b">  
class C 
    public sub F(x as integer)
[|
        goto L1
|]
        L1: 
    end sub
end class
</file>
      </compilation>)
            Assert.True(analysis.StartPointIsReachable)
            Assert.False(analysis.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestRegionCompletesNormally02()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestRegionCompletesNormally02">
          <file name="a.b">
class C 
    public sub F(x as integer)
[|
        x = 2
|]
    end sub
end class
</file>
      </compilation>)
            Assert.True(analysis.StartPointIsReachable)
            Assert.True(analysis.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestRegionCompletesNormally03()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestRegionCompletesNormally03">
          <file name="a.b">
class C 
    public sub F(x as integer)
[|
        if x = 0 then return
|]
    end sub
end class
</file>
      </compilation>)
            Assert.True(analysis.StartPointIsReachable)
            Assert.True(analysis.EndPointIsReachable)
        End Sub

        <WorkItem(543320, "DevDiv")>
        <Fact()>
        Public Sub Bug10987()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug10987">
          <file name="a.b">
Class Test
    Public Shared Sub Main()
        Dim y(1, 2) = [|New Integer|]
    End Sub
End Class
</file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestExpressionInIfStatement()
            Dim dataFlowAnalysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestExpressionInIfStatement">
          <file name="a.b">
Module Program
    Sub Main()
        Dim x = 1
        If 1 = [|x|] Then 
        End If
    End Sub
End Module
  </file>
      </compilation>)

            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsIn))
        End Sub

        <Fact()>
        Public Sub CallingMethodsOnUninitializedStructs()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="CallingMethodsOnUninitializedStructs2">
          <file name="a.b">
Public Structure XXX
    Public x As S(Of Object)
    Public y As S(Of String)
End Structure

Public Structure S(Of T)
    Public x As String
    Public Property y As T
End Structure

Public Class Test
    Public Shared Sub Main(args As String())
        Dim s As XXX
        s.x = New S(Of Object)()
        [|s.x.y.ToString()|]
        Dim t As Object = s
    End Sub
    Public Shared Sub S1(ByRef arg As XXX)
        arg.x.x = ""
        arg.x.y = arg.x.x
    End Sub
End Class
        </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.Captured))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("args, s, t", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(542789, "DevDiv")>
        <Fact()>
        Public Sub Bug10172()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="Bug10172">
          <file name="a.b">
Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Module1
    Sub Main()
        Dim list = New Integer() {1, 2, 3, 4, 5, 6, 7, 8}
        Dim b = From i In list Where i > Function(i) As String
                                             [|Return i|]
                                         End Function.Invoke
    End Sub
End Module
</file>
      </compilation>)

            Assert.True(analysis.Item1.Succeeded)
            Assert.True(analysis.Item2.Succeeded)
        End Sub

        <WorkItem(543645, "DevDiv")>
        <Fact()>
        Public Sub Bug11526()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="Bug10172">
          <file name="a.b">
Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Module1
    Sub Main()
        Dim x = True
        Dim y = DateTime.Now
        [|
        Try
        Catch ex as Exception when x orelse y = #12:00:00 AM#
        End Try
        |]
    End Sub
End Module
</file>
      </compilation>)

            Assert.True(analysis.Item1.Succeeded)
            Assert.True(analysis.Item2.Succeeded)
        End Sub

        <WorkItem(543111, "DevDiv")>
        <Fact()>
        Public Sub Bug10683a()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug10683a">
          <file name="a.b">
Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Module1
    Sub Main()
        Dim x = New Integer() {}
        x.First([|Function(i As Integer, r As Integer) As Boolean
                    Return True
                End Function|])
    End Sub
End Module
</file>
      </compilation>)

            Assert.True(analysis.Succeeded)
        End Sub

        <Fact()>
        Public Sub TestArrayDeclaration01()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestArrayDeclaration01">
          <file name="a.b">
Module Program
    Sub Main(args As String())
        [|
        Dim x(5), y As Integer |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestPreprocessorSymbol()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestPreprocessorSymbol">
          <file name="a.b">
Module Program
    Sub Main()
        [| 
        Console.WriteLine() 
#Const X = 1
        Console.WriteLine() 
        |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
        End Sub

        <Fact()>
        Public Sub TestArrayDeclaration02()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestArrayDeclaration02">
          <file name="a.b">
Module Program
    Sub Main(args As String())
        [|If True Then Dim x(5), y As Integer |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestArrayDeclaration02_()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestArrayDeclaration02">
          <file name="a.b">
Module Program
    Sub Main(args As String())
        Dim b As Boolean = True
        [|If b Then Dim x(5), y As Integer |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestVariablesWithSameName()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestVariablesWithSameName">
          <file name="a.b">
Module Program
    Sub Main(args As String())
        [|If True Then Dim x = 1 Else Dim x = 1 |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestVariablesWithSameName2()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestVariablesWithSameName2">
          <file name="a.b">
Module Program
    Sub Main(args As String())
        Dim b As Boolean = false
        [|If b Then Dim x = 1 Else Dim x = 1 |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <WorkItem(540454, "DevDiv")>
        <Fact()>
        Public Sub TestDataFlowAnalysisWithErrorsInStaticContext()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestDataFlowAnalysisWithErrorsInStaticContext">
          <file name="a.b">
Class C
    Sub Foo()
    End Sub
    Shared Sub Bar()
        [|
        Foo() |]
    End Sub
End Class  </file>
      </compilation>)
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestImplicitReturnVariable()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestImplicitReturnVariable">
          <file name="a.b">
Module Program
    Function Foo() As Integer
        [|
        Foo = 1
        |]
    End Function
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.Equal("Foo", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("Foo", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestVariablesDeclared01()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestVariablesDeclared01">
          <file name="a.b">
class C 
    public sub F(x as integer)
        dim a as integer
[|
        dim b as integer
        dim x as integer, y = 1
        if true then
          dim z = "a" 
        end if
|]
        dim c as integer
    end sub
end class</file>
      </compilation>)
            Assert.Equal("b, x, y, z", GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
        End Sub

        <Fact()>
        Public Sub TestIfElseBranch()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Dim x, y, z
        [|
        If True
            x = 1
        ElseIf True
            y = 1
        Else
            z = 1
        End If
        |]
        Console.WriteLine(x + y + z)
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <Fact()>
        Public Sub TestIfElseBranch_()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Dim x, y, z
        Dim b As Boolean = True
        [|
        If b
            x = 1
        ElseIf b
            y = 1
        Else
            z = 1
        End If
        |]
        Console.WriteLine(x + y + z)
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal("x, y, z", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <Fact()>
        Public Sub TestIfElseBranchReachability01()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestIfElseBranchReachability01">
          <file name="a.b">
Imports System
Module Program
    Function Foo() As Integer
        Dim x, y
        If True Then x = 1 Else If True Then Return 1 Else [|Return 1|]
        Return x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal(1, analysis.ExitPoints.Count())
            Assert.Equal(0, analysis.EntryPoints.Count())
            Assert.False(analysis.StartPointIsReachable())
            Assert.False(analysis.EndPointIsReachable())
        End Sub

        <Fact()>
        Public Sub TestIfElseBranchReachability02()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestIfElseBranchReachability02">
          <file name="a.b">
Imports System
Module Program
    Function Foo() As Integer
        Dim x, y
        If True Then x = 1 Else [|If True Then Return 1 Else Return 1|]
        Return x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal(2, analysis.ExitPoints.Count())
            Assert.Equal(0, analysis.EntryPoints.Count())
            Assert.False(analysis.StartPointIsReachable())
            Assert.False(analysis.EndPointIsReachable())
        End Sub

        <Fact()>
        Public Sub TestIfElseBranchReachability03()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestIfElseBranchReachability03">
          <file name="a.b">
Imports System
Module Program
    Function Foo() As Integer
        Dim x, y
        [|If True Then x = 1 Else If True Then Return 1 Else Return 1|]
        Return x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal(2, analysis.ExitPoints.Count())
            Assert.Equal(0, analysis.EntryPoints.Count())
            Assert.True(analysis.StartPointIsReachable())
            Assert.True(analysis.EndPointIsReachable())
        End Sub

        <Fact()>
        Public Sub TestIfElseBranch01()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch01">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Dim x, y
        [|If True Then x = 1 Else y = 1|]
        Dim z = x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <Fact()>
        Public Sub TestIfElseBranch01_()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch01">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Dim b As Boolean = True
        Dim x, y
        [|If b Then x = 1 Else y = 1|]
        Dim z = x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <Fact()>
        Public Sub TestIfElseBranch02()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch02">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Dim x, y
        If True Then [|x = 1|] Else y = 1
        Dim z = x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <Fact()>
        Public Sub TestIfElseBranch03()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch03">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Dim x, y, z
        If True Then x = 1 Else [|y = 1|]
        Dim z = x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut)) '' else clause is unreachable
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <Fact()>
        Public Sub TestIfElseBranch03_()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch03">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Dim b As Boolean = True
        Dim x, y, z
        If b Then x = 1 Else [|y = 1|]
        Dim z = x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <Fact()>
        Public Sub TestIfElseBranch04()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch04">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Dim x, y, z
        If True Then x = 1 Else If True Then y = 1 Else [|z = 1|]
        Dim zz = z + x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))  ''  else clause is unreachable
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <Fact()>
        Public Sub TestIfElseBranch04_()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch04">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Dim b As Boolean = True
        Dim x, y, z
        If b Then x = 1 Else If b Then y = 1 Else [|z = 1|]
        Dim zz = z + x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <Fact()>
        Public Sub TestIfElseBranch05()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch05">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Dim x, y, z
        If True Then x = 1 Else [|If True Then y = 1 Else y = 1|]
        Dim zz = z + x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <Fact()>
        Public Sub TestIfElseBranch05_()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch05">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Dim b As Boolean = True
        Dim x, y, z
        If b Then x = 1 Else [|If b Then y = 1 Else y = 1|]
        Dim zz = z + x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <Fact()>
        Public Sub TestVariablesInitializedWithSelfReference()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestVariablesInitializedWithSelfReference">
          <file name="a.b">
class C 
    public sub F(x as integer)
[|
        dim x as integer = x
        dim y as integer, z as integer = 1
|]
    end sub
end class</file>
      </compilation>)
            Assert.Equal("x, y, z", GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("x, z", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
        End Sub

        <Fact()>
        Public Sub TestVariablesDeclared02()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestVariablesDeclared02">
          <file name="a.b">
class C 
    public sub F(x as integer)
[|
        dim a as integer
        dim b as integer
        dim x as integer, y as integer = 1
        if true then
            dim z as string = "a"
        end if
        dim c as integer
|]
    end sub
end class</file>
      </compilation>)
            Assert.Equal("a, b, c, x, y, z", GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
        End Sub

        <Fact()>
        Public Sub AlwaysAssignedUnreachable()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="AlwaysAssignedUnreachable">
          <file name="a.b">
class C 
    Public Sub F(x As Integer)
[|
        Dim y As Integer
        If x = 1 Then
            y = 2
            Return
        Else
            y = 3
            Throw New Exception
        End If
        Dim c As Integer
|]
    End Sub
end class</file>
      </compilation>)
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
        End Sub

        <Fact()>
        Public Sub TestDataFlowLateCall()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowLateCall">
          <file name="a.b">
Option Strict Off

Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim o as object = 1
[|
        foo(o)
|]
    End Sub

    Sub foo(x As String)

    End Sub

    Sub foo(Byref x As Integer)

    End Sub
End Module
</file>
      </compilation>)
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("args, o", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestDataFlowLateCall001()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowLateCall001">
          <file name="a.b">
Option Strict Off

Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    shared Sub Main(args As String())
        Dim o as object = 1
        Dim oo as object = new Program
[|
        oo.foo(o)
|]
    End Sub

    Sub foo(x As String)

    End Sub

    Sub foo(Byref x As Integer)

    End Sub
End Class
</file>
      </compilation>)
            Assert.Equal("o, oo", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("o, oo", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("args, o, oo", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestDataFlowIndex()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsOut01">
          <file name="a.b">
Option Strict Off

Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim o as object = 1
[|
        Dim oo = o(o)
|]
    End Sub

    Sub foo(x As String)

    End Sub

    Sub foo(Byref x As Integer)

    End Sub
End Module
</file>
      </compilation>)
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal("oo", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("args, o", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub UnassignedVariableFlowsOut01()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="UnassignedVariableFlowsOut01">
          <file name="a.b">
class C 
    public sub F()
        Dim i as Integer = 10
[|
        Dim j as Integer = j + i
|]
        Console.Write(i)
        Console.Write(j)
    end sub
end class</file>
      </compilation>)
            Assert.Equal("j", GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("j", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("i, j", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("i, j", GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal("j", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestDataFlowsIn01()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsIn01">
          <file name="a.b">
class C 
    public sub F(x as integer)
        dim a as integer = 1, y as integer = 2
[|
        dim b as integer = a + x + 3
|]
        dim c as integer = a + 4 + y
    end sub
end class</file>
      </compilation>)
            Assert.Equal("a, x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
        End Sub

        <Fact()>
        Public Sub TestDataFlowsIn02()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsIn02">
          <file name="a.b">
class Program
    sub Test(of T as class, new)(byref t as T) 

[|
        dim t1 as T
        Test(t1)
        t = t1
|]
        System.Console.WriteLine(t1.ToString())
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
        End Sub

        <Fact()>
        Public Sub TestDataFlowsIn03()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsIn03">
          <file name="a.b">
class Program
    shared sub Main(args() as string)
        dim x as integer = 1
        dim y as integer = 2
[|
        dim z as integer = x + y
|]
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
        End Sub

        <Fact()>
        Public Sub TestDataFlowsOut01()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsOut01">
          <file name="a.b">
class C 
    public sub F(x as integer)
        dim a as integer = 1, y as integer
[|
        if x = 1 then
            x = 2
            y = x 
        end if
|]
        dim c as integer = a + 4 + x + y
    end sub
end class</file>
      </compilation>)
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <Fact()>
        Public Sub TestDataFlowsOut02()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsOut02">
          <file name="a.b">
class Program
    public sub Test(args() as string)
[|
        dim s as integer = 10, i as integer = 1
        dim b as integer = s + i
|]
        System.Console.WriteLine(s)
        System.Console.WriteLine(i)
    end sub
end class</file>
      </compilation>)
            Assert.Equal("i, s", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <Fact()>
        Public Sub TestDataFlowsOut03()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsOut03">
          <file name="a.b">
imports System.Text
module Program
    sub Main() as string
        dim builder as StringBuilder = new StringBuilder()
[|
        builder.Append("Hello")
        builder.Append("From")
        builder.Append("Roslyn")
|]
        return builder.ToString()
    end sub
end module</file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <Fact()>
        Public Sub TestDataFlowsOut04()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Class C
    Sub F(ByRef x As Integer)
        [|x = 12|]
    End Sub
End Class

            ]]></file>
        </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.Equal(True, controlFlowAnalysisResults.StartPointIsReachable)
            Assert.Equal(True, controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact()>
        Public Sub TestDataFlowsOut06()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
              <compilation>
                  <file name="a.b"><![CDATA[
Class C
    Sub F(b As Boolean)
        Dim i As Integer = 1
        While b
            [|i = i + 1|]
        End While
    End Sub
End Class

            ]]></file>
              </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.Equal(True, controlFlowAnalysisResults.StartPointIsReachable)
            Assert.Equal(True, controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("b, i, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact()>
        Public Sub TestDataFlowsOut07()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsOut07">
          <file name="a.b">
class Program
   sub F(b as boolean)
        dim i as integer
        [|
        i = 2
        goto [next]
        |]
    [next]:
        dim j as integer = i
    end sub
end class</file>
      </compilation>)
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <Fact()>
        Public Sub TestDataFlowsOut08()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsOut08">
          <file name="a.b">
Class Program
   Sub F()
        Dim i As Integer = 2
        Try
            [|
            i = 1
            |]
        Finally
            Dim j As Integer = i
        End Try
    End Sub
End Class
</file>
      </compilation>)
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <Fact()>
        Public Sub TestDataFlowsOut09()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsOut09">
          <file name="a.b">
class Program
    sub Test(args() as string)
        dim i as integer
        dim s as string

        [|i = 10
        s = args(0) + i.ToString()|]
    end sub
end class</file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <WorkItem(543492, "DevDiv")>
        <Fact()>
        Public Sub MeAndMyBaseReference1()
            Dim analysis = CompileAndAnalyzeDataFlow(
<compilation name="MeIsWrittenOutside1">
    <file name="a.b">
Imports System
Public Class BaseClass
    Public Overridable Sub MyMeth()
    End Sub
End Class
Public Class MyClass : Inherits BaseClass
    Public Overrides Sub MyMeth()
        [|MyBase.MyMeth()|]
    End Sub
End Class
    </file>
</compilation>)
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
        End Sub

        <WorkItem(543492, "DevDiv")>
        <Fact()>
        Public Sub MeAndMyBaseReference2()
            Dim analysis = CompileAndAnalyzeDataFlow(
<compilation name="MeIsWrittenOutside2">
    <file name="a.b">
Imports System
Public Class BaseClass
    Public Overridable Function MyMeth() As Boolean
        Return False
    End Function
End Class
Public Class MyClass1 : Inherits BaseClass
    Public Overrides Function MyMeth() As Boolean
        Return MyBase.MyMeth()
    End Function
    Public Sub OtherMeth()
        Dim f = Function() [|MyBase.MyMeth|]
    End Sub
End Class
    </file>
</compilation>)
            Assert.Equal("f, Me", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
        End Sub

        <Fact()>
        Public Sub TestDataFlowsOutExpression01()
            Dim analysis = CompileAndAnalyzeDataFlow(
<compilation name="TestDataFlowsOutExpression01">
    <file name="a.b">
class C 
    public sub F(x as integer)
        dim a as integer = 1, y as integer
        dim tmp as integer = x 
[|
            x = 2
            y = x
|]
            temp += (a = 2)
        dim c as integer = a + 4 + x + y
    end sub
end class</file>
</compilation>)
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <Fact()>
        Public Sub TestAlwaysAssigned01()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestAlwaysAssigned01">
          <file name="a.b">
class C
    public sub F(x as integer)

        dim a as integer = 1, y as integer= 1
[|
        if x = 2 then
            a = 3
        else 
            a = 4
        end if
        x = 4
        if x = 3 then
            y = 12
        end if
|]
        dim c as integer = a + 4 + y
    end sub
end class</file>
      </compilation>)
            Assert.Equal("a, x", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
        End Sub

        <Fact()>
        Public Sub TestAlwaysAssigned03()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestAlwaysAssigned03">
          <file name="a.b">
module C
    sub Main(args() as string)

        dim i as integer = [|
        int.Parse(args(0).ToString())
        |]

    end sub
end module</file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
        End Sub

        <Fact()>
        Public Sub TestWrittenInside02()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestWrittenInside02">
          <file name="a.b">
module C
    sub Main(args() as string)

        dim i as integer = [|
        int.Parse(args(0).ToString())
        |]

    end sub
end module</file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <Fact()>
        Public Sub TestWrittenInside03()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestWrittenInside03">
          <file name="a.b">
module C
    sub Main(args() as string)

        dim i as integer 
        i = [|
        int.Parse(args(0).ToString())
        |]

    end sub
end module</file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <Fact()>
        Public Sub TestAlwaysAssigned04()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestAlwaysAssigned04">
          <file name="a.b">
module C
    sub Main(args() as string)

        dim i as integer 
        i = [|
        int.Parse(args(0).ToString())
        |]

    end sub
end module</file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
        End Sub

        <Fact()>
        Public Sub TestAlwaysAssignedDuplicateVariables()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestAlwaysAssignedDuplicateVariables">
          <file name="a.b">
class C
    public sub F(x as integer)

[|
        dim a, a, b, b as integer
        b = 1
|]
    end sub
end class</file>
      </compilation>)
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
        End Sub

        <Fact()>
        Public Sub TestAlwaysAssigned02_LocalConst()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestAlwaysAssigned02">
          <file name="a.b">
class C
    public sub F(x as integer)

[|
        const dim a as integer = 1
|]
    end sub
end class</file>
      </compilation>)
            Assert.Equal("a", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
        End Sub

        <Fact()>
        Public Sub TestAccessedInsideOutside()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestAccessedInsideOutside">
          <file name="a.b">
class C
    public sub F(x as integer)

        dim a, b, c, d, e, f, g, h, i as integer
        a = 1
        b = a + x
        c = a + x
[|
        d = c
        f = d
        e = d
|]
        g = e
        i = g
        h = g
    end sub
end class</file>
      </compilation>)
            Assert.Equal("c, d", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("d, e, f", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("a, e, g, x", GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal("a, b, c, g, h, i, Me, x", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestAlwaysAssignedViaPassingAsByRefParameter()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
           <compilation>
               <file name="a.b"><![CDATA[
Class C
    Public Sub F(x As Integer)
[|        Dim a As Integer
        G(a)|]
    End Sub

    Sub G(ByRef x As Integer)
        x = 1
    End Sub
End Class

            ]]></file>
           </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.Equal(True, controlFlowAnalysisResults.StartPointIsReachable)
            Assert.Equal(True, controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal("a", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("a, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("a", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact()>
        Public Sub TestRedimPreserveDataFlow()
            VerifyReDimDataFlowAnalysis(
            <![CDATA[
                    Dim x(2) As Integer
                    [|ReDim Preserve x(3)|]
                ]]>,
                alwaysAssigned:={"x"},
                captured:={},
                dataFlowsIn:={"x"},
                dataFlowsOut:={},
                readInside:={"x"},
                readOutside:={},
                variablesDeclared:={},
                writtenInside:={"x"},
                writtenOutside:={"x"})
        End Sub

        <Fact()>
        Public Sub TestRedimDataFlow()
            VerifyReDimDataFlowAnalysis(
            <![CDATA[
                    Dim x(2) As Integer
                    [|ReDim x(3)|]
                    x(0) = 1
                ]]>,
                alwaysAssigned:={"x"},
                captured:={},
                dataFlowsIn:={},
                dataFlowsOut:={"x"},
                readInside:={},
                readOutside:={"x"},
                variablesDeclared:={},
                writtenInside:={"x"},
                writtenOutside:={"x"})
        End Sub

        <WorkItem(542156, "DevDiv")>
        <Fact()>
        Public Sub TestRedimImplicitDataFlow()
            VerifyReDimDataFlowAnalysis(
            <![CDATA[
                    [|ReDim x(3)|]
                    Dim y = x(0)
                ]]>,
                alwaysAssigned:={"x"},
                captured:={},
                dataFlowsIn:={},
                dataFlowsOut:={"x"},
                readInside:={},
                readOutside:={"x"},
                variablesDeclared:={},
                writtenInside:={"x"},
                writtenOutside:={"y"})
        End Sub

        <WorkItem(542156, "DevDiv")>
        <Fact()>
        Public Sub TestRedimMultipleImplicitDataFlow()
            VerifyReDimDataFlowAnalysis(
            <![CDATA[
                    [|ReDim x(3, z), y(4, z)|]
                    y = x
                    System.Console.WriteLine(z)
                ]]>,
                alwaysAssigned:={"x", "y"},
                captured:={},
                dataFlowsIn:={"z"},
                dataFlowsOut:={"x"},
                readInside:={"z"},
                readOutside:={"x", "z"},
                variablesDeclared:={},
                writtenInside:={"x", "y"},
                writtenOutside:={"y"})
        End Sub

#Region "Ternary Operator"

        <Fact()>
        Public Sub TestAlwaysAssignedWithTernaryOperator()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestAlwaysAssignedWithTernaryOperator">
          <file name="a.b">
class C
    public sub F(x as integer)
        dim ext as External = New External
        dim a as boolean
        [|dim c as boolean = if(true,ext.M1(a),ext.M1(a))|]
    end sub
end class
</file>
      </compilation>, customIL)
            Assert.Equal("a, c", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
        End Sub

        <Fact()>
        Public Sub TestAlwaysAssignedWithTernaryOperator2()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestAlwaysAssignedWithTernaryOperator2">
          <file name="a.b">
class C
    public sub F(x as integer)
        dim ext as External = New External
        dim a, b as boolean
        [|dim c as boolean = if(true,ext.M1(a),ext.M1(b))|]
    end sub
end class
</file>
      </compilation>, customIL)
            Assert.Equal("a, c", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
        End Sub

        <Fact()>
        Public Sub TestDeclarationWithSelfReferenceAndTernaryOperator()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestDeclarationWithSelfReferenceAndTernaryOperator">
          <file name="a.b">
class C
    shared sub Main()

[|
        dim x as integer = if(true, 1, x)
|]
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestDeclarationWithTernaryOperatorAndAssignment()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestDeclarationWithTernaryOperatorAndAssignment">
          <file name="a.b">
class C
    shared sub Main()

        dim x, y as boolean
        dim ext as external = new external
[|
        y = if(true, 1, ext.M1(x))
|]
    end sub
end class
            </file>
      </compilation>, customIL)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTernaryExpressionWithAssignments()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestTernaryExpressionWithAssignments">
          <file name="a.b">
class C
    shared sub Main()
        dim x as boolean = true
        dim y as integer
[|
        dim z as integer 
        y = if(x, 1, 2)
        z = y
|]
        y.ToString()
    end sub
end class
            </file>
      </compilation>)
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestBranchOfTernaryOperator()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestBranchOfTernaryOperator">
          <file name="a.b">
class C
    shared sub Main()
       dim x as boolean = true
       dim y as boolean = if(x,[|x|],true)
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestAssinmentExpressionAsBranchOfTernaryOperator()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestAssinmentExpressionAsBranchOfTernaryOperator">
          <file name="a.b">
class C
    shared sub Main()
        dim x as boolean
        dim ext as external = new external
        dim y as boolean = if(true,[|ext.M1(x)|],x)
    end sub
end class
            </file>
      </compilation>, customIL)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("ext, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTernaryConditional01()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestTernaryConditional01">
          <file name="a.b">
class C
    shared sub Main()
        dim x, y, z as boolean
        dim ext as external = new external
        dim zz as boolean = if([|ext.M1(x)|],ext.M1(y),ext.M1(z))
    end sub
end class
            </file>
      </compilation>, customIL)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub TestTernaryConditional02()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestTernaryConditional02">
          <file name="a.b">
class C
    shared sub Main()
        dim x, y, z as boolean
        dim ext as external = new external
        dim zz as boolean = if(ext.M1(x),[|ext.M1(y)|],ext.M1(z))
    end sub
end class
            </file>
      </compilation>, customIL)

            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub TestTernaryConditional03()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestTernaryConditional03">
          <file name="a.b">
class C
    shared sub Main()
        dim x, y, z as boolean
        dim ext as external = new external
        dim zz as boolean = if(ext.M1(x),ext.M1(y),[|ext.M1(z)|])
    end sub
end class
            </file>
      </compilation>, customIL)
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub TestTernaryConditional04()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestTernaryConditional04">
          <file name="a.b">
class C
    shared sub Main()
        dim x, y, z as boolean
        dim ext as external = new external
        dim zz as boolean = [|if(ext.M1(x),ext.M1(y),ext.M1(z))|]
    end sub
end class
            </file>
      </compilation>, customIL)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub TestTernaryConditional05()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestTernaryConditional05">
          <file name="a.b">
class C
    shared sub Main()
        dim x, y as boolean
        dim ext as external = new external
        dim zz as boolean = [|if(ext.M1(x),ext.M1(y),ext.M1(y))|]
    end sub
end class
            </file>
      </compilation>, customIL)

            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

#End Region

        <Fact()>
        Public Sub TestDeclarationWithSelfReference()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestDeclarationWithSelfReference">
          <file name="a.b">
class C
    shared sub Main()
[|
        dim x as integer = x
|]
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestIfStatementWithAssignments()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestIfStatementWithAssignments">
          <file name="a.b">
class C
    shared sub Main()
        dim x as boolean = true
        dim y as integer
[|
        if x then
            y = 1
        else 
            y = 2
        end if
|]
        y.ToString()
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestIfStatementWithConstantCondition()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestIfStatementWithConstantCondition">
          <file name="a.b">
class C
    shared sub Main()
        dim x as boolean = true
        dim y as integer
[|
        if true then
            y = x
        end if
|]
        y.ToString()
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestIfStatementWithNonConstantCondition()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestIfStatementWithNonConstantCondition">
          <file name="a.b">
class C
    shared sub Main()
       dim x as boolean = true
       dim y as integer
[|
        if true or x then
            y = x
        end if
|]
        y.ToString()
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestSingleVariableSelection()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestSingleVariableSelection">
          <file name="a.b">
class C
    shared sub Main()
       dim x as boolean = true
       dim y as boolean = x or [|
x |]
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestParenthesizedExpressionSelection()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestParenthesizedExpressionSelection">
          <file name="a.b">
class C
    shared sub Main()
       dim x as boolean = true
       dim y as boolean = x or [|(x = x) |] orelse x
    end sub
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned)) ' In C# '=' is an assignment while in VB it is a comparison.
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut)) 'C# flows out because this is an assignement expression.  In VB this is an equality test.
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside)) 'C# this is an assignment. In VB, this is a comparison so no assignment.
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestRefArgumentSelection()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestRefArgumentSelection">
          <file name="a.b">
class C
    shared sub Main()
        dim x as integer = 0
[|
        Foo(
x 
)
|]
      System.Console.WriteLine(x)
    end sub

    shared sub Foo(byref x as integer)
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(541891, "DevDiv")>
        <Fact()>
        Public Sub TestRefArgumentSelection02()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestRefArgumentSelection02">
          <file name="a.b">
class C
     Sub Main()
        Dim x As UInteger
        System.Console.WriteLine([|Foo(x)|])
    End Sub

    Function Foo(ByRef x As ULong)
        x = 123
        Return x + 1
    End Function
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(541891, "DevDiv")>
        <Fact()>
        Public Sub TestRefArgumentSelection02a()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestRefArgumentSelection02">
          <file name="a.b">
class C
     Sub Main()
        Dim x As UInteger
        System.Console.WriteLine(Foo([|x|]))
    End Sub

    Function Foo(ByRef x As ULong)
        x = 123
        Return x + 1
    End Function
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestCompoundAsseignmentTargetSelection01()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestCompoundAsseignmentTargetSelection01">
          <file name="a.b">
class C
     Sub Main()
        Dim x As String = ""
        [|x|]+=1
    End Sub
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestCompoundAsseignmentTargetSelection02()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestCompoundAsseignmentTargetSelection02">
          <file name="a.b">
class C
     Sub Main()
        Dim x As String = ""
        [|x+=1|]
    End Sub
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestCompoundAsseignmentTargetSelection03()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestCompoundAsseignmentTargetSelection03">
          <file name="a.b">
Imports System
Module M1
    Sub M(ParamArray ary As Long())
        Dim local01 As Integer = 1
        Dim local02 As Short = 2
[|
        local01 ^= local02
        Try
           local02 &lt;&lt;= ary(0) 
           ary(1) *= local01
           Dim flocal As Single = 0
           flocal /= ary(0)
           ary(1) \= ary(0)
        Catch ex As Exception
        Finally
            Dim slocal = Nothing
            slocal &amp;= Nothing
        End Try
|]
    End Sub
End Module
            </file>
      </compilation>)

            Assert.Equal("ex, flocal, slocal", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("local01, slocal", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ary, local01, local02", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("ary, flocal, local01, local02, slocal", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("ex, flocal, local01, local02, slocal", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("ary, local01, local02", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(541891, "DevDiv")>
        <Fact()>
        Public Sub TestRefArgumentSelection03()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestRefArgumentSelection03">
          <file name="a.b">
class C
     Sub Main()
        Dim x As ULong

        System.Console.WriteLine([|Foo(x)|])
    End Sub

    Function Foo(ByRef x As ULong)
        x = 123
        Return x + 1
    End Function
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestInvocation()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestInvocation">
          <file name="a.b">
class C
    shared sub Main()
        dim x as integer = 1, y as integer = 1
[|
        Foo(x)
|]
    end sub

    shared sub Foo(int x) 
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside)) ' Sees Me beng read
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestInvocationWithAssignmentInArguments()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestInvocationWithAssignmentInArguments">
          <file name="a.b">
class C
    shared sub Main()
        dim x as integer = 1, y as integer = 1
[|
        x = y
        y = 2
        Foo(y, 2) ' VB does not support expression assignment F(x = y, y = 2)
|]
        dim z as integer = x + y
    }

    shared sub Foo(int x, int y)
    end sub
}
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside)) ' Sees Me being read
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact>
        Public Sub TestArrayInitializer()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Class C
    Sub Main(args As String())
        Dim y As Integer = 1
        Dim x(,) As Integer x = { { 
[|y|]
 } }
    End Sub
End Class

            ]]></file>
        </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.Equal(True, controlFlowAnalysisResults.StartPointIsReachable)
            Assert.Equal(True, controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("args, Me, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact, WorkItem(538979, "DevDiv")>
        Public Sub AssertFromInvalidLocalDeclaration()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="AssertFromInvalidLocalDeclaration">
          <file name="a.b">
Imports System

Public Class C
    Public Shared Function Main() As Integer
        [|
        Dim v As Variant = New Byte(2)
        |]
        Dim b as Byte = v(0)
        Return 1
    End Function
End Class
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal("v", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("v", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("v", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("v", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("v", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact(), WorkItem(538979, "DevDiv")>
        Public Sub AssertFromInvalidKeywordAsExpr()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="AssertFromInvalidKeywordAsExpr">
          <file name="a.b">
class B : A
    public Function M() as float
[|
        return mybase 
|]
    End Function
end class

class A 
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
        End Sub

        <WorkItem(539286, "DevDiv")>
        <Fact()>
        Public Sub RegionAnalysisInFieldInitializers_Simple()

            Dim dataResults1 = CompileAndAnalyzeDataFlow(
<compilation name="RegionAnalysisInFieldInitializers_Simple">
    <file name="a.b">
Class Class1
    Public Shared A As Integer = 10
    Public B As Integer = [|
                        10 + A + Me.F() |]

    Public Function F() As Integer
        Return Nothing
    End Function
End Class
            </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataResults1.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataResults1.Captured))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataResults1.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataResults1.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataResults1.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataResults1.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataResults1.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataResults1.WrittenOutside))

        End Sub

        <WorkItem(539286, "DevDiv")>
        <Fact()>
        Public Sub RegionAnalysisInPtopertyInitializers_Simple()

            Dim dataResults1 = CompileAndAnalyzeDataFlow(
<compilation name="RegionAnalysisInPtopertyInitializers_Simple">
    <file name="a.b">
Class Class1
    Public Shared A As Integer = 10
    Public Property B As Integer = [|
                        10 + A + Me.F() |]

    Public Function F() As Integer
        Return Nothing
    End Function
End Class
            </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataResults1.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataResults1.Captured))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataResults1.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataResults1.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataResults1.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataResults1.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataResults1.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataResults1.WrittenOutside))

        End Sub

        <WorkItem(539286, "DevDiv")>
        <Fact()>
        Public Sub RegionAnalysisInFieldInitializers_WithMyBase()
            Dim source =
                <compilation name="RegionAnalysisInFieldInitializers_WithMyBase">
                    <file name="a.b">
                        Class Class1
                            Inherits Base

                            Public Shared A As Integer = 10
                            Public B As Integer = [|
                                                10 + A + Me.F() + MyBase.F() + 
                                                Function()
                                                    Return 10 + A + Me.F() + MyBase.F()
                                                End Function.Invoke |]

                            Public Overrides Function F() As Integer
                                Return Nothing
                            End Function
                        End Class

                        Class Base
                            Public Overridable Function F() As Integer
                                Return Nothing
                            End Function
                        End Class
                    </file>
                </compilation>

            Dim dataResults1 = CompileAndAnalyzeDataFlow(source)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataResults1.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataResults1.Captured))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataResults1.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataResults1.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataResults1.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataResults1.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataResults1.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataResults1.WrittenOutside))

        End Sub

        <WorkItem(539286, "DevDiv")>
        <Fact()>
        Public Sub RegionAnalysisInFieldInitializers_Lambda()

            Dim results1 = CompileAndAnalyzeControlAndDataFlow(
<compilation name="RegionAnalysisInFieldInitializers_Lambda">
    <file name="a.b">
Imports System
Class Class1
    Public Sub TST()
        Dim f As Func(Of Integer, Integer) =
             Function(p)
                 Dim a As Integer = 1
                 Dim b As Integer = 2
                 [|
                 b = 3
                 Dim c As Integer = 1 + a + b + Me.Foo() |]
                 Return c
             End Function
    End Sub

    Public Function Foo() As Integer
        Return Nothing
    End Function
End Class
            </file>
</compilation>)

            Dim results2 = CompileAndAnalyzeControlAndDataFlow(
<compilation name="AssertFromInvalidKeywordAsExpr">
    <file name="a.b">
Imports System
Class Class1
    Dim f As Func(Of Integer, Integer) = 
        Function(p)
            Dim a As Integer = 1
            Dim b As Integer = 2
            [|
            b = 3
            Dim c As Integer = 1 + a + b + Me.Foo() |]
            Return c
        End Function

    Public Function Foo() As Integer
        Return Nothing
    End Function
End Class
            </file>
</compilation>)


            Dim controlResults1 = results1.Item1
            Dim dataResults1 = results1.Item2
            Dim controlResults2 = results2.Item1
            Dim dataResults2 = results2.Item2

            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.AlwaysAssigned),
                GetSymbolNamesSortedAndJoined(dataResults2.AlwaysAssigned))
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.Captured),
                GetSymbolNamesSortedAndJoined(dataResults2.Captured))
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.DataFlowsIn),
                GetSymbolNamesSortedAndJoined(dataResults2.DataFlowsIn))
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.DataFlowsOut),
                GetSymbolNamesSortedAndJoined(dataResults2.DataFlowsOut))
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.ReadInside),
                GetSymbolNamesSortedAndJoined(dataResults2.ReadInside))
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.ReadOutside),
                GetSymbolNamesSortedAndJoined(dataResults2.ReadOutside))
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.WrittenInside),
                GetSymbolNamesSortedAndJoined(dataResults2.WrittenInside))
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.WrittenOutside),
                String.Join(", ", New String() {"f"}.Concat((dataResults2.WrittenOutside).Select(Function(s) s.Name)).OrderBy(Function(name) name)))

        End Sub

        <WorkItem(539286, "DevDiv")>
        <Fact()>
        Public Sub RegionAnalysisInFieldInitializers_NestedLambdaAndTwoConstructors()

            Dim results1 = CompileAndAnalyzeControlAndDataFlow(
<compilation name="RegionAnalysisInFieldInitializers_NestedLambdaAndTwoConstructors">
    <file name="a.b">
Class Class1(Of T)
    Private Sub TST()
        Dim f As Func(Of T, Integer, Integer) =
            Function(x, p)
                Dim a_outer As Integer = 1
                Dim tx As T = x
                Dim ff As Func(Of T, Integer, Integer) =
                Function(xx, pp)
                    Dim a As Integer = 1
                    Dim b As Integer = 2
                    Dim ttx As T = tx
                    [|
                    b = 3
                    Dim c As Integer = Foo() + p + pp + a + b + a_outer |]
                    Return c
                End Function
                Return ff(Nothing, p)
            End Function
    End Sub

    Public Function Foo() As Integer
        Return Nothing
    End Function

    Public Sub New()
    End Sub
    Public Sub New(i As Integer)
    End Sub
End Class
            </file>
</compilation>)

            Dim results2 = CompileAndAnalyzeControlAndDataFlow(
<compilation name="AssertFromInvalidKeywordAsExpr">
    <file name="a.b">
Class Class1(Of T)
    Dim f As Func(Of T, Integer, Integer) =
        Function(x, p)
            Dim a_outer As Integer = 1
            Dim tx As T = x
            Dim ff As Func(Of T, Integer, Integer) =
            Function(xx, pp)
                Dim a As Integer = 1
                Dim b As Integer = 2
                Dim ttx As T = tx
                [|
                b = 3
                Dim c As Integer = Foo() + p + pp + a + b + a_outer |]
                Return c
            End Function
            Return ff(Nothing, p)
        End Function

    Public Function Foo() As Integer
        Return Nothing
    End Function

    Public Sub New()
    End Sub
    Public Sub New(i As Integer)
    End Sub
End Class
            </file>
</compilation>)


            Dim controlResults1 = results1.Item1
            Dim dataResults1 = results1.Item2
            Dim controlResults2 = results2.Item1
            Dim dataResults2 = results2.Item2

            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.AlwaysAssigned),
                GetSymbolNamesSortedAndJoined(dataResults2.AlwaysAssigned))
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.Captured),
                GetSymbolNamesSortedAndJoined(dataResults2.Captured))
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.DataFlowsIn),
                GetSymbolNamesSortedAndJoined(dataResults2.DataFlowsIn))
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.DataFlowsOut),
                GetSymbolNamesSortedAndJoined(dataResults2.DataFlowsOut))
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.ReadInside),
                GetSymbolNamesSortedAndJoined(dataResults2.ReadInside))
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.ReadOutside),
                GetSymbolNamesSortedAndJoined(dataResults2.ReadOutside))
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.WrittenInside),
                GetSymbolNamesSortedAndJoined(dataResults2.WrittenInside))
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataResults1.WrittenOutside),
                String.Join(", ", New String() {"f"}.Concat((dataResults2.WrittenOutside).Select(Function(s) s.Name)).OrderBy(Function(name) name)))

        End Sub

        <WorkItem(539197, "DevDiv")>
        <Fact()>
        Public Sub ByRefParameterNotInAppropriateCollections1()
            ' ByRef parameters are not considered assigned
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
<compilation name="AssertFromInvalidKeywordAsExpr">
    <file name="a.b">
Imports System
Imports System.Collections.Generic
Class Program
    Sub Test(of T)(ByRef t As T)
[|
        Dim t1 As T
        Test(t1)
        t = t1
|]
        System.Console.WriteLine(t1.ToString())
    End Sub
End Class
            </file>
</compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal("t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("t", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(539197, "DevDiv")>
        <Fact()>
        Public Sub ByRefParameterNotInAppropriateCollections2()
            ' ByRef parameters are not considered assigned
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
<compilation name="AssertFromInvalidKeywordAsExpr">
    <file name="a.b">
Imports System
Imports System.Collections.Generic
Class Program
    Sub Test(Of T)(ByRef t As T)
[|
        Dim t1 As T = GetValue(of T)(t)
|]
        System.Console.WriteLine(t1.ToString())
    End Sub
    Private Function GetValue(Of T)(ByRef t As T) As T
        Return t
    End Function
End Class

            </file>
</compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal("t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, t", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, t", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        Private Shared customIL As XCData = <![CDATA[
.class public auto ansi beforefieldinit External
       extends [mscorlib]System.Object
{
  .method public hidebysig instance bool 
          M1([out] bool& x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  2
    .locals init ([0] bool CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldc.i4.1
    IL_0003:  stind.i1
    IL_0004:  ldarg.1
    IL_0005:  ldind.i1
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method External::M1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method External::.ctor

} // end of class External
]]>

        <Fact()>
        Public Sub TestOutParameterAlwaysAsigned()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestOutParameterAlwaysAsigned">
          <file name="a.b">
class C
    shared sub Main()
        dim b as boolean = true
        dim ext as external = new external
        dim zz as boolean = [|ext.M1(b)|]
    end sub
end class
            </file>
      </compilation>, customIL)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))  ' NOTE: always assigned
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("b, ext, zz", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub BinaryConditional()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation name="BinaryConditional">
    <file name="a.b">
Class A
    Function Test1() As Integer
        Dim ext As External = New External
        Dim x As Boolean = True
        Dim y As Boolean = IF(New Object(), [|ext.M1(x)|])
    End Function
End Class
            </file>
</compilation>, customIL)

            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub BinaryConditional01()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation name="BinaryConditional01">
    <file name="a.b">
Class A
    Function Test1() As Integer
        Dim ext As External = New External
        Dim x As Boolean = True
        Dim y As True = IF("", [|ext.M1(x)|])
    End Function
End Class
            </file>
</compilation>, customIL)

            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub BinaryConditional02()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation name="BinaryConditional02">
    <file name="a.b">
Class A
    Function Test1() As Integer
        Dim ext As External = New External
        Dim x As Boolean = True
        Dim y As True = [|IF("", ext.M1(x))|]
    End Function
End Class
            </file>
</compilation>, customIL)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub BinaryConditional03()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation name="BinaryConditional03">
    <file name="a.b">
Class A
    Function Test1() As Integer
        Dim ext As External = New External
        Dim x As Boolean = True
        Dim xx As Boolean = True
        Dim y As True = [|IF(ext.M1(xx), ext.M1(x))|]
    End Function
End Class
            </file>
</compilation>, customIL)

            Assert.Equal("xx", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x, xx", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub BinaryConditional04()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation name="BinaryConditional04">
    <file name="a.b">
Class A
    Function Test1() As Integer
        Dim ext As External = New External
        Dim x As Boolean = True
        Dim xx As Boolean = True
        Dim y As True = IF([|ext.M1(xx)|], ext.M1(x))
    End Function
End Class
            </file>
</compilation>, customIL)

            Assert.Equal("xx", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("xx", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub BinaryConditional05()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation name="BinaryConditional05">
    <file name="a.b">
Class A
    Sub Test1()
        Dim ext As External = New External
        Dim x As Boolean = Nothing
        Dim z As Object = [|IF(Nothing, ext.M1(x))|]
    End Sub
End Class
            </file>
</compilation>, customIL)

            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub BinaryAndAlso01()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation name="BinaryAndAlso01">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Dim x As Boolean = True
        Dim y As Boolean = False
        Dim z As Boolean = IF(Nothing, [|F(x)|]) AndAlso IF(Nothing, F(y)) AndAlso False
    End Sub
End Class
            </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("Me, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, x, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub BinaryAndAlso02()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation name="BinaryAndAlso02">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Dim x As Boolean
        Dim y As Boolean = False
        Dim z As Boolean = x AndAlso [|y|] AndAlso False
    End Sub
End Class
            </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub BinaryAndAlso03()
            Dim source =
<compilation name="BinaryAndAlso03">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Dim ext As External = New External
        Dim x As Boolean = True
        Dim y As Boolean = False
        Dim z As Boolean = [|ext.M1(x)|] AndAlso ext.M1(y)
    End Sub
End Class
            </file>
</compilation>

            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(source, customIL)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub BinaryAndAlso04()
            Dim source =
<compilation name="BinaryAndAlso04">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Dim ext As External = New External
        Dim x As Boolean = True
        Dim y As Boolean = False
        Dim z As Boolean = ext.M1(x) AndAlso [|ext.M1(y)|]
    End Sub
End Class
            </file>
</compilation>

            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(source, customIL)
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub BinaryAndAlso05()
            Dim source =
<compilation name="BinaryAndAlso05">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Dim ext As External = New External
        Dim x As Boolean = True
        Dim y As Boolean = False
        Dim z As Boolean = [|ext.M1(x) AndAlso ext.M1(y)|] AndAlso True
    End Sub
End Class
            </file>
</compilation>

            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(source, customIL)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub BinaryAndAlso06()
            Dim source =
<compilation name="BinaryAndAlso06">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Dim ext As External = New External
        Dim x As Boolean = True
        Dim y As Boolean = False
        Dim z As Boolean = [|ext.M1(x) AndAlso ext.M1(y) AndAlso True|]
    End Sub
End Class
            </file>
</compilation>

            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(source, customIL)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub BinaryOrElse01()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation name="BinaryOrElse01">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Dim x As Boolean = True
        Dim y As Boolean = False
        Dim z As Boolean = IF(Nothing, [|F(x)|]) OrElse IF(Nothing, F(y)) OrElse False
    End Sub
End Class
            </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub BinaryOrElse02()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation name="BinaryOrElse02">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Dim x As Boolean
        Dim y As Boolean = False
        Dim z As Boolean = x OrElse [|y|] OrElse False
    End Sub
End Class
            </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub BinaryOrElse03()
            Dim source =
<compilation name="BinaryOrElse03">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Dim ext As External = New External
        Dim x As Boolean = True
        Dim y As Boolean = False
        Dim z As Boolean = [|ext.M1(x)|] OrElse ext.M1(y)
    End Sub
End Class
            </file>
</compilation>

            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(source, customIL)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub BinaryOrElse04()
            Dim source =
<compilation name="BinaryOrElse04">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Dim ext As External = New External
        Dim x As Boolean = True
        Dim y As Boolean = False
        Dim z As Boolean = ext.M1(x) OrElse [|ext.M1(y)|]
    End Sub
End Class
            </file>
</compilation>

            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(source, customIL)
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub BinaryOrElse05()
            Dim source =
<compilation name="BinaryOrElse05">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Dim ext As External = New External
        Dim x As Boolean = True
        Dim y As Boolean = False
        Dim z As Boolean = [|ext.M1(x) OrElse ext.M1(y)|] OrElse True
    End Sub
End Class
            </file>
</compilation>

            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(source, customIL)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <Fact()>
        Public Sub BinaryOrElse06()
            Dim source =
<compilation name="BinaryOrElse06">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Dim ext As External = New External
        Dim x As Boolean = True
        Dim y As Boolean = False
        Dim z As Boolean = [|ext.M1(x) OrElse ext.M1(y) OrElse True|]
    End Sub
End Class
            </file>
</compilation>

            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(source, customIL)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ext", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <WorkItem(541005, "DevDiv")>
        <Fact()>
        Public Sub TestMultipleLocalsInitializedByAsNew1()
            Dim dataFlowAnalysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestMultipleLocalsInitializedByAsNew">
          <file name="a.b">
Module Program
    Class c
        Sub New(i As Integer)
        End Sub
    End Class

    Sub Main(args As String())
        Dim a As Integer = 1
        Dim x, y, z As New c([|a|]+1)
    End Sub
End Module
  </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("a, args, x, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <WorkItem(541005, "DevDiv")>
        <Fact()>
        Public Sub TestMultipleLocalsInitializedByAsNew2()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestMultipleLocalsInitializedByAsNew">
          <file name="a.b">
Module Program
    Class c
        Sub New(i As Integer)
        End Sub
    End Class

    Sub Main(args As String())
        Dim a As Integer = 1
        [|Dim x, y, z As New c(a)|]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.Equal("x, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("a, args", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenOutside))
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <WorkItem(528623, "DevDiv")>
        <Fact()>
        Public Sub TestElementAccess01()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestElementAccess">
          <file name="elem.b">
Imports System

Public Class Test
    Sub F(p as Long())
        Dim v() As Long =  new Long() { 1, 2, 3 }
        [|
        v(0) = p(0)
        p(0) = v(1)
        |]
        v(1) = v(0)
        ' p(2) = p(0)
    End Sub
End Class
  </file>
      </compilation>)

            Dim dataFlowAnalysis = analysis.Item2
            Assert.True(dataFlowAnalysis.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("p, v", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("p, v", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("v", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("Me, p, v", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenOutside))

        End Sub

        <Fact()>
        Public Sub DataFlowForDeclarationOfEnumTypedVariable()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Class C
    Sub Main(args As String())
        [|Dim s As color|]
        Try
        Catch ex As Exception When s = color.black
            Console.Write("Exception")
        End Try
        End Sub
End Class 

Enum color
    black
End Enum
]]></file>
        </compilation>)

            Assert.Equal("s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("args, ex, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(542565, "DevDiv")>
        <Fact()>
        Public Sub IdentifierNameInForStatement()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Module Module1
    Sub Main()            
                For [|Idx|] = 0 To ubound(arry) Step 1
                Next Idx
        End Sub
End Module
  </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(528860, "DevDiv")>
        <Fact()>
        Public Sub IdentifierNameInMemberAccessExpr()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Public Class Foo
    Sub M()
        Dim c As C = New C()
        Dim n1 = c.[|M|]
  End Sub
End Class
  </file>
   </compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(528860, "DevDiv")>
        <Fact()>
        Public Sub IdentifierNameInMemberAccessExpr2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Public Class C
    Sub M()
        Dim c As C = New C()
        Dim n1 = c.[|M|]
        End Sub
End Class
  </file>
   </compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(542860, "DevDiv")>
        <Fact()>
        Public Sub IdentifierNameSyntax()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Imports Microsoft.VisualBasic
Public Class C
    Sub M()
        Dim n1 = [|ChrW|](85)
    End Sub
End Class
  </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(542860, "DevDiv")>
        <Fact()>
        Public Sub IdentifierNameSyntax2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Imports Microsoft.VisualBasic
Public Class C
    Sub M()
        Dim n1 = [|Foo|](85)
        End Sub
    Function Foo(i As Integer) As Integer
        Return i
    End Function
End Class
  </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(542860, "DevDiv")>
        <Fact()>
        Public Sub IdentifierNameSyntax3()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Imports Microsoft.VisualBasic
Public Class C
    Sub M()
        Dim n1 = [|Foo|](85)
  End Sub
    ReadOnly Property Foo(i As Integer) As Integer
        Get
            Return i
        End Get
    End Property
End Class
  </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(543369, "DevDiv")>
        <Fact()>
        Public Sub PredefinedTypeIncompleteSub()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
    Friend Module AcceptVB7_12mod
        Sub AcceptVB7_12()
                Dim lng As [|Integer|]
                Dim int1 As Short
  </file>
   </compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(543369, "DevDiv")>
        <Fact()>
        Public Sub PredefinedType2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
    Friend Module AcceptVB7_12mod
        Sub AcceptVB7_12()
                Dim lng As [|Integer|]
                Dim int1 As Short
  End Sub
    And Module
  </file>
   </compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(543461, "DevDiv")>
        <Fact()>
        Public Sub CollectionInitSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim i1 = New Integer() {4, 5}
  End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim exprSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("{4, 5}")).Parent, CollectionInitializerSyntax)
            Dim analysis = model.AnalyzeDataFlow(exprSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(543461, "DevDiv")>
        <Fact()>
        Public Sub CollectionInitSyntax2()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Imports System.Collections.Generic
Module Program
    Sub Main(args As String())
        Dim i1 = New List(Of Integer) From {4, 5}
  End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim exprSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("{4, 5}")).Parent, CollectionInitializerSyntax)
            Dim analysis = model.AnalyzeDataFlow(exprSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(543461, "DevDiv")>
        <Fact()>
        Public Sub CollectionInitSyntax3()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Imports System.Collections.Generic
Module Program
    Sub Main(args As String())
        Dim i1 = {4, 5}
  End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim exprSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("{4, 5}")).Parent, CollectionInitializerSyntax)
            Dim analysis = model.AnalyzeDataFlow(exprSyntaxNode)

            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(543509, "DevDiv")>
        <Fact()>
        Public Sub IfStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim x = 10
        If False
            x = x + 1
        End If
    End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("If False")).Parent, IfStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <Fact()>
        Public Sub ElseStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim x = 10
        If False
            x = x + 1
        Else 
            x = x - 1
        End If
    End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("Else")).Parent, ElseStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <Fact()>
        Public Sub WithStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        With New Object()
        End With
    End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("With New Object()")).Parent, WithStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(757796, "DevDiv")>
        <Fact()>
        Public Sub Bug757796()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        Dim tableauEmission(123) As Integer
        For t As Integer = 0 To 123 - 1
            With tableauEmission(t)
            End With
        Next
    End Sub
End Module  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim withStatement = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("With tableauEmission(t)")).Parent, WithStatementSyntax)
            Dim tableauEmissionNode = DirectCast(withStatement.Expression, Microsoft.CodeAnalysis.VisualBasic.Syntax.InvocationExpressionSyntax).Expression
            Dim analysis = model.AnalyzeDataFlow(tableauEmissionNode)

            Assert.True(analysis.Succeeded)
        End Sub

        <Fact()>
        Public Sub TryStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        Try
            Dim a = 123
        Catch e As Exception
        End Try
    End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("Try")).Parent, TryStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <Fact()>
        Public Sub CatchStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        Try
            Dim a = 123
        Catch e As Exception
        End Try
    End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("Catch e As Exception")).Parent, CatchStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <Fact()>
        Public Sub FinallyStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        Try
            Dim a = 123
        Finally
        End Try
    End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("Finally")).Parent, FinallyStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(543722, "DevDiv")>
        <Fact()>
        Public Sub SyncLockStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        SyncLock New With {.x = 0}
        End SyncLock
    End Sub
End Module   
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("SyncLock New With {.x = 0}")).Parent, SyncLockStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(543736, "DevDiv")>
        <Fact()>
        Public Sub WhileStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        While True
        End While
    End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("While True")).Parent, WhileStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <Fact()>
        Public Sub UsingStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Imports System.IO
Module Program
    Sub Main(args As String())
        Using mem = New MemoryStream()
        End Using
    End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("Using mem = New MemoryStream()")).Parent, UsingStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(545449, "DevDiv")>
        <Fact()>
        Public Sub LoopWhileStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Imports System.IO
Module Program
    Sub Main(args As String())
        Do
            If Not Me.Scan() Then
                Return False
            End If
        Loop While Me.backwardBranchChanged
        Return True
    End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("Loop While Me.backwardBranchChanged")).Parent, LoopStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <Fact()>
        Public Sub SelectStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Class Frame
    Sub Foo()
        Select Case 1 + 2 + 3
            Case 1
            Case 2
        End Select
    End Sub
End Class
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("Select Case 1 + 2 + 3")).Parent, SelectStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <Fact()>
        Public Sub CaseStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Class Frame
    Sub Foo()
        Select Case 1 + 2 + 3
            Case 1
            Case 2
        End Select
    End Sub
End Class
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim selectSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("Select Case 1 + 2 + 3")).Parent, SelectStatementSyntax)
            Dim stmtSyntaxNode = DirectCast(selectSyntaxNode.Parent, SelectBlockSyntax).CaseBlocks(0).Begin
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <Fact()>
        Public Sub DoStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Class Frame
    Sub Foo()
        Do
            Exit Do
        Loop
    End Sub
End Class
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("Do")).Parent, DoStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <Fact()>
        Public Sub ForStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Class Frame
    Sub Foo()
        For i = 0 To 1
        Next
    End Sub
End Class
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("For i = 0 To 1")).Parent, ForStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <Fact()>
        Public Sub ForEachStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Class Frame
    Sub Foo()
        For Each c In ""
        Next
    End Sub
End Class
      </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("For Each c In """"")).Parent, ForEachStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(543548, "DevDiv")>
        <Fact()>
        Public Sub NamespaceIdentifierNameInMemberAccess()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Namespace STForEach01
    Friend Module STForEach01mod
        Sub STForEach01
        End Sub
    End Module
End Namespace

Friend Module MainModule
    Sub Main()
        [|STForEach01|].STForEach01
    End Sub
End Module
  </file>
   </compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(543548, "DevDiv")>
        <Fact()>
        Public Sub NamespaceIdentifierNameInMemberAccess2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Namespace STForEach01
    Friend Module STForEach01mod
        Sub STForEach01
        End Sub
    End Module
End Namespace

Friend Module MainModule
    Sub Main()
        [|STForEach01.STForEach01mod|].STForEach01
    End Sub
End Module
  </file>
   </compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(543548, "DevDiv")>
        <Fact()>
        Public Sub NamespaceIdentifierNameInMemberAccess3()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Namespace STForEach01
    Friend Module STForEach01mod
        ReadOnly Property STForEach01 As Integer
            Get
                Return 1
            End Get
        End Property 
    End Module
End Namespace

Friend Module MainModule
    Sub Main()
        Dim a As Integer = [|STForEach01|].STForEach01
    End Sub
End Module
    </file>
</compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(543548, "DevDiv")>
        <Fact()>
        Public Sub NamespaceIdentifierNameInMemberAccess4()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Namespace STForEach01
    Friend Module STForEach01mod
        ReadOnly Property STForEach01 As Integer
            Get
                Return 1
            End Get
        End Property 
    End Module
End Namespace

Friend Module MainModule
    Sub Main()
        Dim a As Integer = [|STForEach01.STForEach01mod|].STForEach01
    End Sub
End Module
    </file>
</compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(543695, "DevDiv")>
        <Fact()>
        Public Sub NamespaceIdentifierNameInMemberAccess5()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()
        Dim d1 = Sub(x As Integer)
                     [|System|].Console.WriteLine(x)
                 End Sub
    End Sub
End Module
    </file>
</compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(543695, "DevDiv")>
        <Fact()>
        Public Sub NamespaceIdentifierNameInMemberAccess6()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()
        [|System|].Console.WriteLine(x)
    End Sub
End Module
    </file>
</compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(543695, "DevDiv")>
        <Fact()>
        Public Sub NamespaceIdentifierNameInMemberAccess7()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Public Class A
    Public Class B
        Public Shared Sub M()
        End Sub
    End Class
End Class

Module Program
    Sub Main()
        [|A.B|].M()
    End Sub
End Module
    </file>
</compilation>)
            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(543695, "DevDiv")>
        <Fact()>
        Public Sub NamespaceIdentifierNameInMemberAccess8()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Public Class A
    Public Class B
        Public Shared Sub M()
        End Sub
    End Class
End Class

Module Program
    Sub Main()
        [|A|].B.M()
    End Sub
End Module
    </file>
</compilation>)
            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(545080, "DevDiv")>
        <Fact()>
        Public Sub NamespaceIdentifierNameInMemberAccess9()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Public Class Compilation
    Public Class B
        Public Shared Function M(a As Integer) As Boolean
            Return False
        End Function
    End Class
End Class

Friend Class Program
    Public Shared Sub Main()
        Dim x = [| Compilation |].B.M(a:=123)
    End Sub
    Public ReadOnly Property Compilation As Compilation
        Get
            Return Nothing
        End Get
    End Property
End Class

    </file>
</compilation>)
            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(545080, "DevDiv")>
        <Fact()>
        Public Sub NamespaceIdentifierNameInMemberAccess10()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Public Class Compilation
    Public Shared Function M(a As Integer) As Boolean
        Return False
    End Function
End Class

Friend Class Program
    Public Shared Sub Main()
        Dim x = [| Compilation |].M(a:=123)
    End Sub
    Public ReadOnly Property Compilation As Compilation
        Get
            Return Nothing
        End Get
    End Property
End Class

    </file>
</compilation>)
            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <Fact()>
        Public Sub ConstLocalUsedInLambda01()
            Dim analysisResult = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub Main()
        Dim local = 1
        Const constLocal = 2
        Dim f = [| Function(p as sbyte) As Short
                    Return local + constlocal + p
                End Function |]
        Console.Write(f)
    End Sub
End Module
    </file>
</compilation>)

            Assert.Equal("p", GetSymbolNamesSortedAndJoined(analysisResult.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysisResult.AlwaysAssigned))
            Assert.Equal("local", GetSymbolNamesSortedAndJoined(analysisResult.Captured))
            Assert.Equal("constLocal, local", GetSymbolNamesSortedAndJoined(analysisResult.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysisResult.DataFlowsOut))
            Assert.Equal("constLocal, local, p", GetSymbolNamesSortedAndJoined(analysisResult.ReadInside))
            ' WHY
            Assert.Equal("p", GetSymbolNamesSortedAndJoined(analysisResult.WrittenInside))
            Assert.Equal("f", GetSymbolNamesSortedAndJoined(analysisResult.ReadOutside))
            Assert.Equal("constLocal, f, local", GetSymbolNamesSortedAndJoined(analysisResult.WrittenOutside))

        End Sub

        <Fact()>
        Public Sub ConstLocalUsedInLambda02()
            Dim analysisResult = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Class C
    Function F(mp As Short) As Integer
        Try
            Dim local = 1
            Const constLocal = 2
            Dim lf = [| Sub()
                         local = constlocal + mp
                     End Sub |]
            lf()
            Return local
        Finally
        End Try
    End Function
End Class
    </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysisResult.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysisResult.AlwaysAssigned))
            Assert.Equal("local, mp", GetSymbolNamesSortedAndJoined(analysisResult.Captured))
            Assert.Equal("constLocal, mp", GetSymbolNamesSortedAndJoined(analysisResult.DataFlowsIn))
            Assert.Equal("local", GetSymbolNamesSortedAndJoined(analysisResult.DataFlowsOut))
            Assert.Equal("constLocal, mp", GetSymbolNamesSortedAndJoined(analysisResult.ReadInside))
            Assert.Equal("local", GetSymbolNamesSortedAndJoined(analysisResult.WrittenInside))
            Assert.Equal("lf, local", GetSymbolNamesSortedAndJoined(analysisResult.ReadOutside))
            Assert.Equal("constLocal, lf, local, Me, mp", GetSymbolNamesSortedAndJoined(analysisResult.WrittenOutside))

        End Sub

        <WorkItem(543701, "DevDiv")>
        <Fact()>
        Public Sub LiteralExprInVarDeclInsideSingleLineLambda()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Test
    Sub Sub1()
        Dim x = Sub() Dim y = [|10|]
    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(543702, "DevDiv")>
        <Fact()>
        Public Sub LiteralExprInsideEnumMemberDecl()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Program
    Enum NUMBERS
        One = [|1|]
    End Enum
End Module
    </file>
</compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(11662, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectCreationExpr()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim x As [|New C|]
    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
        End Sub

#Region "ObjectInitializer"

        <Fact()>
        Public Sub ObjectInitializersNoLocalsAccessed()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Class C1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Class

Public Class C2
    Public Shared Sub Main()
        Dim intlocal As Integer
        Dim x = New C1() With {.FieldStr = [|.FieldInt.ToString()|]}
    End Sub
End Class
    </file>
</compilation>)

            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializersLocalsAccessed1_OnlyImplicitReceiverRegion1()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Dim x, y As New S1() With {.FieldStr = [|.FieldInt.ToString()|]}
    End Sub
End Class
    </file>
</compilation>)

            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializersLocalsAccessed1_OnlyImplicitReceiverRegion2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Dim x, y As New S1() With {.FieldInt = [|.FieldStr.Length|]}
    End Sub
End Class
    </file>
</compilation>)

            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializersLocalsAccessed1_DeclAndImplicitReceiverRegion()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        [| Dim x, y As New S1() With {.FieldInt = .FieldStr.Length} |]
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializersLocalsAccessed1_ValidRegion1()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Default Public Property PropInt(i As String) As String
        Get
            Return 0
        End Get
        Set(value As String)
        End Set
    End Property
End Structure

Public Class S2
    Public Shared Sub Main()
        Dim x, y As New S1() With {.FieldInt = !A.Length }
        x.FieldInt = [| x!A.Length |]
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializersLocalsAccessed1_ValidRegion2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Default Public Property PropInt(i As String) As String
        Get
            Return 0
        End Get
        Set(value As String)
        End Set
    End Property
End Structure

Public Class S2
    Public Shared Sub Main()
        Dim x, y As New S1() With {.FieldInt = [| x!A.Length |] }
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializersLocalsAccessed1_InvalidRegion3()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Default Public Property PropInt(i As String) As String
        Get
            Return 0
        End Get
        Set(value As String)
        End Set
    End Property
End Structure

Public Class S2
    Public Shared Sub Main()
        Dim x, y As New S1() With {.FieldStr = [| !A |] }
    End Sub
End Class
    </file>
</compilation>)

            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <WorkItem(531226, "DevDiv")>
        <Fact()>
        Public Sub DisableConstantLiteralRegion()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Class S2
    Public Shared Sub Main()
        Const PERUSER_EXTENSION As String = [|".user"|] 'Project .user file extension
    End Sub
End Class
    </file>
</compilation>)

            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <WorkItem(531226, "DevDiv")>
        <Fact()>
        Public Sub WithStatement_LValueExpression()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public F1 As Integer
    Public F2 As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Dim arr() As S1 = {}
        With arr([|0|])
            Console.WriteLine(.F1)
            .F2 = 123
            Console.WriteLine(.F2)
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <WorkItem(531226, "DevDiv")>
        <Fact()>
        Public Sub WithStatement_LValueExpression2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public F1 As Integer
    Public F2 As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Dim arr() As S1 = {}
        With arr([|0|])
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <WorkItem(531226, "DevDiv")>
        <Fact()>
        Public Sub WithStatement_LValueExpression3()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public F1 As Integer
    Public F2 As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Dim arr() As S1 = {}
        With [|arr(0)|]
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("arr", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("arr", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <WorkItem(531226, "DevDiv")>
        <Fact()>
        Public Sub WithStatement_LValueExpression4()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public F1 As Integer
    Public F2 As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Dim arr As New S1
        With [|arr|]
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializersLocalsAccessed1a_ObjectInitializer()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Dim o As New S1()
        With o
            [|Console.WriteLine(New S1() With {.FieldStr = .FieldInt.ToString()})|]
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializersLocalsAccessed1a_ObjectInitializer2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Dim o As New S1()
        With o
            Console.WriteLine(New S1() With {.FieldStr = [|.FieldInt.ToString()|] })
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializersLocalsAccessed1b()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Dim o As New S1()
        With o
            [|Console.WriteLine(New List(Of String) From {.FieldStr, "Brian", "Tim"})|]
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializersLocalsAccessed1bb()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Dim o As New S1()
        [|Console.WriteLine(New List(Of String) From {o.FieldStr, "Brian", "Tim"})|]
    End Sub
End Class
    </file>
</compilation>)

            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializers_StructWithFieldAccessesInLambda1()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Shared Sub TEST()
        Dim a, b As New SS2() With {.X = Function() As SS1
                                          With .Y
                                              [| .A = "1" |]
                                              '.B = "2"
                                          End With
                                          Return .Y
                                      End Function.Invoke()}
    End Sub
End Structure
    </file>
</compilation>)

            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializers_StructWithFieldAccessesInLambda2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Shared Sub TEST()
        Dim a, b As New SS2() With {.X = Function() As SS1
                                          With .Y
                                              [| 
                                                b.Y.B = a.Y.A
                                                a.Y.A = "1" 
                                              |]
                                          End With
                                          Return .Y
                                      End Function.Invoke()}
    End Sub
End Structure
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("a", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("a", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializers_StructWithFieldAccessesInLambda3()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Sub New(i As Integer)
        Dim l = Sub()
                    Dim a, b As New SS2() With {.X = Function() As SS1
                                                      With .Y
                                                          [| 
                                                            b.Y.B = a.Y.A
                                                            a.Y.A = "1" 
                                                          |]
                                                      End With
                                                      Return .Y
                                                  End Function.Invoke()}
                End Sub
    End Sub
End Structure
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("a", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("a", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("a, b, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("a, b, i, l, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializers_StructWithFieldAccessesInLambda4()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Sub New(i As Integer)
            Dim a, b As New SS2() With {.X = Function() As SS1
                                                [| a.Y = New SS1()
                                                   b.Y = New SS1() |]
                                                Return .Y
                                             End Function.Invoke()}

            Console.WriteLine(a.ToString())
    End Sub
End Structure
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("a, b, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("a, b, i, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializers_StructWithFieldAccessesInLambda5()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Sub New(i As Integer)
            Dim a, b As New SS2() With {.X = Function() As SS1
                                                [| b.Y = New SS1() |]
                                                Return a.Y
                                             End Function.Invoke()}

            Console.WriteLine(a.ToString())
    End Sub
End Structure
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("a, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("a, b, i, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializers_StructWithFieldAccessesInLambda6()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Sub New(i As Integer)
            Dim a, b As New SS2() With {.X = Function() As SS1
                                                [| b.Y = New SS1() |]
                                                Return b.Y
                                             End Function.Invoke()}

            Console.WriteLine(a.ToString())
    End Sub
End Structure
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("a, b, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("a, b, i, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(544298, "DevDiv")>
        <Fact()>
        Public Sub ObjectInitializers_PassingFieldByRef()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Shared Function Transform(ByRef p As SS1) As SS1
        Return p
    End Function

    Sub New(i As Integer)
        Dim a, b As New SS2() With {.X = [| Transform(b.Y) |] }
    End Sub
End Structure
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("a, b, i, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub ObjectInitializersLocalsAccessed2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Dim x As New S1() With {.FieldStr = [|.FieldInt.ToString()|]}
    End Sub
End Class
    </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub ObjectInitializersWithLocalsAccessed()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Class C1
    Public FieldStr As String
End Class

Public Class C2
    Public Shared Function GetStr(p as string)
        return p    
    end Function

    Public Shared Sub Main()
        Dim strlocal As String
        Dim x = New C1() With {.FieldStr = [|GetStr(strLocal)|]}
    End Sub
End Class
    </file>
</compilation>)

            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("strlocal", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("strlocal", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact()>
        Public Sub ObjectInitializersWithLocalCaptured()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Class C1
    Public Field As Integer = 42
    Public Field2 As Func(Of Integer)
End Class

Class C1(Of T)
    Public Field As T
End Class

Class C2
    Public Shared Sub Main()
        Dim localint as integer = 23
        Dim x As New C1 With {.Field2 = [|Function() As Integer
                                            Return localint
                                        End Function|]}
        x.Field = 42
        Console.WriteLine(x.Field2.Invoke())
    End Sub
End Class 

    </file>
</compilation>)

            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("localint", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("localint", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("localint, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("localint", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact()>
        Public Sub ObjectInitializersWholeStatement()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Class C1
    Public Field As Integer = 42
    Public Field2 As Func(Of Integer)
End Class

Class C1(Of T)
    Public Field As T
End Class

Class C2
    Public Shared Sub Main()
        Dim localint as integer
        [|Dim x As New C1 With {.Field2 = Function() As Integer
                                            localInt = 23
                                            Return localint
                                        End Function}|]
        x.Field = 42
        Console.WriteLine(x.Field2.Invoke())
    End Sub
End Class 

    </file>
</compilation>)

            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("localint", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("localint, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("localint", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count)
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
        End Sub

#End Region

#Region "CollectionInitializer"

        <Fact()>
        Public Sub CollectionInitializersCompleteObjectCreationExpression()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class C2
    Public Shared Sub Main()
        dim foo as string = "Hello World"
        Dim x as [|New List(Of String) From {foo, "!"}|]
    End Sub
End Class
    </file>
</compilation>)

            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("foo", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("foo", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("foo, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub CollectionInitializersOutermostInitializerAreNoVBExpressions()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class C2
    Public Shared Sub Main()
        dim foo as string = "Hello World"
        Dim x as New List(Of String) From [|{foo, "!"}|]
    End Sub
End Class
    </file>
</compilation>)
            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <Fact()>
        Public Sub CollectionInitializersTopLevelInitializerAreNoVBExpressions()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class C2
    Public Shared Sub Main()
        dim foo as string = "Hello World"
        Dim x as New Dictionary(Of String, Integer) From {[|{foo, 1}|], {"bar", 42}}
    End Sub
End Class
    </file>
</compilation>)

            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <WorkItem(530032, "DevDiv")>
        <Fact>
        Public Sub CollectionInitializersNestedInitializer()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class C2
    Public Shared Sub Main(),
        dim foo as string = "Hello World"
        Dim x as New Dictionary(Of String(), Integer) From { {[|{foo, "!"}|], 1} }
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("foo", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("foo", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("foo, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub CollectionInitializersLiftedLocals()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class C2
    Public Shared Sub Main()
        Dim foo As String = "Hello World"
        Dim x As [|New List(Of Action) From {
            Sub()
                Console.WriteLine(foo)
            End Sub,
            Sub()
                Console.WriteLine(x.Item(0))
                x = nothing
            End Sub
        }|]
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("foo, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("foo, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("foo, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("foo, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact(), WorkItem(12970, "DevDiv_Projects/Roslyn")>
        Public Sub CollectionInitUndeclaredIdentifier()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim f1() As String = {[|X|]}

    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
        End Sub

#End Region

        <Fact(), WorkItem(544079, "DevDiv")>
        Public Sub UserDefinedOperatorBody()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Module Module1

    Class B2
        Public f As Integer

        Public Sub New(x As Integer)
            f = x
        End Sub

        Shared Widening Operator CType(x As Integer) As B2
            [| Return New B2(x) |]
        End Operator
    End Class

    Sub Main()
        Dim x As Integer = 11
        Dim b2 As B2 = x
    End Sub
End Module
    </file>
</compilation>)

            Dim ctrlFlowResults = analysisResults.Item1
            Assert.True(ctrlFlowResults.Succeeded)
            Assert.Equal(1, ctrlFlowResults.ExitPoints.Count())
            Assert.Equal(0, ctrlFlowResults.EntryPoints.Count())
            Assert.True(ctrlFlowResults.StartPointIsReachable)
            Assert.False(ctrlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysisResults.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Empty(dataFlowResults.VariablesDeclared)
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Empty(dataFlowResults.Captured)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty((dataFlowResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Empty(dataFlowResults.ReadOutside)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact(), WorkItem(544079, "DevDiv")>
        Public Sub UserDefinedOperatorBody_1()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Module Module1

    Class B2
        Public f As Integer

        Public Sub New(x As Integer)
            f = x
        End Sub

        Shared Operator -(x As Integer, y As B2) As B2
            [| Return New B2(x) |]
        End Operator
    End Class

    Sub Main()
    End Sub
End Module
    </file>
</compilation>)

            Dim ctrlFlowResults = analysisResults.Item1
            Assert.True(ctrlFlowResults.Succeeded)
            Assert.Equal(1, ctrlFlowResults.ExitPoints.Count())
            Assert.Equal(0, ctrlFlowResults.EntryPoints.Count())
            Assert.True(ctrlFlowResults.StartPointIsReachable)
            Assert.False(ctrlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysisResults.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Empty(dataFlowResults.VariablesDeclared)
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Empty(dataFlowResults.Captured)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty((dataFlowResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Empty(dataFlowResults.ReadOutside)
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub UserDefinedOperatorInExpression()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Module Module1

    Class B2
        Public f As Integer
        Public Sub New(x As Integer)
            f = x
        End Sub
        Shared Operator -(x As Integer, y As B2) As B2
            Return New B2(x)
        End Operator
    End Class

    Sub Main(args As String())
        Dim x As Short = 123
        Dim bb = New B2(x)
        Dim ret = [| Function(y)
                      Return args.Length - (y - (x - bb))
                  End Function |]
    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Equal("args, bb, x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("args, bb, x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("args, bb, x, y", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, bb, ret, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact(), WorkItem(545047, "DevDiv")>
        Public Sub UserDefinedLiftedOperatorInExpr()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Class A
    Structure S
        Shared Narrowing Operator CType(x As S?) As Integer
            System.Console.WriteLine("Operator Conv")
            Return 123 'Nothing
        End Operator

        Shared Operator *(x As S?, y As Integer?) As Integer?
            System.Console.WriteLine("Operator *")
            Return y
        End Operator
    End Structure
End Class

Module Program
     Sub M(Optional p As Integer? = Nothing)
        Dim local As A.S? = New A.S() 
        Dim f As Func(Of A.S, Integer?) = [| Function(x)
                                              Return x * local * p
                                          End Function |]
        Console.Write(f(local))
    End Sub
End Module
    </file>
</compilation>)


            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Equal("local, p", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("local, p", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("local, p, x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("f, local", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("f, local, p", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact(), WorkItem(545047, "DevDiv")>
        Public Sub DataFlowsInAndNullable()
            ' WARNING: if this test is edited, the test with the 
            '          test with the same name in C# must be modified too
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure S
    Public F As Integer
    Public Sub New(_f As Integer)
        Me.F = _f
    End Sub
End Structure

Module Program
    Sub Main(args As String())
        Dim i As Integer? = 1
        Dim s As New S(1)
        [|
        Console.Write(i.Value)
        Console.Write(s.F)
        |]
    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Empty(dataFlowResults.VariablesDeclared)
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Empty(dataFlowResults.Captured)
            Assert.Equal("i, s", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("i, s", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Empty(dataFlowResults.ReadOutside)
            Assert.Equal("args, i, s", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(545249, "DevDiv")>
        <Fact()>
        Sub TestWithEventsInitializer()
            Dim comp = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Class C1
    WithEvents e As C1 = [|Me|]
End Class
    </file>
</compilation>)
            Debug.Assert(comp.Succeeded)
        End Sub

        <WorkItem(545249, "DevDiv")>
        <Fact()>
        Sub TestWithEventsInitializer2()
            Dim comp = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Class C1
    Public Sub New(c As C1)
    End Sub
    WithEvents e As New C1([|Me|])
End Class
    </file>
</compilation>)
            Debug.Assert(comp.Succeeded)
        End Sub

        <Fact()>
        Sub TestWithEventsInitializer3()
            Dim comp = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Class C1
    Public Sub New(c As C1)
    End Sub
    WithEvents e, f As C1 = [|Me|]
End Class
    </file>
</compilation>)
            Debug.Assert(comp.Succeeded)
        End Sub

        <Fact()>
        Sub TestWithEventsInitializer4()
            Dim comp = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Class C1
    Public Sub New(c As C1)
    End Sub
    WithEvents d As New C1(Me), e, f As New C1([|Me|])
End Class
    </file>
</compilation>)
            Debug.Assert(comp.Succeeded)
        End Sub

        <WorkItem(545480, "DevDiv")>
        <Fact()>
        Sub ReturnStatementInElseInsideIncompleteFunction()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb">
Public Class vbPartialCls002
    Public Function Fun1() As Object
        If Nothing Then
            Return New Object()
        Else
            Return Nothing
    </file>
</compilation>)

            Dim tree = comp.SyntaxTrees.First()
            Dim stmtNode = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of StatementSyntax).Where(Function(node) node.ToString() = "Return Nothing").First()
            Dim semanticModel = comp.GetSemanticModel(tree)
            Dim controlFlowAnalysis = semanticModel.AnalyzeControlFlow(stmtNode)
            Assert.True(controlFlowAnalysis.Succeeded)
        End Sub

        <WorkItem(545900, "DevDiv")>
        <Fact()>
        Sub AnonymousObjectCreationExprInsideOptionalParamDecl()
            Dim comp = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Program
    Function scen3(Optional ByRef p1 As Object = New With {.abc = [|123|]})
    End Function
End Module
    </file>
</compilation>)
            Assert.False(comp.Succeeded)
        End Sub

        <WorkItem(545900, "DevDiv")>
        <Fact()>
        Sub AnonymousObjectCreationExprInsideOptionalParamDecl2()
            Dim comp = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Class Program
    Public XYZ As String = "xyz"
    Function scen3(Optional ByRef p1 As Object = New With {.abc = [|Me.XYZ|]}) As String
        Return Nothing
    End Function
End Class
    </file>
</compilation>)
            Assert.False(comp.Succeeded)
        End Sub

        <WorkItem(545900, "DevDiv")>
        <Fact()>
        Sub LambdaExprInsideOptionalParamDecl2()
            Dim comp = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main(args As String())

    End Sub
    Property P(i As Integer, Optional k As String = (Function() As String
                                                         Return [| "" |]
                                                     End Function)()) As String
        Get
            Return k
        End Get
        Set(value As String)

        End Set
    End Property
End Module    
    </file>
</compilation>)
            Assert.True(comp.Succeeded)
        End Sub

        <Fact()>
        Public Sub ConstantUnevaluatedReceiver()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Const F As Object = Nothing
    Function M() As Object
        Return Me.F
    End Function
End Class
]]></file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim expr = FindNodeOfTypeFromText(Of ExpressionSyntax)(tree, "Me")
            model.AnalyzeDataFlow(expr)
        End Sub

        <Fact()>
        Public Sub CallUnevaluatedReceiver()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Shared Function F() As Object
        Return Nothing
    End Function
    Function M() As Object
        Return Me.F()
    End Function
End Class
]]></file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim expr = FindNodeOfTypeFromText(Of ExpressionSyntax)(tree, "Me")
            model.AnalyzeDataFlow(expr)
        End Sub

        <WorkItem(546639, "DevDiv")>
        <Fact()>
        Public Sub AddressOfUnevaluatedReceiver()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Shared Sub M()
    End Sub
    Function F() As System.Action
        Return AddressOf Me.M
    End Function
End Class
]]></file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim expr = FindNodeOfTypeFromText(Of ExpressionSyntax)(tree, "Me")
            model.AnalyzeDataFlow(expr)
        End Sub

        <WorkItem(546629, "DevDiv")>
        <Fact()>
        Public Sub TypeExpressionUnevaluatedReceiver()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Class B
        Friend Const F As Object = Nothing
    End Class
    Function M() As Object
        Return (Me.B).F
    End Function
End Class
]]></file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim expr = FindNodeOfTypeFromText(Of ExpressionSyntax)(tree, "Me")
            model.AnalyzeDataFlow(expr)
        End Sub

        <WorkItem(545266, "DevDiv")>
        <Fact()>
        Public Sub DataFlowImplicitLoopVariableInBrokenCodeNotInDataFlowsOut()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main(ByVal args As String())
        GoTo Label1
        For i = 1 To 5
Label1:
            Dim j = [|i|]
        Next
    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Empty(dataFlowResults.VariablesDeclared)
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Empty(dataFlowResults.Captured)
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Empty(dataFlowResults.ReadOutside)
            Assert.Equal("args, i, j", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(545266, "DevDiv")>
        <Fact()>
        Public Sub DataFlowImplicitLoopVariableInBrokenCodeNotInDataFlowsOut_2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main(ByVal args As String())
        GoTo Label1
        For x = 1 To 5
        For i = 1 To 5
Label1:
            Dim j = [|i|]
        Next
        next
    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Empty(dataFlowResults.VariablesDeclared)
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Empty(dataFlowResults.Captured)
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Empty(dataFlowResults.ReadOutside)
            Assert.Equal("args, i, j, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(545266, "DevDiv")>
        <Fact()>
        Public Sub DataFlowUnassignedVariablesWithoutAssignmentInsideDoNotFlowOut_1()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main(ByVal args As String())

        GoTo Label1

        if args(0) > 23 then
            dim i as integer = 23
Label1:
            Dim j = [|i|]
            i = 23
        end if
    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Empty(dataFlowResults.VariablesDeclared)
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Empty(dataFlowResults.Captured)
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Equal("args", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, i, j", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(545266, "DevDiv")>
        <Fact()>
        Public Sub DataFlowUnassignedVariablesWithoutAssignmentInsideDoNotFlowOut_2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main(ByVal args As String())

        GoTo Label1

        if args(0) > 23 then
            dim i as integer
Label1:
            dim k = i
            Dim j = [|i|]
            i = 23
        end if
    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Empty(dataFlowResults.Captured)
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Equal("args, i", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, i, j, k", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(545266, "DevDiv")>
        <Fact()>
        Public Sub DataFlowImplicitUsingVariableInBrokenCodeNotInDataFlowsOut()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main(ByVal args As String())
        GoTo Label1
        using i as new Object
Label1:
            Dim j = [|i|]
        end using
    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Empty(dataFlowResults.VariablesDeclared)
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Empty(dataFlowResults.Captured)
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, i, j", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(546995, "DevDiv")>
        <Fact()>
        Public Sub DataFlowUnassignedVariablesWithoutAssignmentInsideDoNotFlowOut_3()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main(ByVal args As String())
        [|GoTo Lable1
        For i = 1 To 5
Lable1:
            Dim q = i
        Next|]
    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("i, q", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Empty(dataFlowResults.Captured)
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("i, q", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Empty(dataFlowResults.ReadOutside)
            Assert.Equal("args", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub
        <WorkItem(669341, "DevDiv")>
        <Fact()>
        Public Sub ReceiverRead()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Public Structure X
    Public Y As Y
End Structure

Public Structure Y
    Public Z As Z
End Structure

Public Structure Z
    Public Value As Integer
End Structure

Module Module1

    Sub Main()
        Dim X As New X
        Dim Value = [|X.Y|].Z.Value
    End Sub

End Module
    </file>
</compilation>)

            Assert.Equal("X", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Equal("X", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Value, X", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(669341, "DevDiv")>
        <Fact()>
        Public Sub ReceiverWritten()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Public Structure X
    Public Y As Y
End Structure

Public Structure Y
    Public Z As Z
End Structure

Public Structure Z
    Public Value As Integer
End Structure

Module Module1

    Sub Main()
        Dim X As New X
        [|X.Y|].Z.Value = 12
    End Sub

End Module
    </file>
</compilation>)

            Assert.Empty(dataFlowResults.ReadInside)
            Assert.Equal("X", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Empty(dataFlowResults.ReadOutside)
            Assert.Equal("X", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(669341, "DevDiv")>
        <Fact()>
        Public Sub ReceiverReadAndWritten()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Public Structure X
    Public Y As Y
End Structure

Public Structure Y
    Public Z As Z
End Structure

Public Structure Z
    Public Value As Integer
End Structure

Module Module1

    Sub Main()
        Dim X As New X
        [|X.Y|].Z.Value += 12
    End Sub

End Module
    </file>
</compilation>)

            Assert.Equal("X", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("X", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("X", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("X", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub


#Region "Anonymous Type, Lambda"

        <WorkItem(543464, "DevDiv")>
        <Fact()>
        Public Sub TestCaptured()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestLifted">
          <file name="a.b">
class C
    Dim field = 123
    public sub F(x as integer)

        dim a as integer = 1, y as integer = 1
[|
        dim l1 = function() x+y+field
|]
        dim c as integer = a + 4 + y
    end sub
end class</file>
      </compilation>)

            Assert.Equal("l1", GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal("l1", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal("Me, x, y", GetSymbolNamesSortedAndJoined(analysis.Captured))
            Assert.Equal("Me, x, y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("Me, x, y", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("l1", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("a, y", GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal("a, c, Me, x, y", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(542629, "DevDiv")>
        <Fact()>
        Public Sub TestRegionControlFlowAnalysisInsideLambda()
            Dim controlFlowAnalysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestRegionControlFlowAnalysisInsideLambda">
          <file name="a.b">
Imports System
Module Module1
    Sub Main()
        Dim f1 As Func(Of Integer, Integer) = Function(lambdaParam As Integer)
                                                  [| Return lambdaParam + 1 |]
                                              End Function
    End Sub
End Module
  </file>
      </compilation>)
            Assert.Equal(1, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.False(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <WorkItem(542629, "DevDiv")>
        <Fact()>
        Public Sub TestRegionControlFlowAnalysisInsideLambda2()
            Dim controlFlowAnalysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestRegionControlFlowAnalysisInsideLambda2">
          <file name="a.b">
Imports System
Module Module1
    Sub Main()
        Dim f1 As Object = Function(lambdaParam As Integer)
                               [| Return lambdaParam + 1 |]
                           End Function
        End Sub
End Module
  </file>
      </compilation>)
            Assert.Equal(1, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.False(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <WorkItem(542629, "DevDiv")>
        <Fact()>
        Public Sub TestRegionControlFlowAnalysisInsideLambda3()
            Dim controlFlowAnalysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestRegionControlFlowAnalysisInsideLambda3">
          <file name="a.b">
Imports System
Module Module1
    Sub Main()
        Dim f1 As Object = Nothing 
        f1 = Function(lambdaParam As Integer)
                 [| Return lambdaParam + 1 |]
             End Function
        End Sub
End Module
  </file>
      </compilation>)
            Assert.Equal(1, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.False(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub DoLoopInLambdaBody()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
<compilation name="DoLoopWithContinue">
    <file name="a.b">
Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        Console.Write(x)
        dim x as System.Action(of Integer) = Sub(i)
[|
            Do
                Console.Write(i)
                i = i + 1
                Continue Do
                'Blah
            Loop Until i > 5 |]   
        end sub     
        Return x
                           End Function
End Class
  </file>
</compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("i, Me, x, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub AnonymousTypeAsLambdaLocal()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Option Infer On
Imports System

Public Class Test
  delegate R Func(OfT, R)(ref T t);
    Public Shared Sub Main()
        Dim local(3) As String
[|
        Dim lambda As Func(Of Integer, Integer) =
                  Function(ByRef p As Integer) As Integer
                      p = p * 2
                      Dim at = New With {New C(Of Integer)().F, C(Of String).SF, .L = local.Length + p}
                      Console.Write("{0}, {1}, {2}", at.F, at.SF)
                      Return at.L
                  End Function
|]
    End Sub

    Class C(Of T)
        Public Function F() As T
            Return Nothing
        End Function
        Shared Public Function SF() As T
            Return Nothing
        End Function
    End Class

End Class
    </file>
   </compilation>)

            Dim controlFlowResults = analysisResults.Item1
            Dim dataFlowResults = analysisResults.Item2
            Assert.True(controlFlowResults.Succeeded)
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("lambda", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("local", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("at, lambda, p", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("local", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("p", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("at, local, p", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("at, lambda, p", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("local", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub AnonymousTypeAsNewInLocalContext()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Imports System

Interface IFoo
    Delegate Sub DS(ByRef p As Char)
End Interface

Class CFoo
    Implements IFoo
End Class

Friend Module AM
    Sub Main(args As String())
        Dim ifoo As IFoo = New CFoo()
        Dim at1 As New With {.if = ifoo}
[|
        Dim at2 As New With {.if = at1, ifoo,
            .friend = New With {Key args, .lambda = DirectCast(Sub(ByRef p As Char)
                                                                   args(0) = p &amp; p
                                                                   p = "Q"c
                                                               End Sub, IFoo.DS)}}
|]
     Console.Write(args(0))
    End Sub
End Module
    </file>
   </compilation>)

            Dim controlFlowResults = analysisResults.Item1
            Dim dataFlowResults = analysisResults.Item2
            Assert.True(controlFlowResults.Succeeded)
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("at2", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("args", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("at2, p", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("args, at1, ifoo", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("p", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("args, at1, ifoo, p", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("at2, p", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("args, ifoo", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, at1, ifoo", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub AnonymousTypeAsExpression()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Imports System

Interface IFoo
    Delegate Sub DS(ByRef p As Char)
End Interface

Friend Module AM
    Sub Main(args As String())

       Dim at1 As New With {.friend = New With {args, Key.lambda = DirectCast(Sub(ByRef p As Char)
                                                                                  args(0) = p &amp; p
                                                                                  p = "Q"c
                                                                              End Sub, IFoo.DS) }
                          }
       Dim at2 As New With { Key .a= at1, .friend = New With { [| at1 |] }}
       Console.Write(args(0))

    End Sub
End Module
    </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("args", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("at1", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("at1", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("args, at1, p", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, at1, at2, p", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub AnonymousTypeAccessInstanceMember()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.vb">
Imports System

Class AM

    Dim field = 123
    Sub M(args As String())

       Dim at1 As New With {.friend = [| New With {args, Key.lambda = Sub(ByRef ary As Char())
                                                                       Field = ary.Length
                                                                   End Sub } |]
                          }
    End Sub
End Class
    </file>
        </compilation>)

            Assert.True(dataFlowResults.Succeeded)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("ary", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("args, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("ary", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("args, ary, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("ary", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, at1, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub AnonymousTypeFieldInitializerWithLeftOmitted()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.vb">
Imports System

Class AM

    Dim field = 123
    Sub M(args As String())
       Dim var1 As New AM
       Dim at1 As New With { var1, .friend = [| .var1 |] }
    End Sub
End Class
    </file>
        </compilation>)

            Assert.True(dataFlowResults.Succeeded)

            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("var1", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, at1, Me, var1", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub AnonymousTypeUsingMe()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Imports System

Class Base
    Protected Function F1() As Long
        Return 123
    End Function
    Friend Overridable Function F2(n As Integer) As Integer
        Return 456
    End Function
End Class

Class Derived
    Inherits Base
    Friend Overrides Function F2(n As Integer) As Integer
        Return 789
    End Function

    Sub M()
        Dim func = Function(x)
                       Dim at = [| New With {.dim = New With {Key .nested = Me.F2(x * x)}} |]
                       Return at.dim.nested
                   End Function
    End Sub
End Class
    </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("at", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("at, func, Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub AnonymousTypeAccessMyBase()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Imports System

Class Base
    Protected Overridable Function F1() As Long
        Return 123
    End Function
End Class

Class Derived
    Inherits Base
    Protected Overrides Function F1() As Long
        Return 789
    End Function

    Sub M()

        Dim func = Function(x)
                       Dim at = [| New With {Key .dim = New With {MyBase.F1()}} |]
                       Return at.dim.F1
                   End Function
    End Sub
End Class
    </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("at", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("at, func, Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub AnonymousTypeAccessMyClass()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Imports System

Module M1

    Class B1
        Public Overridable Function F() As String
            Return "B1::F_"
        End Function
    End Class

    Class B2
        Inherits B1

        Public Overrides Function F() As String
            Return "B2::F_"
        End Function

        Public Sub TestMMM()
            Dim an = [| New With {.an = Function(s) As String
                                         Return s + Me.F() + MyBase.F() + MyClass.F()
                                     End Function
                              } |]
            Console.WriteLine(an.an("R="))
    End Sub

    End Class

    Class D
        Inherits B2

        Public Overrides Function F() As String
            Return "D::F_"
    End Function
End Class

    Public Sub Main()
        Call (New D()).TestMMM()
    End Sub

End Module
    </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, s", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("an", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("an, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(543046, "DevDiv")>
        <Fact()>
        Public Sub Lambda()
            ' The region is not correct and it is not clear if there is a way to fix the test
            Assert.Throws(Of ArgumentException)(
                Sub()
                    Dim dataFlowResults = CompileAndAnalyzeDataFlow(
           <compilation>
               <file name="a.vb">
Option Strict On
Imports System
Imports System.Runtime.InteropServices
Public Class S1
    [|Const str As String = "" &lt; MyAttribute(Me.color.blue) &gt;
    Sub foo()
    End Sub|]
    Shared Sub main()
    End Sub
    Enum color
        blue
    End Enum
End Class
Class MyAttribute
    Inherits Attribute
    Sub New(str As S1.color)
    End Sub
End Class
    </file>
           </compilation>)

                    Assert.False(dataFlowResults.Succeeded)
                End Sub)
        End Sub

        <WorkItem(543684, "DevDiv")>
        <Fact()>
        Public Sub AddressOfExpr()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main()
        Dim x5 = Function() AddressOf [|Main|]
    End Sub
End Module
    </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(543741, "DevDiv")>
        <Fact()>
        Public Sub AddressOfExpr2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Module Program
    Public Event Ev1
    Public Sub Handler1()
    End Sub
    Public Sub AddFirstHandler()
        AddHandler Ev1, AddressOf [|Handler1|]
    End Sub
End Module
    </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)
        End Sub

        <Fact()>
        Public Sub XmlEmbeddedExpression()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
      <compilation>
          <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Module M
    Function F() As Object
        Dim v0 = "v0"
        Dim v1 = XName.Get("v1", "")
        Dim v2 = XName.Get("v2", "")
        Dim v3 = "v3"
        Dim v4 = New XAttribute(XName.Get("v4", ""), "v4")
        Dim v5 = "v5"
        Return <?xml version="1.0"?><<%= v1 %> <%= v2 %>="v2" v3=<%= v3 %> <%= v4 %>><%= v5 %></>
    End Function
End Module
    ]]></file>
      </compilation>, additionalRefs:=XmlReferences)
            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim root = tree.GetCompilationUnitRoot()
            Dim node = DirectCast(root.FindToken(root.ToFullString().IndexOf("Return")).Parent, StatementSyntax)
            Dim dataFlowAnalysis = model.AnalyzeDataFlow(node, node)
            Assert.True(dataFlowAnalysis.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("v1, v2, v3, v4, v5", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("v1, v2, v3, v4, v5", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("v0, v1, v2, v3, v4, v5", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub XmlMemberAccess()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
      <compilation>
          <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Module M
    Function F() As Object
        Dim x = <a><b><c d="e"/></b></a>
        Return x.<b>...<c>.@<d>
    End Function
End Module
    ]]></file>
      </compilation>, additionalRefs:=XmlReferences)
            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim root = tree.GetCompilationUnitRoot()
            Dim node = DirectCast(root.FindToken(root.ToFullString().IndexOf("Return")).Parent, StatementSyntax)
            Dim dataFlowAnalysis = model.AnalyzeDataFlow(node, node)
            Assert.True(dataFlowAnalysis.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub GenericStructureCycle()
            Dim source =
                <compilation>
                    <file name="c.vb"><![CDATA[
Structure S(Of T)
    Public F As S(Of S(Of T))
End Structure
Module M
    Sub M()
        Dim o As S(Of Object)
    End Sub
End Module
    ]]></file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlib(source)
            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim root = tree.GetCompilationUnitRoot()
            Dim node = DirectCast(root.FindToken(root.ToFullString().IndexOf("Dim")).Parent, StatementSyntax)
            Dim dataFlowAnalysis = model.AnalyzeDataFlow(node, node)
            Assert.True(dataFlowAnalysis.Succeeded)
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <WorkItem(529322, "DevDiv")>
        <Fact(Skip:="529322")>
        Public Sub GenericStructureCycleFromMetadata()
            Dim ilSource = <![CDATA[
.class public sealed S<T> extends System.ValueType
{
  .field public valuetype S<valuetype S<!T>> F
}
]]>.Value
            Dim source =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Sub M()
        Dim o As S(Of Object)
    End Sub
End Module
    ]]></file>
                </compilation>
            Dim compilation = CreateCompilationWithCustomILSource(source, ilSource)
            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim root = tree.GetCompilationUnitRoot()
            Dim node = DirectCast(root.FindToken(root.ToFullString().IndexOf("Dim")).Parent, StatementSyntax)
            Dim dataFlowAnalysis = model.AnalyzeDataFlow(node, node)
            Assert.True(dataFlowAnalysis.Succeeded)
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

#End Region

#Region "With Statement"

        <Fact()>
        Public Sub WithStatement_Expression_RValue_1()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        With [| New SSS(Me.ToString(), i) |]
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_RValue_2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        With [| New SSS(Me.ToString(), i) |]
            .A = ""
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_RValue_3()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        With [| New SSS(Me.ToString(), i) |]
            Dim s As Action = Sub()
                                .A = ""
                              End Sub
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("i, Me, s", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_LValue_1()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        Dim x As New SSS(Me.ToString(), i)
        With [| x |]
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_LValue_2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        Dim x As New SSS(Me.ToString(), i)
        With [| x |]
            .A = ""
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("i, Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_LValue_2_()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        Dim x As New SSS(Me.ToString(), i)
        With [| x |]
            .A = ""
            Dim a = .A
            Dim b = .B
            .B = 1
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("i, Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("a, b, i, Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_LValue_2a()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        Dim x As New SSS(Me.ToString(), i)
        With x 
            [| .A = "" |]
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("i, Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_LValue_2b()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        Dim x As New SSS(Me.ToString(), i)
        With x 
            [| .B = "" |]
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("i, Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_LValue_3()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        Dim x As New SSS(Me.ToString(), i)
        With [| x |]
            Dim s As Action = Sub()
                                .A = ""
                              End Sub
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("i, Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_LValue_4()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS
    Public A As String
    Public B As Integer
End Structure

Structure SSS
    Public S As SSSS
nd Structure

Class Clazz
    Sub TEST()
        Dim x As New SSS()
        With [| x.S |]
            Dim s As Action = Sub()
                                .A = ""
                              End Sub
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_LValue_4a()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS
    Public A As String
    Public B As Integer
End Structure

Structure SSS
    Public S As SSSS
nd Structure

Class Clazz
    Sub TEST()
        Dim x As New SSS()
        With  [| x |] .S 
            Dim s As Action = Sub()
                                 .A = "" 
                              End Sub
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_LValue_4b()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS
    Public A As String
    Public B As Integer
End Structure

Structure SSS
    Public S As SSSS
nd Structure

Class Clazz
    Sub TEST()
        Dim x As New SSS()
        With  x.S 
            Dim s As Action = Sub()
                                [| .A = "" |]
                              End Sub
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_LValue_4c()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS
    Public A As String
    Public B As Integer
End Structure

Structure SSS
    Public S As SSSS
nd Structure

Class Clazz
    Sub TEST()
        Dim x As New SSS()
        With  x.S 
            Dim s As Action = Sub()
                                [| .A |] = "" 
                              End Sub
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_LValue_4d()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS3
    Public A As String
    Public B As Integer
End Structure

Structure SSSS2
    Public S3 As SSSS3
End Structure

Structure SSSS
    Public S2 As SSSS2
End Structure

Structure SSS
    Public S As SSSS
End Structure

Class Clazz
    Sub TEST()
        Dim x As New SSS()
        With x.S 
            With .S2
                With .S3
                    Dim s As Action = Sub()
                                        [| .A  = "" |]
                                      End Sub
                End With
            End With
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_LValue_4e()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS3
    Public A As String
    Public B As Integer
End Structure

Structure SSSS2
    Public S3 As SSSS3
End Structure

Structure SSSS
    Public S2 As SSSS2
End Structure

Structure SSS
    Public S As SSSS
End Structure

Class Clazz
    Sub TEST()
        Dim x As New SSS()
        With x.S 
            With .S2
                With .S3
                    Dim s As Action = Sub()
                                        Dim xyz = [| .A |]
                                      End Sub
                End With
            End With
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x, xyz", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_LValue_4f()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS3
    Public A As String
    Public B As Integer
End Structure

Structure SSSS2
    Public S3 As SSSS3
End Structure

Structure SSSS
    Public S2 As SSSS2
End Structure

Structure SSS
    Public S As SSSS
End Structure

Class Clazz
    Sub TEST()
        Dim x As New SSS()
        With [| x.S.S2 |].S3
            Dim s As Action = Sub()
                                .A = ""
                              End Sub
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_Expression_LValue_4g()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS3
    Public A As String
    Public B As Integer
End Structure

Structure SSSS2
    Public S3 As SSSS3
End Structure

Structure SSSS
    Public S2 As SSSS2
End Structure

Class SSS
    Public S As SSSS
End Class

Class Clazz
    Sub TEST()
        Dim x As New SSS()
        With [| x.S.S2 |].S3
            Dim s As Action = Sub()
                                .A = ""
                              End Sub
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_MeReference_1()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS3
    Public A As String
    Public B As Integer
End Structure

Structure SSSS2
    Public S3 As SSSS3
End Structure

Structure SSSS
    Public S2 As SSSS2
End Structure

Structure SSS
    Public S As SSSS
End Structure

Class Clazz
    Public x As New SSS()
    Sub TEST()
        With [| x.S.S2 |].S3
            Dim s As Action = Sub()
                                .A = ""
                              End Sub
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_MeReference_2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS3
    Public A As String
    Public B As Integer
End Structure

Structure SSSS2
    Public S3 As SSSS3
End Structure

Structure SSSS
    Public S2 As SSSS2
End Structure

Structure SSS
    Public S As SSSS
End Structure

Structure Clazz
    Public x As New SSS()
    Sub TEST()
        With [| x.S |].S2
            With .S3
                .A = ""
            End With
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_MeReference_3()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS3
    Public A As String
    Public B As Integer
End Structure

Structure SSSS2
    Public S3 As SSSS3
End Structure

Structure SSSS
    Public S2 As SSSS2
End Structure

Structure SSS
    Public S As SSSS
End Structure

Structure Clazz
    Public x As New SSS()
    Sub TEST()
        With x.S.S2
            With .S3
                [| .A = "" |]
            End With
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_ComplexExpression_1()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS3
    Public A As String
    Public B As Integer
End Structure

Structure SSSS2
    Public S3 As SSSS3
End Structure

Structure SSSS
    Public S2 As SSSS2
End Structure

Structure SSS
    Public S As SSSS
End Structure

Class Clazz
    Public x As New SSS()
    Sub TEST()
        With DirectCast(Function()
                            Return [| Me.x |]
                        End Function, Func(Of SSS))()
            With .S.S2
                Dim a = .S3.A
            End With
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("a, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub WithStatement_ComplexExpression_2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS3
    Public A As String
    Public B As Integer
End Structure

Structure SSSS2
    Public S3 As SSSS3
End Structure

Structure SSSS
    Public S2 As SSSS2
End Structure

Structure SSS
    Public S As SSSS
End Structure

Class Clazz
    Public x As New SSS()
    Sub TEST()
        Dim arr(,) As SSS

        With arr(1,
                 [| DirectCast(Function()
                            Return x 
                        End Function, Func(Of SSS)) |] ().S.S2.S3.B).S
            Dim a = .S2.S3.A
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("arr, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("a, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

#End Region

#Region "Select Statement"

        <Fact()>
        Public Sub TestSelectCase_Empty()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_Empty">
          <file name="a.b">
Module Program
    Sub Main()
        Dim obj As Object = 0

        [|
        Select Case obj
        End Select
        |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestSelectCase_SingleCaseBlock_01()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_SingleCaseBlock_01">
          <file name="a.b">
Module Program
    Sub Main()
        Dim obj1 As Object = 0
        Dim obj2 As Object = 0
        Dim obj3 As Object

        [|
            Select Case obj1
                Case obj2
                    Dim obj4 = 1
                    obj3 = obj4
            End Select
        |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestSelectCase_SingleCaseBlock_02()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_SingleCaseBlock_02">
          <file name="a.b">
Module Program
    Sub Main()
        Dim obj1 As Object = 0
        Dim obj2 As Object = 0
        Dim obj3 As Object

            Select Case obj1
                Case obj2
            [|
                    Dim obj4 = 1
                    obj3 = obj4
            |]
            End Select
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestSelectCase_CaseBlocksWithCaseElse_01()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_CaseBlocksWithCaseElse_01">
          <file name="a.b">
Module Program
    Sub Main()
        Dim obj1 As Object = 0
        Dim obj2 As Object = 0
        Dim obj3 As Object
        Dim obj4 As Object

        [|
            Select Case obj1
                Case obj2
                    Dim obj5 = 1
                    obj3 = obj5
                    obj4 = obj5
                Case Else
                    Dim obj5 = 2
                    obj2 = obj5
                    obj4 = obj5
            End Select
        |]

        obj1 = obj3 + obj4
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj5, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2, obj5, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj2, obj3, obj4, obj5, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestSelectCase_CaseBlocksWithCaseElse_01_CaseElseRegion()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_CaseBlocksWithCaseElse_01_CaseElseRegion">
          <file name="a.b">
Module Program
    Sub Main()
        Dim obj1 As Object = 0
        Dim obj2 As Object = 0
        Dim obj3 As Object
        Dim obj4 As Object

            Select Case obj1
                Case obj2
                    Dim obj5 = 1
                    obj3 = obj5
                    obj4 = obj5

                Case Else
            [|
                    Dim obj5 = 2
                    obj2 = obj5
                    obj4 = obj5
            |]
            End Select

        obj1 = obj3 + obj4
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("obj2, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj2, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj1, obj2, obj3, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2, obj3, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestSelectCase_CaseBlocksWithCaseElse_02()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_CaseBlocksWithCaseElse_02">
          <file name="a.b">
Module Program
    Sub Main()
        Dim obj1 As Object = 0
        Dim obj2 As Object = 0
        Dim obj3 As Object

        [|
            Select Case obj1
                Case obj2
                    Dim obj4 = 1
                    obj3 = obj4
                Case Else
            End Select
        |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestSelectCase_CaseBlockWithCaseElse_03()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_CaseBlockWithCaseElse_03">
          <file name="a.b">
Module Program
    Sub Main()
        Dim obj1 As Object = 0
        Dim obj2 As Object = 0
        Dim obj3 As Object

        [|
            Select Case obj1
                Case obj2
                  LabelCase:
                    Dim obj4 = 1
                    obj3 = obj4
                Case Else
                    Goto LabelCase
            End Select
        |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestSelectCase_CaseBlocksWithoutCaseElse_01()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_CaseBlocksWithoutCaseElse_01">
          <file name="a.b">
Module Program
    Sub Main()
        Dim obj1 As Object = 0
        Dim obj2 As Object = 0
        Dim obj3 As Object
        Dim obj4 As Object

        [|
            Select Case obj1
                Case obj2
                    Dim obj5 = 1
                    obj3 = obj5
                    obj4 = obj5
                Case obj3
                    Dim obj5 = 2
                    obj2 = obj5
                    obj4 = obj5
            End Select
        |]

        obj1 = obj3 + obj4
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj5, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2, obj3", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2, obj3, obj5, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj2, obj3, obj4, obj5, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestSelectCase_CaseBlockWithoutCaseElse_02()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_CaseBlockWithoutCaseElse_02">
          <file name="a.b">
Module Program
    Sub Main()
        Dim obj1 As Object = 0
        Dim obj2 As Object = 0
        Dim obj3 As Object

        [|
            Select Case obj1
                Case obj2
                  LabelCase:
                    Dim obj4 = 1
                    obj3 = obj4
                Case obj3
                    Goto LabelCase
            End Select
        |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2, obj3", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2, obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestSelectCase_CaseStatementRegion()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestSelectCase_CaseStatementRegion">
          <file name="a.b">
Module Program
    Sub Main()
        Dim obj1 As Object = 0
        Dim obj2 As Object = 0
        Dim obj3 As Object

        Select Case obj1
            Case [|obj2|]
                obj3 = 0
        End Select
    End Sub
End Module
  </file>
      </compilation>)
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj1", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2, obj3", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestSelectCase_Error_CaseElseBeforeCaseBlock()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_Error_CaseElseBeforeCaseBlock">
          <file name="a.b">
Module Program
    Sub Main()
        Dim obj1 As Object = 0
        Dim obj2 As Object = 0
        Dim obj3 As Object
        Dim obj4 As Object

            Select Case obj1
                Case Else
            [|
                    Dim obj5 = 2
                    obj2 = obj5
                    obj4 = obj5
            |]
                Case obj2
                    Dim obj5 = 1
                    obj3 = obj5
                    obj4 = obj5
            End Select

        obj1 = obj3 + obj4
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("obj2, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj2, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj1, obj2, obj3, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2, obj3, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(529089, "DevDiv")>
        <Fact>
        Public Sub CaseClauseNotReachable()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_Error_CaseElseBeforeCaseBlock">
          <file name="a.b">
Module Program
    Sub Main(args As String())
        Dim x = 10
        Select Case 5
            Case 10
                [|x = x + 1|]
        End Select
    End Sub
End Module
      </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.StartPointIsReachable)
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(543402, "DevDiv")>
        <Fact()>
        Public Sub EndSelectStatement()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        Select Case 99
        End Select
    End Sub
End Module
      </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("End Select")).Parent, StatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtNode, stmtNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(543434, "DevDiv")>
        <Fact()>
        Public Sub SelectCaseStatement()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        Select Case 99
        End Select
    End Sub
End Module
      </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("Select Case")).Parent, StatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtNode, stmtNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <Fact, WorkItem(543492, "DevDiv")>
        Public Sub MyBaseExpressionSyntax()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Public Class BaseClass
    Public Overridable Sub MyMeth()
    End Sub
End Class

Public Class MyClass : Inherits BaseClass
    Public Overrides Sub MyMeth()
        MyBase.MyMeth()
    End Sub
    Public Sub OtherMeth()
        Dim f = Function() MyBase
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib(source)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim invocation = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)().Single()
            Dim flowAnalysis = model.AnalyzeDataFlow(invocation)
            Assert.Empty(flowAnalysis.Captured)
            Assert.Equal("[Me] As [MyClass]", flowAnalysis.DataFlowsIn.Single().ToTestDisplayString())
            Assert.Empty(flowAnalysis.DataFlowsOut)
            Assert.Equal("[Me] As [MyClass]", flowAnalysis.ReadInside.Single().ToTestDisplayString())
            Assert.Empty(flowAnalysis.WrittenInside)
            Assert.Equal("[Me] As [MyClass]", flowAnalysis.WrittenOutside.Single().ToTestDisplayString())

            Dim lambda = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of LambdaExpressionSyntax)().Single()
            flowAnalysis = model.AnalyzeDataFlow(lambda)
            Assert.Equal("[Me] As [MyClass]", flowAnalysis.Captured.Single().ToTestDisplayString())
            Assert.Equal("[Me] As [MyClass]", flowAnalysis.DataFlowsIn.Single().ToTestDisplayString())
            Assert.Empty(flowAnalysis.DataFlowsOut)
            Assert.Equal("[Me] As [MyClass]", flowAnalysis.ReadInside.Single().ToTestDisplayString())
            Assert.Empty(flowAnalysis.WrittenInside)
            Assert.Equal("f, Me", GetSymbolNamesSortedAndJoined(flowAnalysis.WrittenOutside))
        End Sub

#End Region

#Region "Helpers"

        Private Shared Function GetSourceXElementFromTemplate(code As XCData) As XElement
            Return <compilation>
                       <file name="a.vb">
Option Infer On
Option Explicit Off
Option Strict Off

Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Sub Main()
<%= code.Value %>
    End Sub
End Module
                       </file>
                   </compilation>
        End Function

        Private Sub VerifyReDimDataFlowAnalysis(
                code As XCData,
                Optional alwaysAssigned() As String = Nothing,
                Optional captured() As String = Nothing,
                Optional dataFlowsIn() As String = Nothing,
                Optional dataFlowsOut() As String = Nothing,
                Optional readInside() As String = Nothing,
                Optional readOutside() As String = Nothing,
                Optional variablesDeclared() As String = Nothing,
                Optional writtenInside() As String = Nothing,
                Optional writtenOutside() As String = Nothing)
            VerifyDataFlowAnalysis(GetSourceXElementFromTemplate(code),
                                   alwaysAssigned,
                                   captured,
                                   dataFlowsIn,
                                   dataFlowsOut,
                                   readInside,
                                   readOutside,
                                   variablesDeclared,
                                   writtenInside,
                                   writtenOutside)
        End Sub

#End Region

    End Class

End Namespace
