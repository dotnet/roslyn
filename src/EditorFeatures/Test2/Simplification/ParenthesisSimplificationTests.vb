' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    Public Class ParenthesisSimplificationTests
        Inherits AbstractSimplificationTests

#Region "VB Array Literal tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_DontRemoveInJaggedArrayLiteral()
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

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_DontRemoveInCollectionInitializer()
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

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_RemoveInCollectionInitializer1()
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

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_RemoveInCollectionInitializer2()
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

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_RemoveInCollectionInitializer3()
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

            Test(input, expected)

        End Sub

#End Region

#Region "VB Binary Expressions"
        <WorkItem(633582)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyOnLeftSideOfBinaryExpression()
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

            Test(input, expected)

        End Sub

        <WorkItem(633582)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyOnRightSideOfBinaryExpressionIfOperatorsAreCommutative()
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

            Test(input, expected)

        End Sub

        <WorkItem(633582)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_DontSimplifyOnRightSideOfBinaryExpressionIfOperatorsAreNotCommutative()
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

            Test(input, expected)

        End Sub


        <WorkItem(738826)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub SimplifyParenthesisedGetTypeOperator()
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

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub SimplifyParenthesisedAroundNameOfExpression()
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

            Test(input, expected)

        End Sub


#End Region

#Region "C#"
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_Unnecessary_Parenthesis_in_Array_Index()
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
        var z = new Foo {
                            A = arr[{|Simplify:(0)|}]
                        };
     }
}
class Foo{
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
        var z = new Foo {
                            A = arr[0]
                        };
     }
}
class Foo{
    public int A { get; set; }
}
</code>

            Test(input, expected)

        End Sub

        <WorkItem(619292)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_RemoveParensInJaggedArrayLiteral()
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

            Test(input, expected)

        End Sub

        <WorkItem(619294)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_RemoveParensInCollectionInitializer()
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

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_RemoveParensInCollectionInitializer2()
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

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_RemoveParensOnVariableDeclaration()
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

            Test(input, expected)

        End Sub

        <WorkItem(633582)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyParenthesesAroundExpressionInQueryClause()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System.Linq;

class foo
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

class foo
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

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyParenthesesInsideExceptionFilter()
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

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyParenthesesInsideInterpolation1()
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

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyParenthesesInsideInterpolation2()
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

            Test(input, expected)

        End Sub

        <WorkItem(724, "#724")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyParenthesesInsideInterpolation3()
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

            Test(input, expected)

        End Sub

        <WorkItem(724, "#724")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyParenthesesInsideInterpolation4()
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

            Test(input, expected)

        End Sub

        <WorkItem(724, "#724")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyParenthesesInsideInterpolation5()
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

            Test(input, expected)

        End Sub

        <WorkItem(724, "#724")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyParenthesesInsideInterpolation6()
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

            Test(input, expected)

        End Sub

#End Region

#Region "C# Binary Expressions"

        <WorkItem(633582)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyOnLeftSideOfBinaryExpression()
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

            Test(input, expected)

        End Sub

        <WorkItem(633582)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyOnRightSideOfBinaryExpressionIfOperatorsAreCommutative()
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

            Test(input, expected)

        End Sub

        <WorkItem(633582)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyOnRightSideOfBinaryExpressionForAssignment()
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

            Test(input, expected)

        End Sub

        <WorkItem(633582)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyOnRightSideOfBinaryExpressionForNullCoalescing()
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

            Test(input, expected)

        End Sub

        <WorkItem(633582)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_DontSimplifyOnRightSideOfBinaryExpressionIfOperatorsAreNotCommutative()
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

            Test(input, expected)

        End Sub
#End Region

        <WorkItem(2211, "https://github.com/dotnet/roslyn/issues/2211")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_DontRemoveParensAroundConditionalAccessExpressionIfParentIsMemberAccessExpression()
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

            Test(input, expected)
        End Sub

        <WorkItem(2211, "https://github.com/dotnet/roslyn/issues/2211")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_DontRemoveParensAroundConditionalAccessExpressionIfParentIsMemberAccessExpression()
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

            Test(input, expected)
        End Sub

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_RemoveParenthesesAroundEmptyXmlElement()
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

            Test(input, expected)
        End Sub

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_RemoveParenthesesAroundEmptyXmlElementInInvocation()
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

            Test(input, expected)
        End Sub

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_RemoveParenthesesAroundXmlElement()
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

            Test(input, expected)
        End Sub

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_RemoveParenthesesAroundXmlElementInInvocation()
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

            Test(input, expected)
        End Sub

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_DontRemoveParenthesesAroundEmptyXmlElementWhenPreviousTokenIsLessThan()
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

            Test(input, expected)
        End Sub

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_DontRemoveParenthesesAroundEmptyXmlElementWhenPreviousTokenIsGreaterThan()
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

            Test(input, expected)
        End Sub

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_DontRemoveParenthesesAroundXmlElementWhenPreviousTokenIsLessThan()
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

            Test(input, expected)
        End Sub

        <WorkItem(4490, "https://github.com/dotnet/roslyn/issues/4490")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_DontRemoveParenthesesAroundXmlElementWhenPreviousTokenIsGreaterThan()
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

            Test(input, expected)
        End Sub
    End Class
End Namespace
