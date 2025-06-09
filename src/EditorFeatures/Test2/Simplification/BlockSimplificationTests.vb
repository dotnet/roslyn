' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.CSharp.CodeStyle
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    <Trait(Traits.Feature, Traits.Features.Simplification)>
    Public Class SimplificationTests
        Inherits AbstractSimplificationTests

        Private Shared ReadOnly DoNotPreferBraces As New OptionsCollection(LanguageNames.VisualBasic) From
        {
            {CSharpCodeStyleOptions.PreferBraces, New CodeStyleOption2(Of PreferBracesPreference)(PreferBracesPreference.None, NotificationOption2.Silent)}
        }

        <Fact>
        Public Async Function TestCSharp_DoNotSimplifyIfBlock() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    void M()
    {
        if (true)
        {|Simplify:{
            return;
        }|}
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class Program
{
    void M()
    {
        if (true)
        {
            return;
        }
    }
}
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestCSharp_DoNotSimplifyMethodBlock() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    void M()
    {|Simplify:{
        return;
    }|}
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class Program
{
    void M()
    {
        return;
    }
}
</code>

            Await TestAsync(input, expected, DoNotPreferBraces)
        End Function

        <Fact>
        Public Async Function TestCSharp_DoNotSimplifyTryBlock() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    void M()
    {
        try
        {|Simplify:{
            return;
        }|}
        finally
        {
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class Program
{
    void M()
    {
        try
        {
            return;
        }
        finally
        {
        }
    }
}
</code>

            Await TestAsync(input, expected, DoNotPreferBraces)
        End Function

        <Fact>
        Public Async Function TestCSharp_SimplifyIfBlock() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    void M()
    {
        if (true)
        {|Simplify:{
            return;
        }|}
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class Program
{
    void M()
    {
        if (true)
            return;
    }
}
</code>

            Await TestAsync(input, expected, DoNotPreferBraces)
        End Function

        <Fact>
        Public Async Function TestCSharp_SimplifyElseBlock() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    void M()
    {
        if (true)
        {
        }
        else
        {|Simplify:{
            return;
        }|}
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class Program
{
    void M()
    {
        if (true)
        {
        }
        else
            return;
    }
}
</code>

            Await TestAsync(input, expected, DoNotPreferBraces)
        End Function

        <Fact>
        Public Async Function TestCSharp_SimplifyWhileBlock() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    void M()
    {
        while (true)
        {|Simplify:{
            return;
        }|}
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class Program
{
    void M()
    {
        while (true)
            return;
    }
}
</code>

            Await TestAsync(input, expected, DoNotPreferBraces)
        End Function

        <Fact>
        Public Async Function TestCSharp_SimplifyDoBlock() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    void M()
    {
        do
        {|Simplify:{
            return;
        }|}
        while (true);
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class Program
{
    void M()
    {
        do
            return;
        while (true);
    }
}
</code>

            Await TestAsync(input, expected, DoNotPreferBraces)
        End Function

        <Fact>
        Public Async Function TestCSharp_SimplifyUsingBlock() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    void M()
    {
        using (x)
        {|Simplify:{
            return;
        }|}
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class Program
{
    void M()
    {
        using (x)
            return;
    }
}
</code>

            Await TestAsync(input, expected, DoNotPreferBraces)
        End Function

        <Fact>
        Public Async Function TestCSharp_SimplifyLockBlock() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    void M()
    {
        lock (x)
        {|Simplify:{
            return;
        }|}
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class Program
{
    void M()
    {
        lock (x)
            return;
    }
}
</code>

            Await TestAsync(input, expected, DoNotPreferBraces)
        End Function

        <Fact>
        Public Async Function TestCSharp_SimplifyForBlock() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    void M()
    {
        for (;;)
        {|Simplify:{
            return;
        }|}
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class Program
{
    void M()
    {
        for (;;)
            return;
    }
}
</code>

            Await TestAsync(input, expected, DoNotPreferBraces)
        End Function

        <Fact>
        Public Async Function TestCSharp_SimplifyForeachBlock() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    void M()
    {
        foreach (var x in y)
        {|Simplify:{
            return;
        }|}
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class Program
{
    void M()
    {
        foreach (var x in y)
            return;
    }
}
</code>

            Await TestAsync(input, expected, DoNotPreferBraces)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67867")>
        Public Async Function TestCSharp_SimplifyBaseConstructorCall() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;

public partial class A
{
    public A(Id id, IEnumerable<D> deps) { }
}
{|Simplify:
public partial class B : A
{
    public B() : base(default(Id), default(IEnumerable<D>)) { }
}
|}
public partial class D { }
public partial class Id { }
public partial class V { }
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
    <![CDATA[
using System.Collections.Generic;

public partial class A
{
    public A(Id id, IEnumerable<D> deps) { }
}

public partial class B : A
{
    public B() : base(default, default) { }
}

public partial class D { }
public partial class Id { }
public partial class V { }
]]>
</code>

            Await TestAsync(input, expected, DoNotPreferBraces)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67867")>
        Public Async Function TestCSharp_DoNotSimplifyBothArgumentsInAmbiguousBaseConstructorCall() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;

public partial class A
{
    public A(Id id, IEnumerable<D> deps) { }
    public A(string s, V v) { }
}
{|Simplify:
public partial class B : A
{
    public B() : base(default(Id), default(IEnumerable<D>)) { }
}
|}
public partial class D { }
public partial class Id { }
public partial class V { }
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
    <![CDATA[
using System.Collections.Generic;

public partial class A
{
    public A(Id id, IEnumerable<D> deps) { }
    public A(string s, V v) { }
}

public partial class B : A
{
    public B() : base(default, default(IEnumerable<D>)) { }
}

public partial class D { }
public partial class Id { }
public partial class V { }
]]>
</code>

            Await TestAsync(input, expected, DoNotPreferBraces)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67867")>
        Public Async Function TestCSharp_DoNotSimplifyBothArgumentsInAmbiguousCall() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;

public partial class A
{
    public void Goo(Id id, IEnumerable<D> deps) { }
    public void Goo(string s, V v) { }
}
{|Simplify:
public partial class B : A
{
    public B()
    {
        Goo((Id)default, (IEnumerable<D>)default);
    }
}
|}
public partial class D { }
public partial class Id { }
public partial class V { }
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
    <![CDATA[
using System.Collections.Generic;

public partial class A
{
    public void Goo(Id id, IEnumerable<D> deps) { }
    public void Goo(string s, V v) { }
}

public partial class B : A
{
    public B()
    {
        Goo(default, (IEnumerable<D>)default);
    }
}

public partial class D { }
public partial class Id { }
public partial class V { }
]]>
</code>

            Await TestAsync(input, expected, DoNotPreferBraces)
        End Function
    End Class
End Namespace
