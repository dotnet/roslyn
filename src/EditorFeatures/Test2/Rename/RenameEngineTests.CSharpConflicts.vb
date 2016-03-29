' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    Partial Public Class RenameEngineTests
        Public Class CSharpConflicts
            Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

            Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
                _outputHelper = outputHelper
            End Sub

            <WpfFact>
            <WorkItem(773543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773543")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub BreakingRenameWithRollBacksInsideLambdas_2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
using System;

class C
{
    class D { public int x = 1; }
    Action&lt;int> a = (int [|$$x|]) => // Rename x to y
                    {
                        var {|Conflict:y|} = new D();
                        Console.{|Conflict:WriteLine|}({|Conflict:x|});
                    };
 
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="y")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Fact>
            <WorkItem(773534, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773534")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub BreakingRenameWithRollBacksInsideLambdas_1()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
using System;

struct y
{
    public int x;
}
class C
{
    class D { public int x = 1; }
    Action&lt;y> a = (y [|$$x|]) => // Rename x to y
                    {   var {|Conflict:y|} = new D();
                        Console.WriteLine(y.x);
                        Console.WriteLine({|Conflict:x|}.{|Conflict:x|});
                    };
 
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="y")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Fact>
            <WorkItem(773435, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773435")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub BreakingRenameWithInvocationOnDelegateInstance()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    public delegate void Foo(int x);
    public void FooMeth(int x)
    {
 
    }
    public void Sub()
    {
        Foo {|Conflict:x|} = new Foo(FooMeth);
        int [|$$z|] = 1; // Rename z to x
        int y = {|Conflict:z|};
        x({|Conflict:z|}); // Renamed to x(x)
    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="x")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Fact>
            <WorkItem(782020, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/782020")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub BreakingRenameWithSameClassInOneNamespace()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
using K = N.{|Conflict:C|}; // No change, show compiler error
namespace N
{
    class {|Conflict:C|}
    {
    }
}
namespace N
{
    class {|Conflict:$$D|} // Rename D to C
    {
    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="C")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub BreakingRenameCrossAssembly()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBAssembly1">
                            <ProjectReference>CSAssembly1</ProjectReference>
                            <Document>
Class D
    Public Sub Boo()
        Dim x = New {|Conflict:$$C|}()
    End Sub
End Class
                            </Document>
                        </Project>
                        <Project Language="C#" CommonReferences="true" AssemblyName="CSAssembly1">
                            <Document>
public class [|C|]
{
    public static void Foo()
    {

    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="D")

                    result.AssertLabeledSpansAre("Conflict", "D", RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionInsideLambdaBody()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Proaasgram
{
    object z;
    public void masdain(string[] args)
    {
        Func&lt;int, bool> sx = (int [|$$x|]) =>
        {
            {|resolve:z|} = null;
            if (true)
            {
                bool y = foo([|x|]);
            }
            return true;
        };
    }

    public  bool foo(int bar)
    {
        return true;
    }

    public bool foo(object bar)
    {
        return true;
    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="z")


                    result.AssertLabeledSpansAre("resolve", "this.z = null;", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <WorkItem(1069237, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1069237")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionInsideExpressionBodiedLambda()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
using System;
using System.Collections.Generic;
using System.Linq;

public class B
{
    public readonly int z = 0;
    public int X(int [|$$x|]) => {|direct:x|} + {|resolve:z|};
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="z")

                    result.AssertLabeledSpansAre("direct", "z + this.z", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("resolve", "z + this.z", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <WorkItem(1069237, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1069237")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionInsideExpressionBodiedLambda2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
using System;
using System.Collections.Generic;
using System.Linq;

public class B
{
    public static readonly int z = 0;
    public int X(int [|$$x|]) => {|direct:x|} + {|resolve:z|};
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="z")

                    result.AssertLabeledSpansAre("direct", "z + B.z", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("resolve", "z + B.z", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionInsideMethodBody()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
using System;
using System.Collections.Generic;
using System.Linq;

public class B
{
    public readonly int z = 0;
    public int Y(int [|$$y|]) 
    { 
        [|y|] = 0;
        return {|resolve:z|}; 
    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="z")

                    result.AssertLabeledSpansAre("resolve", "return this.z;", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionInInvocationWithLambda_1()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
using System;

static class C
{
    static void Ex(this string x) { }

    static void Outer(Action&lt;string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action&lt;int> x, int y) { Console.WriteLine(2); }

    static void Inner(Action&lt;string> x, string y) { }
    static void Inner(Action&lt;string> x, int y) { }
    static void Inner(Action&lt;int> x, int y) { }

    static void Main()
    {
        {|resolve1:Outer|}(y => {|resolve2:Inner|}(x => {
                                  var z = 5;
z.{|resolve0:Ex|}();
x.Ex();
                                  }, y), 0);
    }
}

static class E
{
    public static void [|$$Ex|](this int x) { } // Rename Ex to Foo
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="L")

                    Dim outputResult = <code>Outer((string y) => Inner(x => {</code>.Value + vbCrLf +
                                       <code>                                  var z = 5;</code>.Value + vbCrLf +
                                       <code>z.L();</code>.Value + vbCrLf +
                                       <code>x.Ex();</code>.Value + vbCrLf +
                                       <code>                                  }, y), 0);</code>.Value

                    result.AssertLabeledSpansAre("resolve0", outputResult, RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("resolve1", outputResult, RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("resolve2", outputResult, RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionInInvocationWithLambda_2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
using System;

static class C
{
    static void Ex(this string x) { }

    static void Outer(Action&lt;string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action&lt;int> x, int y) { Console.WriteLine(2); }

    static void Inner(Action&lt;string> x, string y) { }
    static void Inner(Action&lt;string> x, int y) { }
    static void Inner(Action&lt;int> x, int y) { }

    static void Main()
    {
        {|resolve1:Outer|}((y) => {|resolve2:Inner|}((x) => {
                                  var z = 5;
z.{|resolve0:Ex|}();
x.Ex();
                                  }, y), 0);
    }
}

static class E
{
    public static void [|$$Ex|](this int x) { } // Rename Ex to Foo
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="L")

                    Dim outputResult = <code>Outer((string y) => Inner((x) => {</code>.Value + vbCrLf +
                                       <code>                                  var z = 5;</code>.Value + vbCrLf +
                                       <code>z.L();</code>.Value + vbCrLf +
                                       <code>x.Ex();</code>.Value + vbCrLf +
                                       <code>                                  }, y), 0);</code>.Value

                    result.AssertLabeledSpansAre("resolve0", outputResult, RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("resolve1", outputResult, RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("resolve2", outputResult, RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionInInvocationWithLambda_3()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
using System;

static class C
{
    static void Ex(this string x) { }

    static void Outer(Action&lt;string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action&lt;int> x, int y) { Console.WriteLine(2); }

    static void Inner(Action&lt;string> x, string y) { }
    static void Inner(Action&lt;string> x, int y) { }
    static void Inner(Action&lt;int> x, int y) { }

    static void Main()
    {
        {|resolve1:Outer|}((y) => {|resolve2:Inner|}((x) => {
                                  var z = 5;
z.{|resolve0:D|}();
x.Ex();
                                  }, y), 0);
    }
}

static class E
{
    public static void [|$$D|](this int x) { } // Rename Ex to Foo
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Ex")

                    Dim outputResult = <code>Outer((y) => Inner((string x) => {</code>.Value + vbCrLf +
                                       <code>                                  var z = 5;</code>.Value + vbCrLf +
                                       <code>z.Ex();</code>.Value + vbCrLf +
                                       <code>x.Ex();</code>.Value + vbCrLf +
                                       <code>                                  }, y), 0);</code>.Value

                    result.AssertLabeledSpansAre("resolve0", outputResult, RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("resolve1", outputResult, RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("resolve2", outputResult, RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionInInvocationWithLambda_4()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
using System;

static class C
{
    static void Ex(this string x) { }

    static void Outer(Action&lt;string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action&lt;int> x, int y) { Console.WriteLine(2); }

    static void Inner(Action&lt;string> x, string y) { }
    static void Inner(Action&lt;string> x, int y) { }
    static void Inner(Action&lt;int> x, int y) { }

    static void Main()
    {
        {|resolve1:Outer|}(y => {|resolve2:Inner|}(x => {
                                  var z = 5;
z.{|resolve0:D|}();
x.Ex();
                                  }, y), 0);
    }
}

static class E
{
    public static void [|$$D|](this int x) { } // Rename Ex to Foo
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Ex")

                    Dim outputResult = <code>Outer(y => Inner((string x) => {</code>.Value + vbCrLf +
                                       <code>                                  var z = 5;</code>.Value + vbCrLf +
                                       <code>z.Ex();</code>.Value + vbCrLf +
                                       <code>x.Ex();</code>.Value + vbCrLf +
                                       <code>                                  }, y), 0);</code>.Value

                    result.AssertLabeledSpansAre("resolve0", outputResult, RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("resolve1", outputResult, RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("resolve2", outputResult, RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionInInvocationWithLambda_5()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
using System;

static class C
{
    static void Ex(this string x) { }

    static void Outer(Action&lt;string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action&lt;int> x, int y) { Console.WriteLine(2); }

    static void Inner(Action&lt;string> x, string y) { }
    static void Inner(Action&lt;string> x, int y) { }
    static void Inner(Action&lt;int> x, int y) { }

    static void Main()
    {
        {|resolve1:Outer|}(y => {|resolve2:Inner|}(x => {
                                  var z = 5;
                                  z.{|resolve0:D|}();
                                  x.Ex();
                                  }, y), 0);
    }
}

static class E
{
    public static void [|$$D|](this int x) { } // Rename Ex to Foo
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Ex")

                    Dim outputResult = <code>Outer(y => Inner((string x) => {</code>.Value + vbCrLf +
                                       <code>                                  var z = 5;</code>.Value + vbCrLf +
                                       <code>                                  z.Ex();</code>.Value + vbCrLf +
                                       <code>                                  x.Ex();</code>.Value + vbCrLf +
                                       <code>                                  }, y), 0);</code>.Value

                    result.AssertLabeledSpansAre("resolve0", outputResult, RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("resolve1", outputResult, RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("resolve2", outputResult, RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ParameterConflictingWithInstanceField1()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Foo
{
    int foo;
    void Blah(int [|$$bar|])
    {
        {|stmt2:foo|} = {|stmt1:bar|};
    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="foo")


                    result.AssertLabeledSpansAre("stmt1", "this.foo = foo;", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt2", "this.foo = foo;", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ParameterConflictingWithInstanceField2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Foo
{
    int foo;
    void Blah(int [|$$bar|])
    {
        {|resolved:foo|} = 23;
        {|resolved2:foo|} = {|stmt1:bar|};
    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="foo")


                    result.AssertLabeledSpansAre("stmt1", "this.foo = foo;", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("resolved", "this.foo = 23;", RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("resolved2", "this.foo = foo;", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ParameterConflictingWithInstanceFieldRenamingToKeyword()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Foo
{
    int @if;
    void Blah(int {|Escape1:$$bar|})
    {
        {|Resolve:@if|} = 23;
        {|Resolve2:@if|} = {|Escape2:bar|};
    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="if")

                    result.AssertLabeledSpansAre("Resolve", "this.@if = 23;", RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpecialSpansAre("Escape1", "@if", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpecialSpansAre("Escape2", "this.@if = @if;", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("Resolve2", "this.@if = @if;", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ParameterConflictingWithStaticField()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Foo
{
    static int foo;
    void Blah(int [|$$bar|])
    {
        {|Resolved:foo|} = 23;
        {|Resolved2:foo|} = {|stmt1:bar|};
    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="foo")


                    result.AssertLabeledSpansAre("Resolved", "Foo.foo = 23;", RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("stmt1", "Foo.foo = foo;", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("Resolved2", "Foo.foo = foo;", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ParameterConflictingWithFieldFromAnotherLanguage()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <ProjectReference>VisualBasicAssembly</ProjectReference>
                            <Document>
class Foo : FooBase
{
    void Blah(int bar)
    {
        {|Resolve:$$foo|} = bar;
    }
}
                           </Document>
                        </Project>
                        <Project Language="Visual Basic" AssemblyName="VisualBasicAssembly" CommonReferences="true">
                            <Document>
Public Class FooBase
    Protected [|foo|] As Integer
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="bar")


                    result.AssertLabeledSpansAre("Resolve", "base.bar = bar;", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Fact>
            <WorkItem(539745, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539745")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictingTypeDeclaration()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" LanguageVersion="CSharp6">
                            <Document><![CDATA[
namespace N1
{
    using static C<int>;

    class Program
    {
        public void Goo(int i)
        {
            {|ReplacementCInt:Foo|}(i, i);
        }
    }
}

namespace N2
{
    using static C<string>;

    class Program
    {
        public void Goo(string s)
        {
            {|ReplacementCString:Foo|}(s, s);
        }
    }
}

static class C<T>
{
    public static void [|$$Foo|](T i, T j) { }
}
                            ]]></Document>
                        </Project>
                    </Workspace>, renameTo:="Goo")


                    result.AssertLabeledSpansAre("ReplacementCInt", "C<int>.Goo(i, i);", RelatedLocationType.ResolvedReferenceConflict)
                    result.AssertLabeledSpansAre("ReplacementCString", "C<string>.Goo(s, s);", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenamingToInvalidIdentifier()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class {|Invalid:$$Foo|}
{
    {|Invalid:Foo|} foo;
}
                               </Document>
                        </Project>
                    </Workspace>, renameTo:="`")

                    result.AssertLabeledSpansAre("Invalid", "`", RelatedLocationType.UnresolvedConflict)
                    result.AssertReplacementTextInvalid()
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenamingToInvalidIdentifier2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class {|Invalid:$$Foo|}
{
    {|Invalid:Foo|} foo;
}
                               </Document>
                        </Project>
                    </Workspace>,
                   renameTo:="!")

                    result.AssertLabeledSpansAre("Invalid", "!", RelatedLocationType.UnresolvedConflict)
                    result.AssertReplacementTextInvalid()
                End Using
            End Sub

            <Fact>
            <WorkItem(539636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539636")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenamingToConflictingMethodInvocation()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    static void F()
    {
    }

    class Blah
    {
        void [|$$M|]()
        {
            {|Replacement:F|}();
        }
    }
} 
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="F")


                    result.AssertLabeledSpansAre("Replacement", "Program.F();", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenamingToConflictingMethodInvocation2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    void M()
    {
        int foo;
        {|Replacement:Bar|}();
    }

    void [|$$Bar|]()
    {
    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="foo")


                    result.AssertLabeledSpansAre("Replacement", "this.foo();", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Fact>
            <WorkItem(539733, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539733")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenamingTypeToConflictingMemberAndParentTypeName()
                ' It's important that we see conflicts for both simultaneously, so I do a single
                ' test for both cases.
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class {|Conflict:Foo|}
{
    class [|$$Bar|]
    {
        int {|Conflict:Foo|};
    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Fact>
            <WorkItem(539733, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539733")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenamingMemberToNameConflictingWithParent()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class {|Conflict:Foo|}
{
    int [|$$Bar|];
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Fact>
            <WorkItem(540199, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540199")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenamingMemberToInvalidIdentifierName()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class {|Invalid:$$Foo|}
{
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo@")

                    result.AssertReplacementTextInvalid()
                    result.AssertLabeledSpansAre("Invalid", "Foo@", RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub MinimalQualificationOfBaseType1()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class X
{
    protected class [|$$A|] { }
}

class Y : X
{
    private class C : {|Resolve:A|} { }
    private class B { }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="B")


                    result.AssertLabeledSpansAre("Resolve", "X.B", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub MinimalQualificationOfBaseType2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class X
{
    protected class A { }
}

class Y : X
{
    private class C : {|Resolve:A|} { }
    private class [|$$B|] { }
}
                               </Document>
                        </Project>
                    </Workspace>, renameTo:="A")


                    result.AssertLabeledSpansAre("Resolve", "X.A", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(542322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542322")>
            Public Sub EscapeIfKeywordWhenDoingTypeNameQualification()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
static class Foo
{
    static void {|Escape:Method$$|}() { }

    static void Test()
    {
        int @if;
        {|Replacement:Method|}();
    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="if")

                    result.AssertLabeledSpecialSpansAre("Escape", "@if", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("Replacement", "Foo.@if();", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(542322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542322")>
            Public Sub EscapeUnboundGenericTypesInTypeOfContext()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[

using C = A<int>;

class A<T>
{
    public class B<S> { }
}

class Program
{
    static void Main()
    {
        var type = typeof({|stmt1:C|}.B<>);
    }

    class [|D$$|] { }
}

                               ]]></Document>
                        </Project>
                    </Workspace>, renameTo:="C")


                    result.AssertLabeledSpansAre("stmt1", "var type = typeof(A<>.B<>);", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(542322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542322")>
            Public Sub EscapeUnboundGenericTypesInTypeOfContext2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using C = A<int>;

class A<T>
{
    public class B<S>
    {
        public class E { }
    }
}

class Program
{
    static void Main()
    {
        var type = typeof({|Replacement:C|}.B<>.E);
    }

    class [|D$$|] { }
}
                               ]]></Document>
                        </Project>
                    </Workspace>, renameTo:="C")


                    result.AssertLabeledSpansAre("Replacement", "var type = typeof(A<>.B<>.E);", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(542322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542322")>
            Public Sub EscapeUnboundGenericTypesInTypeOfContext3()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using C = A<int>;

class A<T>
{
    public class B<S>
    {
        public class E { }
    }
}

class Program
{
    static void Main()
    {
        var type = typeof({|Replacement:C|}.B<>.E);
    }

    class [|D$$|] 
    {
        public class B<S>
        {
            public class E { }
        }
    }
}
                               ]]></Document>
                        </Project>
                    </Workspace>,
                   renameTo:="C")


                    result.AssertLabeledSpansAre("Replacement", "var type = typeof(A<>.B<>.E);", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(542651, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542651")>
            Public Sub ReplaceAliasWithGenericTypeThatIncludesArrays()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using C = A<int[]>;

class A<T> { }

class Program
{
    {|Resolve:C|} x;

    class [|D$$|] { }
}
                            ]]></Document>
                        </Project>
                    </Workspace>,
                   renameTo:="C")


                    result.AssertLabeledSpansAre("Resolve", "A<int[]>", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(542651, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542651")>
            Public Sub ReplaceAliasWithGenericTypeThatIncludesPointers()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using C = A<int*>;

class A<T> { }

class Program
{
    {|Resolve:C|} x;

    class [|D$$|] { }
}
                            ]]></Document>
                        </Project>
                    </Workspace>,
                   renameTo:="C")


                    result.AssertLabeledSpansAre("Resolve", "A<int*>", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(542651, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542651")>
            Public Sub ReplaceAliasWithNestedGenericType()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using C = A<int>.E;

class A<T>
{
    public class E { }
}

class B
{
    {|Resolve:C|} x;

    class [|D$$|] { } // Rename D to C
}
                            ]]></Document>
                        </Project>
                    </Workspace>, renameTo:="C")


                    result.AssertLabeledSpansAre("Resolve", "A<int>.E", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact()>
            <WorkItem(535068, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535068")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(542103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542103")>
            Public Sub RewriteConflictingExtensionMethodCallSite()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    C Bar(int tag)
    {
        return this.{|stmt1:Foo|}(1).{|stmt1:Foo|}(2);
    }
}


static class E
{
    public static C [|$$Foo|](this C x, int tag) { return new C(); }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Bar")


                    result.AssertLabeledSpansAre("stmt1", "return E.Bar(E.Bar(this,1),2);", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(535068, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535068")>
            <WorkItem(528902, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528902")>
            <WorkItem(645152, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/645152")>
            Public Sub RewriteConflictingExtensionMethodCallSiteWithReturnTypeChange()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    void [|$$Bar|](int tag)
    {
        this.{|Resolved:Foo|}(1).{|Resolved:Foo|}(2);
    }
}

static class E
{
    public static C Foo(this C x, int tag) { return x; }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("Resolved", "E.Foo(E.Foo(this,1),2);", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WpfFact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(535068, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535068")>
            <WorkItem(542821, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542821")>
            Public Sub RewriteConflictingExtensionMethodCallSiteRequiringTypeArguments()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
class C
{
    void [|$$Bar|]<T>()
    {
        {|Replacement:this.{|Resolved:Foo|}<int>()|};
    }
}
 
 
static class E
{
    public static void Foo<T>(this C x) { }
}
                            ]]></Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("Resolved", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("Replacement", "E.Foo<int>(this)")
                End Using
            End Sub

            <WpfFact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(535068, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535068")>
            <WorkItem(542103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542103")>
            Public Sub RewriteConflictingExtensionMethodCallSiteInferredTypeArguments()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
class C
{
    void [|$$Bar|]<T>(T y)
    {
        {|Replacement:this.{|Resolved:Foo|}(42)|};
    }
}
 
 
static class E
{
    public static void Foo<T>(this C x, T y) { }
}
                            ]]></Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("Resolved", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("Replacement", "E.Foo(this, 42)")
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub DoNotDetectQueryContinuationNamedTheSame()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
using System.Linq;
class C
{
    static void Main(string[] args)
    {
        var temp = from {|stmt1:$$x|} in "abc"
                   select {|stmt1:x|} into y
                   select y;
    }
}

                            </Document>
                        </Project>
                    </Workspace>, renameTo:="y")

                    ' This may feel strange, but the "into" effectively splits scopes
                    ' into two. There are no errors here.
                    result.AssertLabeledSpansAre("stmt1", "y", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(543027, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543027")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameHandlesUsingWithoutDeclaration()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>

using System.IO;
class Program
{
    public static void Main(string[] args)
    {
        Stream {|stmt1:$$s|} = new Stream();
        using ({|stmt2:s|})
        {
        }
    }
}
                       </Document>
                        </Project>
                    </Workspace>, renameTo:="x")

                    result.AssertLabeledSpansAre("stmt1", "x", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt2", "x", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(543027, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543027")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameHandlesForWithoutDeclaration()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>

class Program
{
    public static void Main(string[] args)
    {
        int {|stmt1:$$i|};
        for ({|stmt2:i|} = 0; ; )
        {
        }
    }
}
                       </Document>
                        </Project>
                    </Workspace>, renameTo:="x")

                    result.AssertLabeledSpansAre("stmt1", "x", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt2", "x", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameAttributeSuffix()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document FilePath="Test.cs">
using System;

[{|Special:Something|}()]
class Foo{ }

public class [|$$SomethingAttribute|] : Attribute
{
    public [|SomethingAttribute|]() { }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="SpecialAttribute")


                    result.AssertLabeledSpansAre("Special", "Special", type:=RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameAddAttributeSuffix()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document FilePath="Test.cs">
using System;

[[|Something|]()]
class Foo{ }

public class [|$$SomethingAttribute|] : Attribute
{
    public [|SomethingAttribute|]() { }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Special")


                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameKeepAttributeSuffixOnUsages()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document FilePath="Test.cs">
using System;

[[|SomethingAttribute|]()]
class Foo { }

public class [|$$SomethingAttribute|] : Attribute
{
    public [|SomethingAttribute|] { }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="FooAttribute")


                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameToConflictWithValue()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document FilePath="Test.cs">
class C
{
    public int TestProperty
    {
        set
        {
            int [|$$x|];
            [|x|] = {|Conflict:value|};
        }
    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="value")

                    ' result.AssertLabeledSpansAre("stmt1", "value", RelatedLocationType.NoConflict)
                    ' result.AssertLabeledSpansAre("stmt2", "value", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Fact>
            <WorkItem(543482, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543482")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameAttributeWithConflictingUse()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document FilePath="Test.cs">
class C
{
    [Main()]
    static void test() { }
}
class MainAttribute : System.Attribute
{
    static void Main() { }
}
class [|$$Main|] : System.Attribute
{
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="FooAttribute")


                End Using
            End Sub

            <Fact>
            <WorkItem(542649, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542649")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub QualifyTypeWithGlobalWhenConflicting()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document FilePath="Test.cs">
class A { }

class B
{
    {|Resolve:A|} x;

    class [|$$C|] { }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="A")


                    result.AssertLabeledSpansAre("Resolve", "global::A", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub
        End Class

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameSymbolConflictWithLocals()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;
class C
{
    void Foo()
    {
        { int x; }
        {|Stmt1:Bar|}();
    }
 
    void [|$$Bar|]() { }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="x")


                result.AssertLabeledSpansAre("Stmt1", "this.x();", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(528738, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528738")>
        Public Sub RenameAliasToCatchConflict()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using [|$$A|] = X.Something;
using {|Conflict:B|} = X.SomethingElse;

namespace X
{
    class Something { }
    class SomethingElse { }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="B")


                result.AssertLabeledSpansAre("Conflict", "B", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameAttributeToCreateConflict()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;
[{|Escape:Main|}]
class Some
{
}
class SpecialAttribute : Attribute
{
}
class [|$$Main|] : Attribute // Rename 'Main' to 'Special'
{
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Special")


                result.AssertLabeledSpecialSpansAre("Escape", "@Special", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameUsingToKeyword()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;
using [|$$S|] = System.Collections;
[A]
class A : {|Resolve:Attribute|}
{
}
class B
{
	[|S|].ArrayList a;
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Attribute")


                result.AssertLabeledSpansAre("Resolve", "System.Attribute", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(16809, "http://vstfdevdiv:8080/DevDiv_Projects/Roslyn/_workitems/edit/16809")>
        <WorkItem(535066, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535066")>
        Public Sub RenameInNestedClasses()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
using System;
namespace N
{
    class A<T>
    {
        public virtual void Foo(T x) { }
        class B<S> : A<B<S>>
        {
            class [|$$C|]<U> : B<{|Resolve1:C|}<U>> // Rename C to A
            {
                public override void Foo({|Resolve2:A|}<{|Resolve3:A|}<T>.B<S>>.B<{|Resolve4:A|}<T>.B<S>.{|Resolve1:C|}<U>> x) { }
            }
        }
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="A")


                result.AssertLabeledSpansAre("Resolve1", "A", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Resolve2", "N.A<N.A<T>.B<S>>", RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansAre("Resolve3", "N.A<N.A<T>.B<S>>", RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansAre("Resolve4", "N.A<T>", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact()>
        <WorkItem(535066, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535066")>
        <WorkItem(531433, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531433")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameAndEscapeContextualKeywordsInCSharp()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System.Linq;

class [|t$$o|] // Rename 'to' to 'from'
{
    object q = from x in "" select new {|resolved:to|}();
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="from")

                result.AssertLabeledSpansAre("resolved", "@from", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(522774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522774")>
        Public Sub RenameCrefWithConflict()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
using System;
using F = N;
namespace N
{
    interface I
    {
       void Foo();
    }
}
class C
{
    class E : {|Resolve:F|}.I
    {
        /// <summary>
        /// This is a function <see cref="{|Resolve:F|}.I.Foo"/>
        /// </summary>
        public void Foo() { }
    }
    class [|$$K|]
    {
    }
}
                           ]]></Document>
                    </Project>
                </Workspace>, renameTo:="F")


                result.AssertLabeledSpansAre("Resolve", "N", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameClassContainingAlias()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
using System;
using C = A<int,int>;
class A<T,U>  
{
    public class B<S> 
    {
    }   
}
class [|$$B|]
{
    {|Resolve:C|}.B<int> cb;
}
                           ]]></Document>
                    </Project>
                </Workspace>, renameTo:="C")


                result.AssertLabeledSpansAre("Resolve", "A<int, int>", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameFunctionWithOverloadConflict()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
class Bar
    {
        void Foo(int x) { }
        void [|Boo|](object x) { }
        void Some()
        {
            Foo(1);
            {|Resolve:$$Boo|}(1);
        }
    }
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("Resolve", "Foo((object)1);", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameActionWithFunctionConflict()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
using System;
class Program
{
    static void doer(int x)
    {
        Console.WriteLine("Hey");
    }
    static void Main(string[] args)
    {
        Action<int> {|stmt1:$$action|} = delegate(int x) { Console.WriteLine(x); }; // Rename action to doer
        {|stmt2:doer|}(3);
    }
}
                           ]]></Document>
                    </Project>
                </Workspace>, renameTo:="doer")

                result.AssertLabeledSpansAre("stmt1", "doer", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", "Program.doer(3);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(552522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552522")>
        Public Sub RenameFunctionNameToDelegateTypeConflict1()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;
class A 
{
    static void [|Foo|]() { }  
    class B
    {
        delegate void Del();
        void Boo()
        {
            Del d = new Del({|Stmt1:Foo|});
            {|Stmt2:$$Foo|}();
        }
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Del")


                result.AssertLabeledSpansAre("Stmt1", "Del d = new Del(A.Del);", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("Stmt2", "Del", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(552520, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552520")>
        Public Sub RenameFunctionNameToDelegateTypeConflict2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;
class A
{
    static void [|$$Foo|]() { }
    class B
    {
        delegate void Del();
        void Bar() { }
        void Boo()
        {
            Del d = new Del({|Stmt1:Foo|});
        }
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("Stmt1", "Del d = new Del(A.Bar);", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameFunctionNameToDelegateTypeConflict3()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;                            
class A
{
    delegate void Del(Del a);
    static void [|Bar|](Del a) { }
    class B
    {
        Del Boo = new Del({|decl1:Bar|});
        void Foo()
        {
            Boo({|Stmt2:Bar|});
            {|Stmt3:$$Bar|}(Boo);
        }
    }
}                      
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Boo")

                result.AssertLabeledSpansAre("decl1", "new Del(A.Boo)", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("Stmt2", "Boo(A.Boo);", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("Stmt3", "A.Boo(Boo);", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(552520, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552520")>
        Public Sub RenameFunctionNameToDelegateTypeConflict4()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;

class A
{
    static void Foo(int i) { }
    static void Foo(string s) { }
    
    class B
    {
        delegate void Del(string s);
        void [|$$Bar|](string s) { }
        void Boo()
        {
            Del d = new Del({|stmt1:Foo|});
        }
    }
}
                       </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "Del d = new Del(A.Foo);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(552722, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552722")>
        Public Sub RenameActionTypeConflict()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
using System;
class A
{
    static Action<int> [|$$Baz|] = (int x) => { };    
    class B
    {
        Action<int> Bar = (int x) => { };
        void Foo()
        {
            {|Stmt1:Baz|}(3);            
        }
    }
}]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("Stmt1", "A.Bar(3);", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(552722, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552722")>
        Public Sub RenameConflictAttribute1()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
[{|escape:Bar|}]
class Bar : System.Attribute  
{ }

class [|$$FooAttribute|] : System.Attribute 
{ }
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarAttribute")


                result.AssertLabeledSpansAre("escape", "@Bar", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameConflictAttribute2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;
[{|Resolve:B|}]
class [|$$BAttribute|] : Attribute
{
}
class AAttributeAttribute : Attribute
{
}
                        </Document>
                    </Project>
                </Workspace>, renameTo:="AAttribute")


                result.AssertLabeledSpecialSpansAre("Resolve", "A", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <WorkItem(576573, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576573")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug576573_ConflictAttributeWithNamespace()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;

namespace X
{
    class BAttribute
        : System.Attribute
    { }

    namespace Y.[|$$Z|]
    {

        [{|Resolve:B|}]
        class Foo { }    
    }
}
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BAttribute")


                result.AssertLabeledSpansAre("Resolve", "X.B", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(579602, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579602")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug579602_RenameFunctionWithDynamicParameter()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;

class A
{    
    class B
    {        
        public void [|Boo|](int d) { } //Line 1    
    }
    
    void Bar()    
    {        
        B b = new B();
        dynamic d = 1.5f;
        b.{|stmt1:$$Boo|}(d); //Line 2 Rename Boo to Foo    
    }
}
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "Foo", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub IdentifyConflictsWithVar()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class [|$$vor|]
{
    static void Main(string[] args)
    {
        {|conflict:var|} x = 23;
    }
}
                        </Document>
                    </Project>
                </Workspace>, renameTo:="v\u0061r")


                result.AssertLabeledSpansAre("conflict", "var", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(633180, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633180")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CS_DetectOverLoadResolutionChangesInEnclosingInvocations()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;
 
static class C
{
    static void Ex(this string x) { }
 
    static void Outer(Action&lt;string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action&lt;int> x, int y) { Console.WriteLine(2); }
 
    static void Inner(Action&lt;string> x, string y) { }
    static void Inner(Action&lt;string> x, int y) { }
    static void Inner(Action&lt;int> x, int y) { }
 
    static void Main()
    {
       {|resolved:Outer|}(y => {|resolved:Inner|}(x => x.Ex(), y), 0);
    }
}
 
static class E
{
    public static void [|$$Ex|](this int x) { } // Rename Ex to Foo
}

                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")

                result.AssertLabeledSpansAre("resolved", "Outer((string y) => Inner(x => x.Ex(), y), 0);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(635622, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/635622")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ExpandingDynamicAddsObjectCast()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;
 
class C
{
    static void [|$$Foo|](int x, Action y) { } // Rename Foo to Bar
    static void Bar(dynamic x, Action y) { }
 
    static void Main()
    {
        {|resolve:Bar|}(1, Console.WriteLine);
    }
}


                        </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("resolve", "Bar((object)1, Console.WriteLine);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(673562, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673562")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameNamespaceConflictsAndResolves()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;
 
namespace N
{
    class C
    {
        {|resolve:N|}.C x;
        /// &lt;see cref="{|resolve:N|}.C"/&gt;
        void Sub()
        { }
    }
    namespace [|$$K|] // Rename K to N
    {
        class C
        { }
    }
}


                        </Document>
                    </Project>
                </Workspace>, renameTo:="N")


                result.AssertLabeledSpansAre("resolve", "global::N", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(673667, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673667")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameUnnecessaryExpansion()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
namespace N
{
    using K = {|stmt1:N|}.C;
    class C
    {
    }
    class [|$$D|] // Rename D to N
    {
        class C
        {
            [|D|] x;
        }
    }
}


                        </Document>
                    </Project>
                </Workspace>, renameTo:="N")


                result.AssertLabeledSpansAre("stmt1", "global::N", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(768910, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768910")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInCrefPreservesWhitespaceTrivia()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
                            <![CDATA[
public class A
{
	public class B
	{
		public class C
		{

		}

		/// <summary>
		/// <see cref=" {|Resolve:D|}"/>  
		/// </summary>
		public static void [|$$foo|]() // Rename foo to D
		{
		}
	}
	public class D
	{
	}
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="D")


                result.AssertLabeledSpansAre("Resolve", "A.D", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

#Region "Type Argument Expand/Reduce for Generic Method Calls - 639136"
        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;

class C
{
    static void F&lt;T&gt;(Func&lt;int, T&gt; x) { }
    static void [|$$B|](Func&lt;int, int&gt; x) { } // Rename Bar to Foo

    static void Main()
    {
        {|stmt1:F|}(a => a);
    }
}
                        </Document>
                    </Project>
                </Workspace>, renameTo:="F")


                result.AssertLabeledSpansAre("stmt1", "F<int>(a => a);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <WorkItem(725934, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/725934")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_This()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;

class C
{
    void TestMethod()
    {
        int x = 1;
        Func&lt;int&gt; y = delegate { return {|stmt1:Foo|}(x); };
    }

    int Foo&lt;T&gt;(T x) { return 1; }
    int [|$$Bar|](int x) { return 1; }
}
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "return Foo<int>(x);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_Nested()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    public static void [|$$Foo|]<T>(T x) { }
    public static void Bar(int x) { }
    class D
    {
        void Bar<T>(T x) { }
        void Bar(int x) { }
        void sub()
        {
            {|stmt1:Foo|}(1);
        }
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("stmt1", "C.Bar<int>(1);", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub
        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_ReferenceType()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    public int Foo<T>(T x) { return 1; }
    public int [|$$Bar|](string x) {return 1; } // Rename Bar to Foo
    public void Test()
    {
        string one = "1";
        {|stmt1:Foo|}(one);
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "Foo<string>(one);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_ConstructedTypeArgumentNonGenericContainer()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    public static void Foo<T>(T x) { }
    public static void [|$$Bar|](D<int> x) { } // Rename Bar to Foo
    public void Sub()
    {
        D<int> x = new D<int>();
        {|stmt1:Foo|}(x); 
    }
}

class D<T>
{}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "Foo<D<int>>(x);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_SameTypeParameter()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
using System.Linq.Expressions;
class C
{
    public static int Foo<T>(T x) { return 1; }
    public static int [|$$Bar|]<T>(Expression<Func<int, T>> x) { return 1; }
    Expression<Func<int, int>> x = (y) => Foo(1);
    public void sub()
    {
        {|stmt1:Foo|}(x);
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "Foo<Expression<Func<int, int>>>(x);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_ArrayTypeParameter()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    public void [|$$Foo|]<S>(S x) { }
    public void Bar(int[] x) { }
    public void Sub()
    {
        var x = new int[] { 1, 2, 3 };
        {|stmt1:Foo|}(x);
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("stmt1", "Bar<int[]>(x);", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_MultiDArrayTypeParameter()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    public void Foo<S>(S x) { }
    public void [|$$Bar|](int[,] x) { }
    public void Sub()
    {
        var x = new int[,] { { 1, 2 }, { 2, 3 }  };
        {|stmt1:Foo|}(x);
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "Foo<int[,]>(x);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_UsedAsArgument()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    public int Foo<T>(T x) { return 1; }
    public int [|$$Bar|](int x) {return 1; }
    public void Sub(int x) { }
    public void Test()
    {
        Sub({|stmt1:Foo|}(1));
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "Sub(Foo<int>(1));", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_UsedInConstructorInitialization()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    public C(int x) { }
    public int Foo<T>(T x) { return 1; }
    public int [|$$Bar|](int x) {return 1; }
    public void Test()
    {
        C c = new C({|stmt1:Foo|}(1));
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "C c = new C(Foo<int>(1));", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_CalledOnObject()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    public int Foo<T>(T x) { return 1; }
    public int [|$$Bar|](int x) {return 1; } // Rename Bar to Foo
    public void Test()
    {
        C c = new C();
        c.{|stmt1:Foo|}(1);
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "c.Foo<int>(1);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_UsedInGenericDelegate()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    delegate int FooDel<T>(T x);
    public int Foo<T>(T x) { return 1; }
    public int [|$$Bar|](int x) {return 1; } // Rename Bar to Foo
    public void Test()
    {
        FooDel<int> foodel = new FooDel<int>({|stmt1:Foo|});
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "FooDel<int> foodel = new FooDel<int>(Foo<int>);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_UsedInNonGenericDelegate()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    delegate int FooDel(int x);
    public int Foo<T>(T x) { return 1; }
    public int [|$$Bar|](int x) {return 1; } // Rename Bar to Foo
    public void Test()
    {
        FooDel foodel = new FooDel({|stmt1:Foo|});
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "FooDel foodel = new FooDel(Foo<int>);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_MultipleTypeParameters()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    public void Foo<T, S>(T x, S y) { }
    public void [|$$Bar|]<U, P>(U[] x, P y) { }
    public void Sub()
    {
        int[] x;
        {|stmt1:Foo|}(x, new C());
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "Foo<int[], C>(x, new C());", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <WorkItem(730781, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/730781")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_ConflictInDerived()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    public void Foo<T>(T x) { }
}
class D : C
{
    public void [|$$Bar|](int x) { }
    public void Sub()
    {
        {|stmt1:Foo|}(1);
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "base.Foo(1);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(728653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/728653")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameGenericInvocationWithDynamicArgument()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    public void F<T>(T s) { }
    public void [|$$Bar|](int s) { } // Rename Bar to F
    public void sub()
    {
        dynamic x = 1;
        {|stmt1:F|}(x);
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="F")


                result.AssertLabeledSpansAre("stmt1", "F", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(728646, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/728646")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ExpandInvocationInStaticMemberAccess()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    public static void Foo<T>(T x) { }
    public static void [|$$Bar|](int x) { } // Rename Bar to Foo
    public void Sub()
    {
        
    }
}
 
class D
{
    public void Sub()
    {
        C.{|stmt1:Foo|}(1);
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "C.Foo<int>(1);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(728628, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/728628")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RecursiveTypeParameterExpansionFail()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C<T>
{
    public static void Foo<T>(T x) { }
    public static void [|$$Bar|](C<int> x) { } // Rename Bar to Foo
    public void Sub()
    {
        C<int> x = new C<int>();
        {|stmt1:Foo|}(x); 
    }
}

]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "Foo<C<int>>(x);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(728575, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/728575")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCrefWithProperBracesForTypeInferenceAdditionToMethod()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{   
    public static void Zoo<T>(T x) { }
    /// <summary>
    /// <see cref="{|cref1:Zoo|}"/>
    /// </summary>
    /// <param name="x"></param>
    public void [|$$Too|](int x) { } // Rename to Zoo
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Zoo")


                result.AssertLabeledSpansAre("cref1", "C.Zoo{T}", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_GenericBase()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C<T>
{
    public static void Foo<T>(T x) { }
    public static void [|$$Bar|](int x) { } // Rename Bar to Foo
}

class D : C<int>
{
    public void Test()
    {
        {|stmt1:Foo|}(1);
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "Foo<int>(1);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
        <WpfFact(Skip:="Story 736967"), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub GenericNameTypeInferenceExpansion_InErrorCode()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    public void Foo<T>(T y,out T x)
    {
        x = y;
    }
    public void [|$$Bar|](int y, out int x) // Rename Bar to Foo
    {
        x = 1;
    }
    public void Test()
    {
        int y = 1;
        int x;
        {|stmt1:Foo|}(y, x); // error in code, but Foo is bound to Foo<T>
    }
}

]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "Foo<int>(y, x);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub
#End Region

        <WorkItem(1016652, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1016652")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CS_ConflictBetweenTypeNamesInTypeConstraintSyntax()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System.Collections.Generic;

// rename INamespaceSymbol to ISymbol
public interface {|unresolved1:$$INamespaceSymbol|} { }

public interface {|DeclConflict:ISymbol|} { }

public interface IReferenceFinder { }

internal abstract partial class AbstractReferenceFinder<TSymbol> : IReferenceFinder
    where TSymbol : {|unresolved2:INamespaceSymbol|}
{

}]]></Document>
                    </Project>
                </Workspace>, renameTo:="ISymbol")

                result.AssertLabeledSpansAre("DeclConflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("unresolved1", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("unresolved2", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(1193, "https://github.com/dotnet/roslyn/issues/1193")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub MemberQualificationInNameOfUsesTypeName_StaticReferencingInstance()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    static void F(int [|$$z|])
    {
        string x = nameof({|ref:zoo|});
    }

    int zoo;
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="zoo")

                result.AssertLabeledSpansAre("ref", "string x = nameof(C.zoo);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(1193, "https://github.com/dotnet/roslyn/issues/1193")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub MemberQualificationInNameOfUsesTypeName_InstanceReferencingStatic()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    void F(int [|$$z|])
    {
        string x = nameof({|ref:zoo|});
    }

    static int zoo;
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="zoo")

                result.AssertLabeledSpansAre("ref", "string x = nameof(C.zoo);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(1193, "https://github.com/dotnet/roslyn/issues/1193")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub MemberQualificationInNameOfUsesTypeName_InstanceReferencingInstance()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    void F(int [|$$z|])
    {
        string x = nameof({|ref:zoo|});
    }

    int zoo;
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="zoo")

                result.AssertLabeledSpansAre("ref", "string x = nameof(C.zoo);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(1193, "https://github.com/dotnet/roslyn/issues/1193")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub MemberQualificationInNameOfMethodInvocationUsesThisDot()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    int zoo;

    void F(int [|$$z|])
    {
        string x = nameof({|ref:zoo|});
    }

    void nameof(int x) { }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="zoo")

                result.AssertLabeledSpansAre("ref", "string x = nameof(this.zoo);", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(1053, "https://github.com/dotnet/roslyn/issues/1053")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameComplexifiesInLambdaBodyExpression()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    static int [|$$M|](int b) => 5;
    static int N(long b) => 5;
    System.Func<int, int> a = d => {|resolved:N|}(1);
    System.Func<int> b = () => {|resolved:N|}(1);
}]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="N")

                result.AssertLabeledSpansAre("resolved", "N((long)1)", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(1053, "https://github.com/dotnet/roslyn/issues/1053")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameComplexifiesInExpressionBodiedMembers()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class C
{
    int f = new C().{|resolved1:N|}(0);
    int [|$$M|](int b) => {|resolved2:N|}(0);
    int N(long b) => [|M|](0);
    int P => {|resolved2:N|}(0);
}]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="N")

                result.AssertLabeledSpansAre("resolved1", "new C().N((long)0)", RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansAre("resolved2", "N((long)0)", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(1027506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027506")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TestConflictBetweenClassAndInterface1()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class {|conflict:C|} { }
interface [|$$I|] { }
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="C")

                result.AssertLabeledSpansAre("conflict", "C", RelatedLocationType.UnresolvableConflict)
            End Using
        End Sub

        <WorkItem(1027506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027506")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TestConflictBetweenClassAndInterface2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class [|$$C|] { }
interface {|conflict:I|} { }
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="I")

                result.AssertLabeledSpansAre("conflict", "I", RelatedLocationType.UnresolvableConflict)
            End Using
        End Sub

        <WorkItem(1027506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027506")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TestConflictBetweenClassAndNamespace1()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class {|conflict:$$C|} { }
namespace N { }
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="N")

                result.AssertLabeledSpansAre("conflict", "N", RelatedLocationType.UnresolvableConflict)
            End Using
        End Sub

        <WorkItem(1027506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027506")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TestConflictBetweenClassAndNamespace2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
class {|conflict:C|} { }
namespace [|$$N|] { }
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="C")

                result.AssertLabeledSpansAre("conflict", "C", RelatedLocationType.UnresolvableConflict)
            End Using
        End Sub

        <WorkItem(1027506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027506")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TestNoConflictBetweenTwoNamespaces()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
namespace [|$$N1|][ { }
namespace N2 { }
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="N2")
            End Using
        End Sub

        <WorkItem(1729, "https://github.com/dotnet/roslyn/issues/1729")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TestNoConflictWithParametersOrLocalsOfDelegateType()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
using System;
class C
{
    void M1(Action [|callback$$|])
    {
        [|callback|]();
    }

    void M2(Func<bool> callback)
    {
        callback();
    }

    void M3()
    {
        Action callback = () => { };
        callback();
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="callback2")
            End Using
        End Sub

        <WorkItem(1729, "https://github.com/dotnet/roslyn/issues/1729")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TestConflictWithLocalsOfDelegateTypeWhenBindingChangesToNonDelegateLocal()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs"><![CDATA[
using System;
class C
{
    void M()
    {
        int [|x$$|] = 7; // Rename x to a. "a()" will bind to the first definition of a.
        Action {|conflict:a|} = () => { };
        {|conflict:a|}();
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="a")

                result.AssertLabeledSpansAre("conflict", "a", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(446, "https://github.com/dotnet/roslyn/issues/446")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub NoCrashOrConflictOnRenameWithNameOfInAttribute()
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
class C
{
    static void [|T|]$$(int x) { }

    [System.Obsolete(nameof(Test))]
    static void Test() { }
}
                            </Document>
                       </Project>
                   </Workspace>, renameTo:="Test")
            End Using
        End Sub

        <WorkItem(1195, "https://github.com/dotnet/roslyn/issues/1195")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictWhenNameOfReferenceDoesNotBindToAnyOriginalSymbols()
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
class C
{
    void Test()
    {
        int [|T$$|];
        var x = nameof({|conflict:Test|});
    }
}
                            </Document>
                       </Project>
                   </Workspace>, renameTo:="Test")

                result.AssertLabeledSpansAre("conflict", "Test", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(1195, "https://github.com/dotnet/roslyn/issues/1195")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub NoConflictWhenNameOfReferenceDoesNotBindToSomeOriginalSymbols()
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
class Program
{
    void [|$$M|](int x) { }
    void M() { var x = nameof(M); }
}
                            </Document>
                       </Project>
                   </Workspace>, renameTo:="X")
            End Using
        End Sub

        <WorkItem(1195, "https://github.com/dotnet/roslyn/issues/1195")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub NoConflictWhenNameOfReferenceBindsToSymbolForFirstTime()
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
class Program
{
    void [|X$$|]() { }
    void M() { var x = nameof(T); }
}
                            </Document>
                       </Project>
                   </Workspace>, renameTo:="T")
            End Using
        End Sub

        <WorkItem(1195, "https://github.com/dotnet/roslyn/issues/1195")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictWhenNameOfReferenceChangesBindingFromMetadataToSource()
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
using System;

class Program
{
    static void M()
    {
        var [|Consol$$|] = 7;
        var x = nameof({|conflict:Console|});
    }
}
                            </Document>
                       </Project>
                   </Workspace>, renameTo:="Console")

                result.AssertLabeledSpansAre("conflict", "Console", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(1031, "https://github.com/dotnet/roslyn/issues/1031")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub InvalidNamesDoNotCauseCrash_IntroduceQualifiedName()
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
class {|conflict:C$$|} { }
                            </Document>
                       </Project>
                   </Workspace>, renameTo:="C.D")

                result.AssertReplacementTextInvalid()
                result.AssertLabeledSpansAre("conflict", "C.D", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(1031, "https://github.com/dotnet/roslyn/issues/1031")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub InvalidNamesDoNotCauseCrash_AccidentallyPasteLotsOfCode()
            Dim renameTo = "class C { public void M() { for (int i = 0; i < 10; i++) { System.Console.Writeline(""This is a test""); } } }"

            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
class {|conflict:C$$|} { }
                            </Document>
                       </Project>
                   </Workspace>, renameTo)

                result.AssertReplacementTextInvalid()
                result.AssertLabeledSpansAre("conflict", renameTo, RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(2352, "https://github.com/dotnet/roslyn/issues/2352")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub DeclarationConflictInFileWithoutReferences_SameProject()
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document FilePath="Test1.cs">
class Program
{
    internal void [|A$$|]() { }
    internal void {|conflict:B|}() { }
}
                            </Document>
                           <Document FilePath="Test2.cs">
class Program2
{
    void M()
    {
        Program p = null;
        p.{|conflict:A|}();
        p.{|conflict:B|}();
    }
}
                            </Document>
                       </Project>
                   </Workspace>, renameTo:="B")

                result.AssertLabeledSpansAre("conflict", "B", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(2352, "https://github.com/dotnet/roslyn/issues/2352")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub DeclarationConflictInFileWithoutReferences_DifferentProjects()
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true" AssemblyName="CSAssembly1">
                           <Document FilePath="Test1.cs">
public class Program
{
    public void [|A$$|]() { }
    public void {|conflict:B|}() { }
}
                            </Document>
                       </Project>
                       <Project Language="C#" CommonReferences="true" AssemblyName="CSAssembly2">
                           <ProjectReference>CSAssembly1</ProjectReference>
                           <Document FilePath="Test2.cs">
class Program2
{
    void M()
    {
        Program p = null;
        p.{|conflict:A|}();
        p.{|conflict:B|}();
    }
}
                            </Document>
                       </Project>
                   </Workspace>, renameTo:="B")

                result.AssertLabeledSpansAre("conflict", "B", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(2352, "https://github.com/dotnet/roslyn/issues/2352")>
        <WorkItem(3303, "https://github.com/dotnet/roslyn/issues/3303")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub DeclarationConflictInFileWithoutReferences_PartialTypes()
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document FilePath="Test1.cs">
partial class C
{
    private static void [|$$M|]()
    {
        {|conflict:M|}();
    }
}
                            </Document>
                           <Document FilePath="Test2.cs">
partial class C
{
    private static void {|conflict:Method|}()
    {
    }
}
                            </Document>
                       </Project>
                   </Workspace>, renameTo:="Method")

                result.AssertLabeledSpansAre("conflict", "Method", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(1439, "https://github.com/dotnet/roslyn/issues/1439")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInsideNameOf1()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    int field;

    static void Main(string[] args)
    {
        // Rename "local" to "field"
        int [|$$local|];

        nameof({|Conflict:field|}).ToString(); // Should also expand to Program.field
    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="field")

                result.AssertLabeledSpansAre("Conflict", replacement:="nameof(Program.field).ToString();", type:=RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(1439, "https://github.com/dotnet/roslyn/issues/1439")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInsideNameOf2()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    int field;

    static void Main(string[] args)
    {
        // Rename "local" to "field"
        int [|$$local|];

        nameof({|Conflict:field|})?.ToString(); // Should also expand to Program.field
    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="field")

                result.AssertLabeledSpansAre("Conflict", replacement:="nameof(Program.field)?.ToString();", type:=RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(1439, "https://github.com/dotnet/roslyn/issues/1439")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInsideNameOf3()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    static int field;

    static void Main(string[] args)
    {
        // Rename "local" to "field"
        int [|$$local|];

        Program.nameof({|Conflict:field|}); // Should also expand to Program.field
    }

    static void nameof(string s) { }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="field")

                result.AssertLabeledSpansAre("Conflict", replacement:="Program.nameof(Program.field);", type:=RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(7440, "https://github.com/dotnet/roslyn/issues/7440")>
        Public Sub RenameTypeParameterInPartialClass()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
partial class C&lt;[|$$T|]&gt; {}
partial class C&lt;[|T|]&gt; {}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="T2")
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(7440, "https://github.com/dotnet/roslyn/issues/7440")>
        Public Sub RenameMethodToConflictWithTypeParameter()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
partial class C&lt;{|Conflict:T|}&gt; { void [|$$M|]() { } }
partial class C&lt;{|Conflict:T|}&gt; {}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="T")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub
    End Class
End Namespace