' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Expansion
    <Trait(Traits.Feature, Traits.Features.Expansion)>
    Public Class NameExpansionTests
        Inherits AbstractExpansionTest

#Region "C# Tests"

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604392")>
        Public Async Function TestNoExpansionForPropertyNamesOfObjectInitializers() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    static void Main()
    {
        int z = 1;
        var c = new C { {|Expand:X|} = { Y = { z } } };
    }
}
 
class C
{
    public dynamic X;
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class Program
{
    static void Main()
    {
        int z = 1;
        var c = new C { X = { Y = { z } } };
    }
}

class C
{
    public dynamic X;
}
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1913")>
        Public Async Function TestCSharp_SimpleIdentifierAliasExpansion_AliasBinds() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
namespace NS
{
    using Short = LongNamespace;
    class Test
    {
        public object Method1()
        {
            return (new {|Expand:Short|}.MyClass()).Prop;
        }
    }
}
namespace LongNamespace
{
    public class MyClass
    {
        public object Prop { get; }
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
namespace NS
{
    using Short = LongNamespace;
    class Test
    {
        public object Method1()
        {
            return (new global::LongNamespace.MyClass()).Prop;
        }
    }
}
namespace LongNamespace
{
    public class MyClass
    {
        public object Prop { get; }
    }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1913")>
        Public Async Function TestCSharp_SimpleIdentifierAliasExpansion_AliasDoesNotBind() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
namespace NS
{
    using Short = LongNamespace;
    class Test
    {
        public object Method1()
        {
            return (new {|Expand:Short|}.MyClass()).Prop;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
namespace NS
{
    using Short = LongNamespace;
    class Test
    {
        public object Method1()
        {
            return (new LongNamespace.MyClass()).Prop;
        }
    }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestCSharp_GenericNameExpansion_DoNotExpandAnonymousTypes() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    static void Mumble&lt;T&gt;(T anonymousType) { }

    static void M()
    {
        {|Expand:Mumble|}(new { x = 42 });
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    static void Mumble&lt;T&gt;(T anonymousType) { }

    static void M()
    {
        global::C.Mumble(new { x = 42 });
    }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestCSharp_LambdaParameter_DoNotExpandAnonymousTypes1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
class C
{
    static void Mumble&lt;T&gt;(T anonymousType, Action&lt;T, int&gt; lambda) { }

    static void M()
    {
        Mumble(new { x = 42 }, {|Expand:a => a.x|});
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
class C
{
    static void Mumble&lt;T&gt;(T anonymousType, Action&lt;T, int&gt; lambda) { }

    static void M()
    {
        Mumble(new { x = 42 }, a => a.x);
    }
}
</code>

            Await TestAsync(input, expected, expandParameter:=True)
        End Function

        <Fact>
        Public Async Function TestCSharp_LambdaParameter_DoNotExpandAnonymousTypes2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
class C
{
    static void Mumble&lt;T&gt;(T anonymousType, Action&lt;T, int, int&gt; lambda) { }

    static void M()
    {
        Mumble(new { x = 42 }, {|Expand:(a, y) => a.x|});
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
class C
{
    static void Mumble&lt;T&gt;(T anonymousType, Action&lt;T, int, int&gt; lambda) { }

    static void M()
    {
        Mumble(new { x = 42 }, (a, y) => a.x);
    }
}
</code>

            Await TestAsync(input, expected, expandParameter:=True)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11979")>
        Public Async Function TestCSharp_LambdaParameter_DoNotExpandAnonymousTypes2_variation() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
class C
{
    static void Mumble&lt;T&gt;(T anonymousType, Action&lt;T, int, int&gt; lambda) { }
    static void Mumble() { } // added to the test

    static void M()
    {
        Mumble(new { x = 42 }, {|Expand:(a, y) => a.x|});
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
class C
{
    static void Mumble&lt;T&gt;(T anonymousType, Action&lt;T, int, int&gt; lambda) { }
    static void Mumble() { } // added to the test

    static void M()
    {
        Mumble(new { x = 42 }, (a, y) => a.x);
    }
}
</code>

            Await TestAsync(input, expected, expandParameter:=True)
        End Function

#End Region

#Region "Visual Basic tests"

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1913")>
        Public Async Function TestVisualBasic_SimpleIdentifierAliasExpansion_AliasBinds() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports ShortName = LongNamespace
Namespace NS
    Class Test
        Public Function Method1() As Object
            Return (New {|Expand:ShortName|}.Class1()).Prop
        End Function
    End Class
End Namespace
Namespace LongNamespace
    Public Class Class1
        Public Readonly Property Prop As Object
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
Imports ShortName = LongNamespace
Namespace NS
    Class Test
        Public Function Method1() As Object
            Return (New Global.LongNamespace.Class1()).Prop
        End Function
    End Class
End Namespace
Namespace LongNamespace
    Public Class Class1
        Public Readonly Property Prop As Object
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Namespace
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/1913")>
        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/2805")>
        Public Async Function TestVisualBasic_SimpleIdentifierAliasExpansion_AliasDoesNotBind() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports ShortName = LongNamespace
Namespace NS
    Class Test
        Public Function Method1() As Object
            Return (New {|Expand:ShortName|}.Class1()).Prop
        End Function
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
Imports ShortName = LongNamespace
Namespace NS
    Class Test
        Public Function Method1() As Object
            Return (New LongNamespace.Class1()).Prop
        End Function
    End Class
End Namespace
</code>

            Await TestAsync(input, expected)
        End Function

#End Region

    End Class
End Namespace
