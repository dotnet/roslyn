' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Expansion
    <Trait(Traits.Feature, Traits.Features.Expansion)>
    Public Class LambdaParameterExpansionTests
        Inherits AbstractExpansionTest

        <Fact>
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

        <Fact>
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
        Action<int> a = (global::System.Int32 x) => { };
    }
}
]]></code>

            Await TestAsync(input, expected, expandParameter:=True)
        End Function

        <Fact>
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
        Action<int> a = (global::System.Int32 x) => { };
    }
}
]]></code>

            Await TestAsync(input, expected, expandParameter:=True)
        End Function

        <Fact>
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
        Action<int, int> a = (global::System.Int32 x, global::System.Int32 y) => { };
    }
}
]]></code>

            Await TestAsync(input, expected, expandParameter:=True)
        End Function

        <Fact>
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
        Action<int, int, int> a = (global::System.Int32 x, global::System.Int32 y, global::System.Int32 z) => { };
    }
}
]]></code>

            Await TestAsync(input, expected, expandParameter:=True)
        End Function
    End Class
End Namespace
