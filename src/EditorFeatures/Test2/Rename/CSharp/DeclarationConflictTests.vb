' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class DeclarationConflictTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <WpfTheory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/917043")>
        <CombinatorialData>
        Public Sub NoConflictForDelegate(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="bar")
            End Using
        End Sub

        <WpfTheory(Skip:="917043")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/917043")>
        <CombinatorialData>
        Public Sub NoConflictForIsolatedScopes(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="F")
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictBetweenFields(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictBetweenFieldAndMethodDeclaration(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictBetweenPropertyAndFieldDeclaration(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictBetweenMethodDeclarations(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictBetweenParameterDeclarations(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub NoConflictBetweenMethodsOfDifferentSignature(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="bar")

            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictBetweenMemberDeclarationsWithOutOrRefDifferenceOnly(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub NoConflictBetweenMethodsDifferingByArity(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="bar")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546429")>
        <CombinatorialData>
        Public Sub NoConflictWithNamespaceDefinedInMetadata(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
namespace [|$$Goo|] { }
                        ]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="System")

            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub NoConflictWithEquallyNamedNamespaces(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
namespace [|$$Goo|] { }
namespace N1 { }
                        ]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="N1")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608198")>
        <CombinatorialData>
        Public Sub CS_ConflictInFieldInitializerOfFieldAndModuleNameResolvedThroughFullQualification(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="A")

                result.AssertLabeledSpansAre("stmt1", "var y = new ns.A();", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543654")>
        <CombinatorialData>
        Public Sub CS_NoConflictBetweenLambdaParameterAndField(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="y")
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529986")>
        <CombinatorialData>
        Public Sub CS_ConflictBetweenTypeParametersInTypeDeclaration(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="A")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529986")>
        <CombinatorialData>
        Public Sub CS_ConflictBetweenTypeParametersInMethodDeclaration(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="A")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529986")>
        <CombinatorialData>
        Public Sub CS_ConflictBetweenTypeParametersInMethodDeclaration_2(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="\u0061")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529986")>
        <CombinatorialData>
        Public Sub CS_ConflictBetweenTypeParameterAndMember_1(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="\u0061")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529986")>
        <CombinatorialData>
        Public Sub CS_ConflictBetweenTypeParameterAndMember_2(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="\u0061")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658801")>
        <CombinatorialData>
        Public Sub CS_OverridingImplicitlyUsedMethod(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="MoveNext")

                result.AssertLabeledSpansAre("possibleImplicitConflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682669")>
        <CombinatorialData>
        Public Sub CS_OverridingImplicitlyUsedMethod_1(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="MoveNext")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682669")>
        <CombinatorialData>
        Public Sub CS_OverridingImplicitlyUsedMethod_2(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="MoveNext")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/851604")>
        Public Sub ConflictInsideAttributeArgument(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="Method")

                result.AssertLabeledSpansAre("first", "Method", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("second", "DefaultValue(C.Method)", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/6306")>
        Public Sub ResolveConflictInAnonymousTypeProperty(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="X")

                result.AssertLabeledSpansAre("first", "X(new { a = 1 }, a => (long)a.a);", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansAre("origin", "X", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/6308")>
        Public Sub ResolveConflictWhenAnonymousTypeIsUsedAsGenericArgument(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="X")

                result.AssertLabeledSpansAre("first", "M(new { }, (_, a) => (long)X(a))", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansAre("second", "M(new { }, (_, a) => (long)X(a))", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansAre("origin", "X", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/18566")>
        Public Sub ParameterInPartialMethodDefinitionConflictingWithLocalInPartialMethodImplementation(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("parameter0", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("parameter1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("local0", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub
    End Class
End Namespace
