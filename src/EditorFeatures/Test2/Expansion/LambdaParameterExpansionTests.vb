' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Expansion
    Public Class LambdaParameterExpansionTests
        Inherits AbstractExpansionTest

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestCSharp_ExpandLambdaWithNoParameters() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
class C
{
    void M()
    {
        Action a = {|Expand:() => { }|};
    }
}
        ]]></Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
class C
{
    void M()
    {
        Action a = () => { };
    }
}
</code>

            Await TestAsync(input, expected, expandParameter:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestCSharp_ExpandLambdaWithSingleParameter_NoParens() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
class C
{
    void M()
    {
        Action<int> a = {|Expand:x => { }|};
    }
}
        ]]></Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
class C
{
    void M()
    {
        Action<int> a = (System.Int32 x) => { };
    }
}
]]></code>

            Await TestAsync(input, expected, expandParameter:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestCSharp_ExpandLambdaWithSingleParameter_WithParens() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
class C
{
    void M()
    {
        Action<int> a = {|Expand:(x) => { }|};
    }
}
        ]]></Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
class C
{
    void M()
    {
        Action<int> a = (System.Int32 x) => { };
    }
}
]]></code>

            Await TestAsync(input, expected, expandParameter:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestCSharp_ExpandLambdaWithTwoParameters() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
class C
{
    void M()
    {
        Action<int, int> a = {|Expand:(x, y) => { }|};
    }
}
        ]]></Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
class C
{
    void M()
    {
        Action<int, int> a = (System.Int32 x, System.Int32 y) => { };
    }
}
]]></code>

            Await TestAsync(input, expected, expandParameter:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestCSharp_ExpandLambdaWithThreeParameters() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
class C
{
    void M()
    {
        Action<int, int, int> a = {|Expand:(x, y, z) => { }|};
    }
}
        ]]></Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
class C
{
    void M()
    {
        Action<int, int, int> a = (System.Int32 x, System.Int32 y, System.Int32 z) => { };
    }
}
]]></code>

            Await TestAsync(input, expected, expandParameter:=True)
        End Function

    End Class
End Namespace