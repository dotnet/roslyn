' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_ExtendedPropertyPattern_FirstPart_Get(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    C CProperty { {|Definition:$$get|}; set; }
    int IntProperty { get; set; }
    void M()
    {
        _ = this is { [|CProperty|].IntProperty: 2 };
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_ExtendedPropertyPattern_FirstPart_Set(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    C CProperty { get; {|Definition:$$set|}; }
    int IntProperty { get; set; }
    void M()
    {
        _ = this is { CProperty.IntProperty: 2 };
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_ExtendedPropertyPattern_SecondPart_Get(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    C CProperty { get; set; }
    int IntProperty { {|Definition:$$get|}; set; }
    void M()
    {
        _ = this is { CProperty.[|IntProperty|]: 2 };
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Get_Feature1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int Prop { {|Definition:$$get|}; set; }
}

class C : IC
{
    public virtual int Prop { {|Definition:get|}; set; }
}

class D : C
{
    public override int Prop { {|Definition:get|} => base.[|Prop|]; set => base.Prop = value; }
}

class Usages
{
    void M()
    {
        IC ic;
        var n1 = nameof(ic.Prop);
        var v1 = ic.[|Prop|];
        ic.Prop = 1;
        ic.[|Prop|]++;

        C c;
        var n2 = nameof(c.Prop);
        var v2 = c.[|Prop|];
        c.Prop = 1;
        c.[|Prop|]++;

        D d;
        var n3 = nameof(d.Prop);
        var v3 = d.[|Prop|];
        d.Prop = 1;
        d.[|Prop|]++;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Get_Feature2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int Prop { {|Definition:get|}; set; }
}

class C : IC
{
    public virtual int Prop { {|Definition:$$get|}; set; }
}

class D : C
{
    public override int Prop { {|Definition:get|} => base.[|Prop|]; set => base.Prop = value; }
}

class Usages
{
    void M()
    {
        IC ic;
        var n1 = nameof(ic.Prop);
        var v1 = ic.[|Prop|];
        ic.Prop = 1;
        ic.[|Prop|]++;

        C c;
        var n2 = nameof(c.Prop);
        var v2 = c.[|Prop|];
        c.Prop = 1;
        c.[|Prop|]++;

        D d;
        var n3 = nameof(d.Prop);
        var v3 = d.[|Prop|];
        d.Prop = 1;
        d.[|Prop|]++;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Get_Feature3(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int Prop { {|Definition:get|}; set; }
}

class C : IC
{
    public virtual int Prop { {|Definition:get|}; set; }
}

class D : C
{
    public override int Prop { {|Definition:$$get|} => base.[|Prop|]; set => base.Prop = value; }
}

class Usages
{
    void M()
    {
        IC ic;
        var n1 = nameof(ic.Prop);
        var v1 = ic.[|Prop|];
        ic.Prop = 1;
        ic.[|Prop|]++;

        C c;
        var n2 = nameof(c.Prop);
        var v2 = c.[|Prop|];
        c.Prop = 1;
        c.[|Prop|]++;

        D d;
        var n3 = nameof(d.Prop);
        var v3 = d.[|Prop|];
        d.Prop = 1;
        d.[|Prop|]++;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Set_Feature1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int Prop { get; {|Definition:$$set|}; }
}

class C : IC
{
    public virtual int Prop { get; {|Definition:set|}; }
}

class D : C
{
    public override int Prop { get => base.Prop; {|Definition:set|} => base.[|Prop|] = value; }
}

class Usages
{
    void M()
    {
        IC ic;
        var n1 = nameof(ic.Prop);
        var v1 = ic.Prop;
        ic.[|Prop|] = 1;
        ic.[|Prop|]++;

        C c;
        var n2 = nameof(c.Prop);
        var v2 = c.Prop;
        c.[|Prop|] = 1;
        c.[|Prop|]++;

        D d;
        var n3 = nameof(d.Prop);
        var v3 = d.Prop;
        d.[|Prop|] = 1;
        d.[|Prop|]++;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Set_Feature2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int Prop { get; {|Definition:set|}; }
}

class C : IC
{
    public virtual int Prop { get; {|Definition:$$set|}; }
}

class D : C
{
    public override int Prop { get => base.Prop; {|Definition:set|} => base.[|Prop|] = value; }
}

class Usages
{
    void M()
    {
        IC ic;
        var n1 = nameof(ic.Prop);
        var v1 = ic.Prop;
        ic.[|Prop|] = 1;
        ic.[|Prop|]++;

        C c;
        var n2 = nameof(c.Prop);
        var v2 = c.Prop;
        c.[|Prop|] = 1;
        c.[|Prop|]++;

        D d;
        var n3 = nameof(d.Prop);
        var v3 = d.Prop;
        d.[|Prop|] = 1;
        d.[|Prop|]++;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Set_Feature3(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int Prop { get; {|Definition:set|}; }
}

class C : IC
{
    public virtual int Prop { get; {|Definition:set|}; }
}

class D : C
{
    public override int Prop { get => base.Prop; {|Definition:$$set|} => base.[|Prop|] = value; }
}

class Usages
{
    void M()
    {
        IC ic;
        var n1 = nameof(ic.Prop);
        var v1 = ic.Prop;
        ic.[|Prop|] = 1;
        ic.[|Prop|]++;

        C c;
        var n2 = nameof(c.Prop);
        var v2 = c.Prop;
        c.[|Prop|] = 1;
        c.[|Prop|]++;

        D d;
        var n3 = nameof(d.Prop);
        var v3 = d.Prop;
        d.[|Prop|] = 1;
        d.[|Prop|]++;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Init_Feature1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int Prop { get; {|Definition:$$init|}; }
}

class C : IC
{
    public virtual int Prop { get; {|Definition:init|}; }
}

class D : C
{
    public override int Prop { get => base.Prop; {|Definition:init|} => base.[|Prop|] = value; }

    D()
    {
        this.[|Prop|] = 1;
        this.[|Prop|]++;
    }

    void M()
    {
        _ = new D() { [|Prop|] = 1 };
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Init_FromProp_Feature1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int {|Definition:$$Prop|} { get; set; }
}

class C : IC
{
    public virtual int {|Definition:Prop|} { get; set; }
}

class D : C
{
    public override int {|Definition:Prop|} { get => base.[|Prop|]; set => base.[|Prop|] = value; }
}

class Usages
{
    void M()
    {
        IC ic;
        var n1 = nameof(ic.[|Prop|]);
        var v1 = ic.[|Prop|];
        ic.[|Prop|] = 1;
        ic.[|Prop|]++;

        C c;
        var n2 = nameof(c.[|Prop|]);
        var v2 = c.[|Prop|];
        c.[|Prop|] = 1;
        c.[|Prop|]++;

        D d;
        var n3 = nameof(d.[|Prop|]);
        var v3 = d.[|Prop|];
        d.[|Prop|] = 1;
        d.[|Prop|]++;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_FromProp_Feature2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int {|Definition:Prop|} { get; set; }
}

class C : IC
{
    public virtual int {|Definition:$$Prop|} { get; set; }
}

class D : C
{
    public override int {|Definition:Prop|} { get => base.[|Prop|]; set => base.[|Prop|] = value; }
}

class Usages
{
    void M()
    {
        IC ic;
        var n1 = nameof(ic.[|Prop|]);
        var v1 = ic.[|Prop|];
        ic.[|Prop|] = 1;
        ic.[|Prop|]++;

        C c;
        var n2 = nameof(c.[|Prop|]);
        var v2 = c.[|Prop|];
        c.[|Prop|] = 1;
        c.[|Prop|]++;

        D d;
        var n3 = nameof(d.[|Prop|]);
        var v3 = d.[|Prop|];
        d.[|Prop|] = 1;
        d.[|Prop|]++;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_FromProp_Feature3(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int {|Definition:Prop|} { get; set; }
}

class C : IC
{
    public virtual int {|Definition:Prop|} { get; set; }
}

class D : C
{
    public override int {|Definition:$$Prop|} { get => base.[|Prop|]; set => base.[|Prop|] = value; }
}

class Usages
{
    void M()
    {
        IC ic;
        var n1 = nameof(ic.[|Prop|]);
        var v1 = ic.[|Prop|];
        ic.[|Prop|] = 1;
        ic.[|Prop|]++;

        C c;
        var n2 = nameof(c.[|Prop|]);
        var v2 = c.[|Prop|];
        c.[|Prop|] = 1;
        c.[|Prop|]++;

        D d;
        var n3 = nameof(d.[|Prop|]);
        var v3 = d.[|Prop|];
        d.[|Prop|] = 1;
        d.[|Prop|]++;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_FromNameOf1_Api(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int {|Definition:Prop|} { get; set; }
}

class C : IC
{
    public virtual int {|Definition:Prop|} { get; set; }
}

class D : C
{
    public override int {|Definition:Prop|} { get => base.[|Prop|]; set => base.[|Prop|] = value; }
}

class Usages
{
    void M()
    {
        IC ic;
        var n1 = nameof(ic.[|$$Prop|]);
        var v1 = ic.[|Prop|];
        ic.[|Prop|] = 1;
        ic.[|Prop|]++;

        C c;
        var n2 = nameof(c.[|Prop|]);
        var v2 = c.[|Prop|];
        c.[|Prop|] = 1;
        c.[|Prop|]++;

        D d;
        var n3 = nameof(d.[|Prop|]);
        var v3 = d.[|Prop|];
        d.[|Prop|] = 1;
        d.[|Prop|]++;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_FromNameOf1_Feature(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int {|Definition:Prop|} { get; set; }
}

class C : IC
{
    public virtual int {|Definition:Prop|} { get; set; }
}

class D : C
{
    public override int {|Definition:Prop|} { get => base.[|Prop|]; set => base.[|Prop|] = value; }
}

class Usages
{
    void M()
    {
        IC ic;
        var n1 = nameof(ic.[|$$Prop|]);
        var v1 = ic.[|Prop|];
        ic.[|Prop|] = 1;
        ic.[|Prop|]++;

        C c;
        var n2 = nameof(c.[|Prop|]);
        var v2 = c.[|Prop|];
        c.[|Prop|] = 1;
        c.[|Prop|]++;

        D d;
        var n3 = nameof(d.[|Prop|]);
        var v3 = d.[|Prop|];
        d.[|Prop|] = 1;
        d.[|Prop|]++;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Get_ObjectInitializer1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    public virtual int Prop { {|Definition:$$get|}; set; }
}

class Usages
{
    void M()
    {
        new C
        {
            Prop = 1
        };
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Set_ObjectInitializer1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    public virtual int Prop { get; {|Definition:$$set|}; }
}

class Usages
{
    void M()
    {
        new C
        {
            [|Prop|] = 1
        };
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Prop_ObjectInitializer1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    public virtual int {|Definition:$$Prop|} { get; set; }
}

class Usages
{
    void M()
    {
        new C
        {
            [|Prop|] = 1
        };
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Get_Indexer1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int this[int i] { {|Definition:$$get|} => 0; set { } }
}

class C : IC
{
    public virtual int this[int i] { {|Definition:get|} => 0; set { } }
}

class D : C
{
    public override int this[int i] { {|Definition:get|} => base[||][i]; set { base[i] = value; } }
}

class Usages
{
    void M()
    {
        IC ic;
        var v1 = ic[||][0];
        ic[0] = 1;

        C c;
        var v1 = c[||][0];
        c[0] = 1;

        D d;
        var v1 = d[||][0];
        d[0] = 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Get_Indexer2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int this[int i] { {|Definition:get|} => 0; set { } }
}

class C : IC
{
    public virtual int this[int i] { {|Definition:$$get|} => 0; set { } }
}

class D : C
{
    public override int this[int i] { {|Definition:get|} => base[||][i]; set { base[i] = value; } }
}

class Usages
{
    void M()
    {
        IC ic;
        var v1 = ic[||][0];
        ic[0] = 1;

        C c;
        var v1 = c[||][0];
        c[0] = 1;

        D d;
        var v1 = d[||][0];
        d[0] = 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Get_Indexer3(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int this[int i] { {|Definition:get|} => 0; set { } }
}

class C : IC
{
    public virtual int this[int i] { {|Definition:get|} => 0; set { } }
}

class D : C
{
    public override int this[int i] { {|Definition:$$get|} => base[||][i]; set { base[i] = value; } }
}

class Usages
{
    void M()
    {
        IC ic;
        var v1 = ic[||][0];
        ic[0] = 1;

        C c;
        var v1 = c[||][0];
        c[0] = 1;

        D d;
        var v1 = d[||][0];
        d[0] = 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Set_Indexer1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int this[int i] { get => 0; {|Definition:$$set|} { } }
}

class C : IC
{
    public virtual int this[int i] { get => 0; {|Definition:set|} { } }
}

class D : C
{
    public override int this[int i] { get => base[i]; {|Definition:set|} { base[||][i] = value; } }
}

class Usages
{
    void M()
    {
        IC ic;
        var v1 = ic[0];
        ic[||][0] = 1;

        C c;
        var v1 = c[0];
        c[||][0] = 1;

        D d;
        var v1 = d[0];
        d[||][0] = 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Set_Indexer2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int this[int i] { get => 0; {|Definition:set|} { } }
}

class C : IC
{
    public virtual int this[int i] { get => 0; {|Definition:$$set|} { } }
}

class D : C
{
    public override int this[int i] { get => base[i]; {|Definition:set|} { base[||][i] = value; } }
}

class Usages
{
    void M()
    {
        IC ic;
        var v1 = ic[0];
        ic[||][0] = 1;

        C c;
        var v1 = c[0];
        c[||][0] = 1;

        D d;
        var v1 = d[0];
        d[||][0] = 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Set_Indexer3(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    int this[int i] { get => 0; {|Definition:set|} { } }
}

class C : IC
{
    public virtual int this[int i] { get => 0; {|Definition:set|} { } }
}

class D : C
{
    public override int this[int i] { get => base[i]; {|Definition:$$set|} { base[||][i] = value; } }
}

class Usages
{
    void M()
    {
        IC ic;
        var v1 = ic[0];
        ic[||][0] = 1;

        C c;
        var v1 = c[0];
        c[||][0] = 1;

        D d;
        var v1 = d[0];
        d[||][0] = 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Get_Attribute1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class CAttribute : Attribute
{
    public virtual int Prop { {|Definition:$$get|} => 0; set { } }
}

[C(Prop = 1)]
class D
{
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Set_Attribute1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class CAttribute : Attribute
{
    public virtual int Prop { get => 0; {|Definition:$$set|} { } }
}

[C([|Prop|] = 1)]
class D
{
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Get_Cref(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    /// &lt;see cref="Prop"/&gt;
    int Prop { {|Definition:$$get|}; set; }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Set_Cref(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IC
{
    /// &lt;see cref="Prop"/&gt;
    int Prop { get; {|Definition:$$set|}; }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Get_ExpressionTree1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
using System.Linq.Expressions;

interface I
{
    int Prop { {|Definition:$$get|}; set; }
}

class C
{
    void M()
    {
        Expression&lt;Func&lt;I,int&gt;&gt; e = i => i.[|Prop|];
        Expression&lt;Action&lt;I&gt;&gt; e = i => i.Prop = 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpAccessor_Set_ExpressionTree1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
using System.Linq.Expressions;

interface I
{
    int Prop { get; {|Definition:$$set|};}
}

class C
{
    void M()
    {
        Expression&lt;Func&lt;I,int&gt;&gt; e = i => i.Prop;
        Expression&lt;Action&lt;I&gt;&gt; e = i => i.[|Prop|] = 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_Get_Feature1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
interface IC
    property {|Definition:Prop|} as integer
end interface

class C
    implements IC

    public overridable property Prop as integer implements IC.[|Prop|]
        {|Definition:$$get|}
        end get
        set(value as integer)
        end set
    end property
end class

class D
    inherits C

    public overrides property prop as integer
        {|Definition:get|}
            return mybase.[|prop|]
        end get
        set(value as integer)
            mybase.prop = value
        end set
    end property
end class

class Usages
    sub M()
        dim ic1 as IC
        dim n1 = nameof(ic1.Prop)
        dim v1 = ic1.[|Prop|]
        ic1.Prop = 1
        ic1.[|Prop|] += 1

        dim c1 as C
        dim n2 = nameof(c1.Prop)
        dim v2 = c1.[|Prop|]
        c1.Prop = 1
        c1.[|Prop|] += 1

        dim d1 as D
        dim n3 = nameof(d1.Prop)
        dim v3 = d1.[|Prop|]
        d1.Prop = 1
        d1.[|Prop|] += 1
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_Get_Feature2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
interface IC
    property {|Definition:Prop|} as integer
end interface

class C
    implements IC

    public overridable property Prop as integer implements IC.[|Prop|]
        {|Definition:get|}
        end get
        set(value as integer)
        end set
    end property
end class

class D
    inherits C

    public overrides property prop as integer
        {|Definition:$$get|}
            return mybase.[|prop|]
        end get
        set(value as integer)
            mybase.prop = value
        end set
    end property
end class

class Usages
    sub M()
        dim ic1 as IC
        dim n1 = nameof(ic1.Prop)
        dim v1 = ic1.[|Prop|]
        ic1.Prop = 1
        ic1.[|Prop|] += 1

        dim c1 as C
        dim n2 = nameof(c1.Prop)
        dim v2 = c1.[|Prop|]
        c1.Prop = 1
        c1.[|Prop|] += 1

        dim d1 as D
        dim n3 = nameof(d1.Prop)
        dim v3 = d1.[|Prop|]
        d1.Prop = 1
        d1.[|Prop|] += 1
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_Set_Feature1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
interface IC
    property {|Definition:Prop|} as integer
end interface

class C
    implements IC

    public overridable property Prop as integer implements IC.Prop
        get
        end get
        {|Definition:$$set|}(value as integer)
        end set
    end property
end class

class D
    inherits C

    public overrides property prop as integer
        get
            return mybase.prop
        end get
        {|Definition:set|}(value as integer)
            mybase.[|prop|] = value
        end set
    end property
end class

class Usages
    sub M()
        dim ic1 as IC
        dim n1 = nameof(ic1.Prop)
        dim v1 = ic1.Prop
        ic1.[|Prop|] = 1
        ic1.[|Prop|] += 1

        dim c1 as C
        dim n2 = nameof(c1.Prop)
        dim v2 = c1.Prop
        c1.[|Prop|] = 1
        c1.[|Prop|] += 1

        dim d1 as D
        dim n3 = nameof(d1.Prop)
        dim v3 = d1.Prop
        d1.[|Prop|] = 1
        d1.[|Prop|] += 1
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_Set_Feature2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
interface IC
    property {|Definition:Prop|} as integer
end interface

class C
    implements IC

    public overridable property Prop as integer implements IC.Prop
        get
        end get
        {|Definition:set|}(value as integer)
        end set
    end property
end class

class D
    inherits C

    public overrides property prop as integer
        get
            return mybase.prop
        end get
        {|Definition:$$set|}(value as integer)
            mybase.[|prop|] = value
        end set
    end property
end class

class Usages
    sub M()
        dim ic1 as IC
        dim n1 = nameof(ic1.Prop)
        dim v1 = ic1.Prop
        ic1.[|Prop|] = 1
        ic1.[|Prop|] += 1

        dim c1 as C
        dim n2 = nameof(c1.Prop)
        dim v2 = c1.Prop
        c1.[|Prop|] = 1
        c1.[|Prop|] += 1

        dim d1 as D
        dim n3 = nameof(d1.Prop)
        dim v3 = d1.Prop
        d1.[|Prop|] = 1
        d1.[|Prop|] += 1
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_FromProp_1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
interface IC
    property {|Definition:$$Prop|} as integer
end interface

class C
    implements IC

    public overridable property {|Definition:Prop|} as integer implements IC.[|Prop|]
        get
        end get
        set(value as integer)
        end set
    end property
end class

class D
    inherits C

    public overrides property {|Definition:prop|} as integer
        get
            return mybase.[|prop|]
        end get
        set(value as integer)
            mybase.[|prop|] = value
        end set
    end property
end class

class Usages
    sub M()
        dim ic1 as IC
        dim n1 = nameof(ic1.[|Prop|])
        dim v1 = ic1.[|Prop|]
        ic1.[|Prop|] = 1
        ic1.[|Prop|] += 1

        dim c1 as C
        dim n2 = nameof(c1.[|Prop|])
        dim v2 = c1.[|Prop|]
        c1.[|Prop|] = 1
        c1.[|Prop|] += 1

        dim d1 as D
        dim n3 = nameof(d1.[|Prop|])
        dim v3 = d1.[|Prop|]
        d1.[|Prop|] = 1
        d1.[|Prop|] += 1
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_FromProp_2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
interface IC
    property {|Definition:Prop|} as integer
end interface

class C
    implements IC

    public overridable property {|Definition:$$Prop|} as integer implements IC.[|Prop|]
        get
        end get
        set(value as integer)
        end set
    end property
end class

class D
    inherits C

    public overrides property {|Definition:prop|} as integer
        get
            return mybase.[|prop|]
        end get
        set(value as integer)
            mybase.[|prop|] = value
        end set
    end property
end class

class Usages
    sub M()
        dim ic1 as IC
        dim n1 = nameof(ic1.[|Prop|])
        dim v1 = ic1.[|Prop|]
        ic1.[|Prop|] = 1
        ic1.[|Prop|] += 1

        dim c1 as C
        dim n2 = nameof(c1.[|Prop|])
        dim v2 = c1.[|Prop|]
        c1.[|Prop|] = 1
        c1.[|Prop|] += 1

        dim d1 as D
        dim n3 = nameof(d1.[|Prop|])
        dim v3 = d1.[|Prop|]
        d1.[|Prop|] = 1
        d1.[|Prop|] += 1
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_FromProp_3(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
interface IC
    property {|Definition:Prop|} as integer
end interface

class C
    implements IC

    public overridable property {|Definition:Prop|} as integer implements IC.[|Prop|]
        get
        end get
        set(value as integer)
        end set
    end property
end class

class D
    inherits C

    public overrides property {|Definition:$$prop|} as integer
        get
            return mybase.[|prop|]
        end get
        set(value as integer)
            mybase.[|prop|] = value
        end set
    end property
end class

class Usages
    sub M()
        dim ic1 as IC
        dim n1 = nameof(ic1.[|Prop|])
        dim v1 = ic1.[|Prop|]
        ic1.[|Prop|] = 1
        ic1.[|Prop|] += 1

        dim c1 as C
        dim n2 = nameof(c1.[|Prop|])
        dim v2 = c1.[|Prop|]
        c1.[|Prop|] = 1
        c1.[|Prop|] += 1

        dim d1 as D
        dim n3 = nameof(d1.[|Prop|])
        dim v3 = d1.[|Prop|]
        d1.[|Prop|] = 1
        d1.[|Prop|] += 1
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_Get_ObjectInitializer1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class C
    public property Prop as integer
        {|Definition:$$get|}
            return 0
        end get
        set(value as integer)
        end set
end class

class Usages
    sub M()
        dim x = new C with {
            .Prop = 1
        }
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_Set_ObjectInitializer1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class C
    public property Prop as integer
        get
            return 0
        end get
        {|Definition:$$set|}(value as integer)
        end set
end class

class Usages
    sub M()
        dim x = new C with {
            .[|Prop|] = 1
        }
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_Property_ObjectInitializer1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class C
    public property {|Definition:$$Prop|} as integer
        get
            return 0
        end get
        set(value as integer)
        end set
end class

class Usages
    sub M()
        dim x = new C with {
            .[|Prop|] = 1
        }
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_Get_Indexer1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
interface IC
    default property {|Definition:Prop|}(a as integer) as integer
end interface

class C
    implements IC

    public overridable default property Prop(a as integer) as integer implements IC.[|Prop|]
        {|Definition:$$get|}
        end get
        set(value as integer)
        end set
    end property
end class

class D
    inherits C

    public overrides default property prop(a as integer) as integer
        {|Definition:get|}
            return mybase[||](a)
        end get
        set(value as integer)
            mybase(a) = value
        end set
    end property
end class

class Usages
    sub M()
        dim ic1 as IC
        dim v1 = ic1[||](0)
        ic1(0) = 1

        dim c1 as C
        dim v1 = c1[||](0)
        c1(0) = 1

        dim d1 as D
        dim v1 = d1[||](0)
        d1(0) = 1
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_Get_Indexer2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
interface IC
    default property {|Definition:Prop|}(a as integer) as integer
end interface

class C
    implements IC

    public overridable default property Prop(a as integer) as integer implements IC.[|Prop|]
        {|Definition:get|}
        end get
        set(value as integer)
        end set
    end property
end class

class D
    inherits C

    public overrides default property prop(a as integer) as integer
        {|Definition:$$get|}
            return mybase[||](a)
        end get
        set(value as integer)
            mybase(a) = value
        end set
    end property
end class

class Usages
    sub M()
        dim ic1 as IC
        dim v1 = ic1[||](0)
        ic1(0) = 1

        dim c1 as C
        dim v1 = c1[||](0)
        c1(0) = 1

        dim d1 as D
        dim v1 = d1[||](0)
        d1(0) = 1
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_Set_Indexer1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
interface IC
    default property {|Definition:Prop|}(a as integer) as integer
end interface

class C
    implements IC

    public overridable default property Prop(a as integer) as integer implements IC.Prop
        get
        end get
        {|Definition:$$set|}(value as integer)
        end set
    end property
end class

class D
    inherits C

    public overrides default property prop(a as integer) as integer
        get
            return mybase(a)
        end get
        {|Definition:set|}(value as integer)
            mybase[||](a) = value
        end set
    end property
end class

class Usages
    sub M()
        dim ic1 as IC
        dim v1 = ic1(0)
        ic1[||](0) = 1

        dim c1 as C
        dim v1 = c1(0)
        c1[||](0) = 1

        dim d1 as D
        dim v1 = d1(0)
        d1[||](0) = 1
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_Set_Indexer2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
interface IC
    default property {|Definition:Prop|}(a as integer) as integer
end interface

class C
    implements IC

    public overridable default property Prop(a as integer) as integer implements IC.Prop
        get
        end get
        {|Definition:set|}(value as integer)
        end set
    end property
end class

class D
    inherits C

    public overrides default property prop(a as integer) as integer
        get
            return mybase(a)
        end get
        {|Definition:$$set|}(value as integer)
            mybase[||](a) = value
        end set
    end property
end class

class Usages
    sub M()
        dim ic1 as IC
        dim v1 = ic1(0)
        ic1[||](0) = 1

        dim c1 as C
        dim v1 = c1(0)
        c1[||](0) = 1

        dim d1 as D
        dim v1 = d1(0)
        d1[||](0) = 1
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_Get_Attribute1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
imports System

class CAttribute 
    inherits Attribute

    public property Prop as integer
        {|Definition:$$get|}
        end get
        set(value as integer)
        end set
    end property
end class

&lt;C(Prop:=1)&gt;
class D
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAccessor_Set_Attribute1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
imports System

class CAttribute 
    inherits Attribute

    public property Prop as integer
        get
        end get
        {|Definition:$$set|}(value as integer)
        end set
    end property
end class

&lt;C([|Prop|]:=1)&gt;
class D
end class
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function
    End Class
End Namespace
