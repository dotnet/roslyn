' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpAccessor_Get_Feature1() As Task
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
            Await TestStreamingFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpAccessor_Get_Feature2() As Task
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
            Await TestStreamingFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpAccessor_Get_Feature3() As Task
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
            Await TestStreamingFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpAccessor_Set_Feature1() As Task
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
            Await TestStreamingFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpAccessor_Set_Feature2() As Task
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
            Await TestStreamingFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpAccessor_Set_Feature3() As Task
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
            Await TestStreamingFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpAccessor_FromProp_Feature1() As Task
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
            Await TestStreamingFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpAccessor_FromProp_Feature2() As Task
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
            Await TestStreamingFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpAccessor_FromProp_Feature3() As Task
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
            Await TestStreamingFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpAccessor_FromNameOf_Feature1() As Task
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
            Await TestStreamingFeature(input)
        End Function
    End Class
End Namespace
