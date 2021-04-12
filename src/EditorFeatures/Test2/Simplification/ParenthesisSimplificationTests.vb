﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    Public Class ParenthesisSimplificationTests
        Inherits AbstractSimplificationTests

#Region "VB"
        <WorkItem(2211, "https://github.com/dotnet/roslyn/issues/2211")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontRemoveParensAroundConditionalAccessExpressionIfParentIsMemberAccessExpression() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Collections.Generic

Module Program
    Sub Main()
        Dim x = New List(Of Integer)
        Dim i = {|Simplify:({|Simplify:CType(x?.Count, Integer?)|})|}.GetValueOrDefault()
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
Imports System.Collections.Generic

Module Program
    Sub Main()
        Dim x = New List(Of Integer)
        Dim i = (x?.Count).GetValueOrDefault()
    End Sub
End Module
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_RemoveParenthesesAroundEmptyXmlElement() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Xml.Linq

Class C
    Sub M()
        Dim x = {|Simplify:(&lt;xml/&gt;)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Xml.Linq

Class C
    Sub M()
        Dim x = &lt;xml/&gt;
    End Sub
End Class
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_RemoveParenthesesAroundEmptyXmlElementInInvocation() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Xml.Linq

Class C
    Sub M(xml as XElement)
        Dim x = M({|Simplify:(&lt;xml/&gt;)|})
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Xml.Linq

Class C
    Sub M(xml as XElement)
        Dim x = M(&lt;xml/&gt;)
    End Sub
End Class
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_RemoveParenthesesAroundXmlElement() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Xml.Linq

Class C
    Sub M()
        Dim x = {|Simplify:(&lt;xml&gt;&lt;/xml&gt;)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Xml.Linq

Class C
    Sub M()
        Dim x = &lt;xml&gt;&lt;/xml&gt;
    End Sub
End Class
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_RemoveParenthesesAroundXmlElementInInvocation() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Xml.Linq

Class C
    Sub M(xml as XElement)
        Dim x = M({|Simplify:(&lt;xml&gt;&lt;/xml&gt;)|})
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Xml.Linq

Class C
    Sub M(xml as XElement)
        Dim x = M(&lt;xml&gt;&lt;/xml&gt;)
    End Sub
End Class
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontRemoveParenthesesAroundEmptyXmlElementWhenPreviousTokenIsLessThan() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Xml.Linq

Class C
    Sub M()
        Dim x = 1 &lt; {|Simplify:(&lt;xml/&gt;)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Xml.Linq

Class C
    Sub M()
        Dim x = 1 &lt; (&lt;xml/&gt;)
    End Sub
End Class
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontRemoveParenthesesAroundEmptyXmlElementWhenPreviousTokenIsGreaterThan() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Xml.Linq

Class C
    Sub M()
        Dim x = 1 &gt; {|Simplify:(&lt;xml/&gt;)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Xml.Linq

Class C
    Sub M()
        Dim x = 1 &gt; (&lt;xml/&gt;)
    End Sub
End Class
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontRemoveParenthesesAroundXmlElementWhenPreviousTokenIsLessThan() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Xml.Linq

Class C
    Sub M()
        Dim x = 1 &lt; {|Simplify:(&lt;xml&gt;&lt;/xml&gt;)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Xml.Linq

Class C
    Sub M()
        Dim x = 1 &lt; (&lt;xml&gt;&lt;/xml&gt;)
    End Sub
End Class
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontRemoveParenthesesAroundXmlElementWhenPreviousTokenIsGreaterThan() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Xml.Linq

Class C
    Sub M()
        Dim x = 1 &gt; {|Simplify:(&lt;xml&gt;&lt;/xml&gt;)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Xml.Linq

Class C
    Sub M()
        Dim x = 1 &gt; (&lt;xml&gt;&lt;/xml&gt;)
    End Sub
End Class
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(40442, "https://github.com/dotnet/roslyn/issues/40442")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_RemoveEmptyArgumentListOnMethodGroup() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Public Class TestClass
    Shared Function GetFour() As Integer
        Return 4
    End Function
    
    Public Shared Sub Main()
        Dim inferredMethodGroup = MyIf({|SimplifyExtension:TestClass.GetFour()|})
        System.Console.WriteLine(inferredMethodGroup)
    End Sub
    
    Public Shared Function MyIf(y As Integer)
        Return y
    End Function
    
    Public Shared Function MyIf(y As Object)
        Return y
    End Function
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Public Class TestClass
    Shared Function GetFour() As Integer
        Return 4
    End Function
    
    Public Shared Sub Main()
        Dim inferredMethodGroup = MyIf(TestClass.GetFour)
        System.Console.WriteLine(inferredMethodGroup)
    End Sub
    
    Public Shared Function MyIf(y As Integer)
        Return y
    End Function
    
    Public Shared Function MyIf(y As Object)
        Return y
    End Function
End Class
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(40442, "https://github.com/dotnet/roslyn/issues/40442")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DoNotRemoveEmptyArgumentListOnInlineLambda() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Public Class TestClass
    Public Shared Sub Main()
        Dim inferredInlineFunction = MyIf({|SimplifyExtension:Function()
                Return 5
            End Function()|})
        System.Console.WriteLine(inferredInlineFunction)
    End Sub
    
    Public Shared Function MyIf(y As Integer)
        Return y
    End Function
    
    Public Shared Function MyIf(y As Object)
        Return y
    End Function
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Public Class TestClass
    Public Shared Sub Main()
        Dim inferredInlineFunction = MyIf(Function()
                Return 5
            End Function())
        System.Console.WriteLine(inferredInlineFunction)
    End Sub
    
    Public Shared Function MyIf(y As Integer)
        Return y
    End Function
    
    Public Shared Function MyIf(y As Object)
        Return y
    End Function
End Class
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(40442, "https://github.com/dotnet/roslyn/issues/40442")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DoNotRemoveEmptyArgumentListOnLocalLambda() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Public Class TestClass
    Public Shared Sub Main()
        Dim localDelegate = Function() As Integer
                Return 6
            End Function

        Dim inferredLocalDelegate = MyIf({|SimplifyExtension:localDelegate()|})
        System.Console.WriteLine(inferredLocalDelegate)
    End Sub
    
    Public Shared Function MyIf(y As Integer)
        Return y
    End Function
    
    Public Shared Function MyIf(y As Object)
        Return y
    End Function
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Public Class TestClass
    Public Shared Sub Main()
        Dim localDelegate = Function() As Integer
                Return 6
            End Function

        Dim inferredLocalDelegate = MyIf(localDelegate())
        System.Console.WriteLine(inferredLocalDelegate)
    End Sub
    
    Public Shared Function MyIf(y As Integer)
        Return y
    End Function
    
    Public Shared Function MyIf(y As Object)
        Return y
    End Function
End Class
</code>

            Await TestAsync(input, expected)
        End Function
#End Region

#Region "VB Array Literal tests"

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontRemoveInJaggedArrayLiteral() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        Dim y = {{|Simplify:({1})|}}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Sub M()
        Dim y = {({1})}
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontRemoveInCollectionInitializer() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Collections.Generic
Class C
    Sub M()
        Dim l As New List(Of Integer()) From {{|Simplify:({1})|}}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Collections.Generic
Class C
    Sub M()
        Dim l As New List(Of Integer()) From {({1})}
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_RemoveInCollectionInitializer1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Collections.Generic
Class C
    Sub M()
        Dim l As New List(Of Integer()) From {{{|Simplify:({1})|}}}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Collections.Generic
Class C
    Sub M()
        Dim l As New List(Of Integer()) From {{{1}}}
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_RemoveInCollectionInitializer2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Collections.Generic
Class C
    Sub M()
            Dim d As New Dictionary(Of Integer(), Integer()) From {{{|Simplify:({1, 2})|}, ({1, 2})}}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Collections.Generic
Class C
    Sub M()
            Dim d As New Dictionary(Of Integer(), Integer()) From {{{1, 2}, ({1, 2})}}
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_RemoveInCollectionInitializer3() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Collections.Generic
Class C
    Sub M()
            Dim d As New Dictionary(Of Integer(), Integer()) From {{({1, 2}), {|Simplify:({1, 2})|}}}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Collections.Generic
Class C
    Sub M()
            Dim d As New Dictionary(Of Integer(), Integer()) From {{({1, 2}), {1, 2}}}
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

#End Region

#Region "VB Binary Expressions"
        <WorkItem(633582, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633582")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_SimplifyOnLeftSideOfBinaryExpression() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
 
Module Program
    Sub Main()
        Console.WriteLine({|Simplify:(5 - 1)|} + 2)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
 
Module Program
    Sub Main()
        Console.WriteLine(5 - 1 + 2)
    End Sub
End Module
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(633582, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633582")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_SimplifyOnRightSideOfBinaryExpressionIfOperatorsAreCommutative() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
 
Module Program
    Sub Main()
        Console.WriteLine(5 + {|Simplify:(1 + 2)|})
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
 
Module Program
    Sub Main()
        Console.WriteLine(5 + 1 + 2)
    End Sub
End Module
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(633582, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633582")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontSimplifyOnRightSideOfBinaryExpressionIfOperatorsAreNotCommutative() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
 
Module Program
    Sub Main()
        Console.WriteLine(5 - {|Simplify:(1 + 2)|})
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
 
Module Program
    Sub Main()
        Console.WriteLine(5 - (1 + 2))
    End Sub
End Module
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(738826, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/738826")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyParenthesisedGetTypeOperator() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Collections

Module M
    Sub Main()
        Dim list As New ArrayList()
        System.Console.WriteLine(String.Join(",", list.ToArray({|Simplify:(GetType(String))|})))
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Collections

Module M
    Sub Main()
        Dim list As New ArrayList()
        System.Console.WriteLine(String.Join(",", list.ToArray(GetType(String))))
    End Sub
End Module
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyParenthesisedAroundNameOfExpression() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M(i As Integer)
        Call {|Simplify:(NameOf(i))|}.ToString()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Sub M(i As Integer)
        Call NameOf(i).ToString()
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

#End Region

#Region "C#"
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Unnecessary_Parenthesis_in_Array_Index() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    static void Main(string[] args)
    {
        var arr = new int[1];
        var y = arr[0];
        var z = new Goo {
                            A = arr[{|Simplify:(0)|}]
                        };
     }
}
class Goo{
    public int A { get; set; }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class Program
{
    static void Main(string[] args)
    {
        var arr = new int[1];
        var y = arr[0];
        var z = new Goo {
                            A = arr[0]
                        };
     }
}
class Goo{
    public int A { get; set; }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(619292, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/619292")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_RemoveParensInJaggedArrayLiteral() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        int[][] jaggedArray = 
        {
            new int[] {{|Simplify:(1),3,5,7,9|}},
            new int[] {0,2,4,6},
            new int[] {11,22}
        };
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        int[][] jaggedArray = 
        {
            new int[] {1,3,5,7,9},
            new int[] {0,2,4,6},
            new int[] {11,22}
        };
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(619294, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/619294")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_RemoveParensInCollectionInitializer() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System.Collections.Generic;
class C
{
    void M()
    {
        var d = new List&lt;int&gt; { {|Simplify:(1)|}, 2, {|Simplify:(3)|} };
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System.Collections.Generic;
class C
{
    void M()
    {
        var d = new List&lt;int&gt; { 1, 2, 3 };
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_RemoveParensInCollectionInitializer2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var l = new List&lt;int[]&gt;
        {
            {{|Simplify:(new int[]{1})|}}
        };
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var l = new List&lt;int[]&gt;
        {
            {new int[]{1}}
        };
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_RemoveParensOnVariableDeclaration() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var d = {|Simplify:(1)|};
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var d = 1;
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(633582, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633582")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyParenthesesAroundExpressionInQueryClause() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System.Linq;

class goo
{
    void www()
    {
        var aaa =
            from rrr in new[] { 1, 2, 3 }
            let bbb = false
            where {|Simplify:(1 + rrr > 'a')|}
            select bbb;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System.Linq;

class goo
{
    void www()
    {
        var aaa =
            from rrr in new[] { 1, 2, 3 }
            let bbb = false
            where 1 + rrr > 'a'
            select bbb;
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyParenthesesAroundThisInCastExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class C : IEquatable<C>
{
    bool IEquatable<C>.Equals(C other)
    {
        return true;
    }

    public override bool Equals(object obj)
    {
        return ((IEquatable<C>){|Simplify:(this)|}).Equals(obj as C);
    }
}
        ]]></Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class C : IEquatable<C>
{
    bool IEquatable<C>.Equals(C other)
    {
        return true;
    }

    public override bool Equals(object obj)
    {
        return ((IEquatable<C>)this).Equals(obj as C);
    }
}
]]></code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyParenthesesInsideExceptionFilter() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M(int i)
    {
        try
        {
        }
        catch when ({|Simplify:(i == 42)|})
        {
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M(int i)
    {
        try
        {
        }
        catch when (i == 42)
        {
        }
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyParenthesesInsideInterpolation1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var x = $"{{|Simplify:(true ? 1 : 0)|}}";
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var x = $"{(true ? 1 : 0)}";
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyParenthesesInsideInterpolation2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var x = $"{{|Simplify:(true ? 1 : 0)|}:x}";
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var x = $"{(true ? 1 : 0):x}";
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(724, "https://github.com/dotnet/roslyn/issues/724")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyParenthesesInsideInterpolation3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var x = $"{{|Simplify:(global::System.Guid.Empty)|}}";
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var x = $"{(global::System.Guid.Empty)}";
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(724, "https://github.com/dotnet/roslyn/issues/724")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyParenthesesInsideInterpolation4() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var g = System.Guid.NewGuid();
        var x = $"{g == {|Simplify:(global::System.Guid.Empty)|}}";
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var g = System.Guid.NewGuid();
        var x = $"{g == (global::System.Guid.Empty)}";
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(724, "https://github.com/dotnet/roslyn/issues/724")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyParenthesesInsideInterpolation5() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var g = System.Guid.NewGuid();
        var x = $"{{|Simplify:(g == (global::System.Guid.Empty))|}}";
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var g = System.Guid.NewGuid();
        var x = $"{g == (global::System.Guid.Empty)}";
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(724, "https://github.com/dotnet/roslyn/issues/724")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyParenthesesInsideInterpolation6() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var x = 19;
        var y = 23;
        var z = $"{{|Simplify:((true ? x : y) == 42)|}}";
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var x = 19;
        var y = 23;
        var z = $"{(true ? x : y) == 42}";
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(2211, "https://github.com/dotnet/roslyn/issues/2211")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DontRemoveParensAroundConditionalAccessExpressionIfParentIsMemberAccessExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var x = new List&lt;int&gt;();
        int i = {|Simplify:({|Simplify:(int?)x?.Count|})|}.GetValueOrDefault();
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var x = new List&lt;int&gt;();
        int i = (x?.Count).GetValueOrDefault();
    }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(12600, "https://github.com/dotnet/roslyn/issues/12600")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_RemoveParensInExpressionBodiedProperty() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    object Value => {|Simplify:(new object())|};
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    object Value => new object();
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(12600, "https://github.com/dotnet/roslyn/issues/12600")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_RemoveParensInExpressionBodiedMethod() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    object GetValue() => {|Simplify:(new object())|};
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    object GetValue() => new object();
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(12600, "https://github.com/dotnet/roslyn/issues/12600")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_RemoveParensInLambdaExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    Func&lt;object&gt; Value => () => {|Simplify:(new object())|};
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    Func&lt;object&gt; Value => () => new object();
}
</code>

            Await TestAsync(input, expected)
        End Function

#End Region

#Region "C# Binary Expressions"

        <WorkItem(633582, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633582")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyOnLeftSideOfBinaryExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
 
class Program
{
    static void Main()
    {
        Console.WriteLine({|Simplify:(5 - 1)|} + 2);
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
 
class Program
{
    static void Main()
    {
        Console.WriteLine(5 - 1 + 2);
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(633582, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633582")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyOnRightSideOfBinaryExpressionIfOperatorsAreCommutative() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
 
class Program
{
    static void Main()
    {
        Console.WriteLine(5 + {|Simplify:(1 + 2)|});
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
 
class Program
{
    static void Main()
    {
        Console.WriteLine(5 + 1 + 2);
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(633582, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633582")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyOnRightSideOfBinaryExpressionForAssignment() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
 
class Program
{
    static void Main()
    {
        int y = 0;
        int x = y = {|Simplify:(1 + 2)|};
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
 
class Program
{
    static void Main()
    {
        int y = 0;
        int x = y = 1 + 2;
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(633582, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633582")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyOnRightSideOfBinaryExpressionForNullCoalescing() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
 
class Program
{
    static void Main()
    {
        object x = null;
        object y = null;
        object z = x ?? {|Simplify:(y ?? new object())|};
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
 
class Program
{
    static void Main()
    {
        object x = null;
        object y = null;
        object z = x ?? y ?? new object();
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(633582, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633582")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DontSimplifyOnRightSideOfBinaryExpressionIfOperatorsAreNotCommutative() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
 
class Program
{
    static void Main()
    {
        Console.WriteLine(5 - {|Simplify:(1 + 2)|});
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
 
class Program
{
    static void Main()
    {
        Console.WriteLine(5 - (1 + 2));
    }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(11958, "https://github.com/dotnet/roslyn/issues/11958")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyInStringConcatenationIfItWouldNotChangeMeaning1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var x = "" + {|Simplify:(1)|};
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var x = "" + 1;
    }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(11958, "https://github.com/dotnet/roslyn/issues/11958")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyInStringConcatenationIfItWouldNotChangeMeaning2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var x = "a" + {|Simplify:("b" + "c")|};
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var x = "a" + "b" + "c";
    }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(11958, "https://github.com/dotnet/roslyn/issues/11958")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DontSimplifyIfItWouldChangeStringConcatenation() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var x = "" + {|Simplify:(1 + 2)|};
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var x = "" + (1 + 2);
    }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(11958, "https://github.com/dotnet/roslyn/issues/11958")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DontSimplifyIfOperatorOverloadsWouldNoLongerByCalled() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    public static string operator +(C left, C right) => "";

    void M()
    {
        var x = "" + {|Simplify:(new C() + new C())|};
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    public static string operator +(C left, C right) => "";

    void M()
    {
        var x = "" + (new C() + new C());
    }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(11958, "https://github.com/dotnet/roslyn/issues/11958")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyIfBinaryExpressionTypeIsIdentityConversion() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    int Add(int a, int b, int c) => a + {|Simplify:(b + c)|};
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    int Add(int a, int b, int c) => a + b + c;
}
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(11958, "https://github.com/dotnet/roslyn/issues/11958")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_SimplifyIfBinaryExpressionTypeIsImplicitNumericConversion() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    int Add(int a, short b, short c) => a + {|Simplify:(b + c)|};
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    int Add(int a, short b, short c) => a + b + c;
}
</code>

            Await TestAsync(input, expected)
        End Function

#End Region

    End Class
End Namespace
