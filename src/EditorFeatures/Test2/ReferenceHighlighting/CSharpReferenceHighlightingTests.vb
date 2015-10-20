' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting
    Public Class CSharpReferenceHighlightingTests
        Inherits AbstractReferenceHighlightingTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub VerifyNoHighlightsWhenOptionDisabled()
            VerifyHighlights(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class $$Foo
                            {
                                Foo f;
                            }
                        </Document>
                    </Project>
                </Workspace>,
                optionIsEnabled:=False)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub VerifyHighlightsForClass()
            VerifyHighlights(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class {|Definition:$$Foo|}
                            {
                            }
                        </Document>
                    </Project>
                </Workspace>)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub VerifyHighlightsForScriptReference()
            VerifyHighlights(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            <ParseOptions Kind="Script"/>

                            void M()
                            {
                            }

                            {|Reference:$$Script|}.M();
                        </Document>
                    </Project>
                </Workspace>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub VerifyHighlightsForCSharpClassWithConstructor()
            VerifyHighlights(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class {|Definition:$$Foo|}
                            {
                                {|Definition:Foo|}()
                                {
                                    {|Reference:var|} x = new {|Reference:Foo|}();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>)
        End Sub

        <WorkItem(538721)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub VerifyHighlightsForCSharpClassWithSynthesizedConstructor()
            VerifyHighlights(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class {|Definition:Foo|}
                            {
                                void Blah()
                                {
                                    var x = new {|Reference:$$Foo|}();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        <WorkItem(528436)>
        Public Sub VerifyHighlightsOnCloseAngleOfGeneric()
            VerifyHighlights(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;

class {|Definition:Program|}
{
    static void Main(string[] args)
    {
        new List<{|Reference:Program$$|}>();
    }
}]]>
                        </Document>
                    </Project>
                </Workspace>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        <WorkItem(570809)>
        Public Sub VerifyNoHighlightsOnAsyncLambda()
            VerifyHighlights(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public delegate Task del();
    del ft = $$async () =>
    {
        return await Task.Yield();
    };

}]]>
                        </Document>
                    </Project>
                </Workspace>)
        End Sub

        <WorkItem(543768)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestAlias1()
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
namespace X
{
    using {|Definition:Q|} = System.IO;
    Class B
    {
        public void M()
        {
            $${|Reference:Q|}.Directory.Exists("");
        }
    }
}
</Document>
                </Project>
            </Workspace>

            VerifyHighlights(input)
        End Sub

        <WorkItem(543768)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestAlias2()
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
namespace X
{
    using $${|Definition:Q|} = System.IO;
    Class B
    {
        public void M()
        {
            {|Reference:Q|}.Directory.Exists("");
        }
    }
}
</Document>
                </Project>
            </Workspace>

            VerifyHighlights(input)
        End Sub

        <WorkItem(543768)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestAlias3()
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
namespace X
{
    using Q = System.$${|Reference:IO|};
    Class B
    {
        public void M()
        {
            {|Reference:Q|}.Directory.Exists("");
        }
    }
}
</Document>
                </Project>
            </Workspace>

            VerifyHighlights(input)
        End Sub

        <WorkItem(552000)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestAlias4()
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document><![CDATA[
using C = System.Action;

namespace N
{
    using $${|Definition:C|} = A<C>;  // select C 
    class A<T> { }
    class B : {|Reference:C|} { }
}]]>
                    </Document>
                </Project>
            </Workspace>

            VerifyHighlights(input)
        End Sub

        <WorkItem(542830)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestHighlightThroughVar1()
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    void F()
    {
        $${|Reference:var|} i = 1;
        {|Reference:int|} j = 0;
        double d;
        {|Reference:int|} k = 1;
    }
}
                    </Document>
                </Project>
            </Workspace>

            VerifyHighlights(input)
        End Sub

        <WorkItem(542830)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestHighlightThroughVar2()
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    void F()
    {
        {|Reference:var|} i = 1;
        $${|Reference:int|} j = 0;
        double d;
        {|Reference:int|} k = 1;
    }
}
                    </Document>
                </Project>
            </Workspace>

            VerifyHighlights(input)
        End Sub

        <WorkItem(542830)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestHighlightThroughVar3()
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document><![CDATA[
using System.Collections.Generic;

class C
{
    void F()
    {
        $${|Reference:var|} i = new {|Reference:List|}<string>();
        int j = 0;
        double d;
        {|Reference:var|} k = new {|Reference:List|}<int>();
    }
}
                    ]]></Document>
                </Project>
            </Workspace>

            VerifyHighlights(input)
        End Sub

        <WorkItem(545648)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestUsingAliasAndTypeWithSameName1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using {|Definition:$$X|} = System;

