' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting
    Public Class CSharpReferenceHighlightingTests
        Inherits AbstractReferenceHighlightingTests

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestVerifyNoHighlightsWhenOptionDisabled(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class $$Goo
                            {
                                Goo f;
                            }
                        </Document>
                    </Project>
                </Workspace>,
                testHost,
                optionIsEnabled:=False)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestVerifyHighlightsForClass(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class {|Definition:$$Goo|}
                            {
                            }
                        </Document>
                    </Project>
                </Workspace>, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestVerifyHighlightsForScriptReference(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
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
                </Workspace>, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestVerifyHighlightsForCSharpClassWithConstructor(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class {|Definition:$$Goo|}
                            {
                                {|Definition:Goo|}()
                                {
                                    {|Reference:var|} x = new {|Reference:Goo|}();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, testHost)
        End Function

        <WorkItem(538721, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538721")>
        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestVerifyHighlightsForCSharpClassWithSynthesizedConstructor(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class {|Definition:Goo|}
                            {
                                void Blah()
                                {
                                    var x = new {|Reference:$$Goo|}();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        <WorkItem(528436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528436")>
        Public Async Function TestVerifyHighlightsOnCloseAngleOfGeneric(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
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
                </Workspace>, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        <WorkItem(570809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/570809")>
        Public Async Function TestVerifyNoHighlightsOnAsyncLambda(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
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
                </Workspace>, testHost)
        End Function

        <WorkItem(543768, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543768")>
        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestAlias1(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WorkItem(543768, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543768")>
        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestAlias2(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WorkItem(543768, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543768")>
        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestAlias3(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WorkItem(552000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552000")>
        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestAlias4(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WorkItem(542830, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542830")>
        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestHighlightThroughVar1(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WorkItem(542830, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542830")>
        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestHighlightThroughVar2(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WorkItem(542830, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542830")>
        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestHighlightThroughVar3(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WorkItem(545648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545648")>
        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestUsingAliasAndTypeWithSameName1(testHost As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using {|Definition:$$X|} = System;

class X { }
        </Document>
    </Project>
</Workspace>
            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WorkItem(545648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545648")>
        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestUsingAliasAndTypeWithSameName2(testHost As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using X = System;

class {|Definition:$$X|} { }
        </Document>
    </Project>
</Workspace>
            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WorkItem(567959, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/567959")>
        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestAccessor1(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WorkItem(567959, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/567959")>
        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestAccessor2(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory(Skip:="604466"), CombinatorialData>
        <WorkItem(604466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604466")>
        Public Async Function TestThisShouldNotHighlightTypeName(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WorkItem(531620, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531620")>
        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestHighlightDynamicallyBoundMethod(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WorkItem(531624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531624")>
        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestHighlightParameterizedPropertyParameter(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestInterpolatedString1(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestInterpolatedString2(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestWrittenReference(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestWrittenReference2(testHost As TestHost) As Task
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

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestPatternMatchingType1(testHost As TestHost) As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    void M()
    {
        object o = null;
        if (o is C $${|Definition:c|})
        {
            var d = {|Reference:c|};
        }
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestPatternMatchingType2(testHost As TestHost) As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
    void M()
    {
        object o = null;
        if (o is C {|Definition:c|})
        {
            var d = $${|Reference:c|};
        }
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestPatternMatchingTypeScoping1(testHost As TestHost) As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class Class1 { } 
class Class2 { } 
class C
{
    void M()
    {
        object o = null;
        if (o is Class1 {|Definition:c|})
        {
            var d = $${|Reference:c|};
        }
        else if (o is Class2 c)
        {
            var d = c;
        }
            el
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestPatternMatchingTypeScoping2(testHost As TestHost) As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class Class1 { } 
class Class2 { } 
class C
{
    void M()
    {
        object o = null;
        if (o is Class1 c)
        {
            var d = c;
        }
        else if (o is Class2 {|Definition:c|})
        {
            var d = $${|Reference:c|};
        }
            el
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestRegexReference1(testHost As TestHost) As Task

            Dim input =
           <Workspace>
               <Project Language="C#" CommonReferences="true">
                   <Document>
using System.Text.RegularExpressions;

class C
{
    void Goo()
    {
        var r = new Regex(@"{|Reference:(a)|}\0{|Reference:\$$1|}");
    }
}
                    </Document>
               </Project>
           </Workspace>

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestHighlightParamAndCommentsCursorOnDefinition(testHost As TestHost) As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
        /// &lt; summary &gt;
        /// &lt; paramref name="{|Reference:x|}"/ &gt;
        /// &lt; /summary &gt;
        /// &lt; param name="{|Reference:x|}" &gt; &lt; /param &gt;
        public int this[int $${|Definition:x|}]
        {
            get
            {
                return 0;
            }
        }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestHighlightParamAndCommentsCursorOnReference(testHost As TestHost) As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
        /// &lt; summary &gt;
        /// &lt; paramref name="$${|Reference:x|}"/ &gt;
        /// &lt; /summary &gt;
        /// &lt; param name="{|Reference:x|}" &gt; &lt; /param &gt;
        public int this[int {|Definition:x|}]
        {
            get
            {
                return 0;
            }
        }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestHighlightParamAndCommentsDefinitionNestedBetweenReferences(testHost As TestHost) As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class C
{
        /// &lt; summary &gt;
        /// &lt; paramref name="$${|Reference:x|}"/ &gt;
        /// &lt; /summary &gt;
        /// &lt; param name="{|Reference:x|}" &gt; &lt; /param &gt;
        public int this[int {|Definition:x|}]
        {
            get
            {
                return {|Reference:x|};
            }
        }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyHighlightsAsync(input, testHost)
        End Function
    End Class
End Namespace
