' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Expansion
    Public Class NameExpansionTests
        Inherits AbstractExpansionTest

#Region "C# Tests"

        <WorkItem(604392)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub NoExpansionForPropertyNamesOfObjectInitializers()
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

            Test(input, expected)
        End Sub

        <WorkItem(1913, "https://github.com/dotnet/roslyn/issues/1913")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub CSharp_SimpleIdentifierAliasExpansion_AliasBinds()
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

            Test(input, expected)
        End Sub

        <WorkItem(1913, "https://github.com/dotnet/roslyn/issues/1913")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub CSharp_SimpleIdentifierAliasExpansion_AliasDoesNotBind()
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

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub CSharp_GenericNameExpansion_DontExpandAnonymousTypes()
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

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub CSharp_LambdaParameter_DontExpandAnonymousTypes1()
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

            Test(input, expected, expandParameter:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub CSharp_LambdaParameter_DontExpandAnonymousTypes2()
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

            Test(input, expected, expandParameter:=True)
        End Sub

#End Region

#Region "Visual Basic tests"

        <WorkItem(1913, "https://github.com/dotnet/roslyn/issues/1913")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub VisualBasic_SimpleIdentifierAliasExpansion_AliasBinds()
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

            Test(input, expected)
        End Sub

        <WorkItem(1913, "https://github.com/dotnet/roslyn/issues/1913")>
        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/2805"), Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub VisualBasic_SimpleIdentifierAliasExpansion_AliasDoesNotBind()
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

            Test(input, expected)
        End Sub

#End Region

    End Class
End Namespace
