﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    <[UseExportProvider]>
    Public Class DeclarationConflictTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <WpfFact>
        <WorkItem(917043, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/917043")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub NoConflictForDelegate()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using System;
class C
{
    void M(Comparison&lt;C> [|$$comparison|])
    {
       [|comparison|](null, null);
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="bar")
            End Using
        End Sub

        <WpfFact(Skip:="917043")>
        <WorkItem(917043, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/917043")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub NoConflictForIsolatedScopes()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using System;
class C
{
    void M()
    {
       { int [|$$x|] = 3; }
       { F(); }
    }
    void F(){}
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="F")
            End Using
        End Sub


        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenFields()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Goo
{
    int [|$$goo|];
    int {|Conflict:bar|};
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenFieldAndMethodDeclaration()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Goo
{
    int [|$$goo|];
    int {|Conflict:bar|}() { }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenPropertyAndFieldDeclaration()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Program
{
    int {|Conflict:bar|} { get; set; }
    int [|$$goo|]() { return 0; }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenMethodDeclarations()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Goo
{
    int [|$$goo|]() { }
    int {|Conflict:bar|}() { }
}
                               </Document>
                    </Project>
                </Workspace>, renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenParameterDeclarations()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Goo
{
    int f(int [|$$goo|], int {|Conflict:bar|}) { }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub NoConflictBetweenMethodsOfDifferentSignature()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Goo
{
    int [|$$goo|]() { }
    int bar(int parameter) { }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="bar")

            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenMemberDeclarationsWithOutOrRefDifferenceOnly()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Goo
{
    int [|$$goo|](out int parameter) { }
    int {|Conflict:bar|}(int parameter) { }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub NoConflictBetweenMethodsDifferingByArity()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
class Goo
{
    int [|$$goo|](int parameter) { }
    int bar<T>(int parameter) { }
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="bar")

            End Using
        End Sub

        <Fact>
        <WorkItem(546429, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546429")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub NoConflictWithNamespaceDefinedInMetadata()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
namespace [|$$Goo|] { }
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="System")

            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub NoConflictWithEquallyNamedNamespaces()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
namespace [|$$Goo|] { }
namespace N1 { }
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="N1")

            End Using
        End Sub

        <WorkItem(608198, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608198")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CS_ConflictInFieldInitializerOfFieldAndModuleNameResolvedThroughFullQualification()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;
using ns;
namespace ns
{
    class A
    { }
}

class [|$$C|]
{
    class B
    {
        public Action a = () => {var y = new {|stmt1:A|}();};
    }
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="A")

                result.AssertLabeledSpansAre("stmt1", "var y = new ns.A();", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(543654, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543654")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CS_NoConflictBetweenLambdaParameterAndField()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;

class Program
{
    public Action<int> y = ([|$$c|]) => {};
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="y")
            End Using
        End Sub

        <WorkItem(529986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529986")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CS_ConflictBetweenTypeParametersInTypeDeclaration()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;

class Program<{|declconflict:A|}, [|$$B|]>
{
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="A")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(529986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529986")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CS_ConflictBetweenTypeParametersInMethodDeclaration()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;

class Program
{
    public void Meth<{|declconflict:A|}, [|$$B|]>()
    {}
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="A")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(529986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529986")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CS_ConflictBetweenTypeParametersInMethodDeclaration_2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;

class Program
{
    public void Meth<{|declconflict:@a|}, [|$$B|]>()
    {}
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="\u0061")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(529986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529986")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CS_ConflictBetweenTypeParameterAndMember_1()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;

class Program<{|declconflict:@a|}>
{
    public void [|$$B|]()
    {}
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="\u0061")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(529986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529986")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CS_ConflictBetweenTypeParameterAndMember_2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;

class Program<{|declconflict:@a|}>
{
    public int [|$$B|]() = 23;
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="\u0061")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(658801, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658801")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CS_OverridingImplicitlyUsedMethod()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;

class A
{
    public int Current { get; set; }
    public bool MoveNext()
    {
        return false;
    }
}
 
class C : A
{
    static void Main()
    {
        foreach (var x in new C()) { }
    }
 
    public C GetEnumerator()
    {
        return this;
    }
 
    public void {|possibleImplicitConflict:$$Goo|}() { } // Rename Goo to MoveNext
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="MoveNext")

                result.AssertLabeledSpansAre("possibleImplicitConflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(682669, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682669")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CS_OverridingImplicitlyUsedMethod_1()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;

class A
{
    public int Current { get; set; }
    public bool MoveNext()
    {
        return false;
    }
}
 
class C : A
{
    static void Main()
    {
        foreach (var x in new C()) { }
    }
 
    public C GetEnumerator()
    {
        return this;
    }
 
    public void [|$$Goo|]<T>() { } // Rename Goo to MoveNext
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="MoveNext")

            End Using
        End Sub

        <WorkItem(682669, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682669")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CS_OverridingImplicitlyUsedMethod_2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;

class A
{
    public int Current { get; set; }
    public bool MoveNext<T>()
    {
        return false;
    }
}
 
class C : A
{
    static void Main()
    {
    }
 
    public C GetEnumerator()
    {
        return this;
    }
 
    public void [|$$Goo|]() { } // Rename Goo to MoveNext
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="MoveNext")

            End Using
        End Sub

        <WorkItem(851604, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/851604")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictInsideAttributeArgument()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using System.ComponentModel;
using System.Reflection;
class C
{
    const MemberTypes {|first:$$METHOD|} = MemberTypes.Method;
    delegate void D([DefaultValue({|second:METHOD|})] object x);
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Method")

                result.AssertLabeledSpansAre("first", "Method", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("second", "DefaultValue(C.Method)", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <WorkItem(6306, "https://github.com/dotnet/roslyn/issues/6306")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ResolveConflictInAnonymousTypeProperty()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;
class C
{
    void X<T>(T t, Func<T, long> e) { {|first:X|}(new { a = 1 }, a => a.a); }

    [Obsolete]
    void {|origin:$$Y|}<T>(T t, Func<T, int> e) { }
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="X")

                result.AssertLabeledSpansAre("first", "X(new { a = 1 }, a => (long)a.a);", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansAre("origin", "X", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(6308, "https://github.com/dotnet/roslyn/issues/6308")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ResolveConflictWhenAnonymousTypeIsUsedAsGenericArgument()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;
class C
{
    void M<T>(T t, Func<T, int, int> e) { }
    int M<T>(T t, Func<T, long, long> e) => {|first:M|}(new { }, (_, a) => {|second:X|}(a));

    long X(long a) => a;
    int {|origin:$$Y|}(int a) => a;
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="X")

                result.AssertLabeledSpansAre("first", "M(new { }, (_, a) => (long)X(a))", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansAre("second", "M(new { }, (_, a) => (long)X(a))", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansAre("origin", "X", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(18566, "https://github.com/dotnet/roslyn/issues/18566")>
        Public Sub ParameterInPartialMethodDefinitionConflictingWithLocalInPartialMethodImplementation()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
partial class C
{
    partial void M(int {|parameter0:$$x|});
}
                        </Document>
                        <Document>
partial class C
{
    partial void M(int {|parameter1:x|})
    {
        int {|local0:y|} = 1;
    }
}
                        </Document>
                    </Project>
                </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("parameter0", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("parameter1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("local0", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub
    End Class
End Namespace