class X { }
        </Document>
    </Project>
</Workspace>
            VerifyHighlights(input)
        End Sub

        <WorkItem(545648)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestUsingAliasAndTypeWithSameName2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using X = System;

class {|Definition:$$X|} { }
        </Document>
    </Project>
</Workspace>
            VerifyHighlights(input)
        End Sub

        <WorkItem(567959)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestAccessor1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    string P
    {
        $$get
        {
            return P;
        }
        set
        {
            P = "";
        }
    }
}
        </Document>
    </Project>
</Workspace>

            VerifyHighlights(input)
        End Sub

        <WorkItem(567959)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestAccessor2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    string P
    {
        get
        {
            return P;
        }
        $$set
        {
            P = "";
        }
    }
}
        </Document>
    </Project>
</Workspace>

            VerifyHighlights(input)
        End Sub

        <WorkItem(604466)>
        <WpfFact(Skip:="604466"), Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub ThisShouldNotHighlightTypeName()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        t$$his.M();
    }
}
        </Document>
    </Project>
</Workspace>

            VerifyHighlights(input)
        End Sub

        <WorkItem(531620)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestHighlightDynamicallyBoundMethod()
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    class B
    {
        public void {|Definition:Boo|}(int d) { } //Line 1
        public void Boo(dynamic d) { } //Line 2
        public void Boo(string d) { } //Line 3
    }
    void Aoo()
    {
        B b = new B();
        dynamic d = 1.5f; 
        b.{|Reference:Boo|}(1); //Line 4
        b.$${|Reference:Boo|}(d); //Line 5
        b.Boo("d"); //Line 6
    }
}
                    </Document>
                </Project>
            </Workspace>

            VerifyHighlights(input)
        End Sub

        <WorkItem(531624)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestHighlightParameterizedPropertyParameter()
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    int this[int $${|Definition:i|}]
    {
        get
        {
            return this[{|Reference:i|}];
        }
    }
}
                    </Document>
                </Project>
            </Workspace>

            VerifyHighlights(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestInterpolatedString1()
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    void M()
    {
        var $${|Definition:a|} = "Hello";
        var b = "World";
        var c = $"{ {|Reference:a|} }, {b}!";
    }
}
                    </Document>
                </Project>
            </Workspace>

            VerifyHighlights(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestInterpolatedString2()
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    void M()
    {
        var a = "Hello";
        var $${|Definition:b|} = "World";
        var c = $"{a}, { {|Reference:b|} }!";
    }
}
                    </Document>
                </Project>
            </Workspace>

            VerifyHighlights(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestWrittenReference()
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    void M()
    {
        var $${|Definition:b|} = "Hello";
        {|WrittenReference:b|} = "World";
    }
}
                    </Document>
                </Project>
            </Workspace>

            VerifyHighlights(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestWrittenReference2()
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    void M()
    {
        int {|Definition:$$y|};
        int x = {|WrittenReference:y|} = 7;
    }
}
                    </Document>
                </Project>
            </Workspace>

            VerifyHighlights(input)
        End Sub

    End Class
End Namespace
