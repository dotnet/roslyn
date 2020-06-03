' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.CSharp.CodeStyle
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    Public Class SimplificationTests
        Inherits AbstractSimplificationTests

        Private Shared ReadOnly DoNotPreferBraces As Dictionary(Of OptionKey2, Object) = New Dictionary(Of OptionKey2, Object) From {{New OptionKey2(CSharpCodeStyleOptions.PreferBraces), New CodeStyleOption2(Of PreferBracesPreference)(PreferBracesPreference.None, NotificationOption2.Silent)}}

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
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
    End Class
End Namespace
