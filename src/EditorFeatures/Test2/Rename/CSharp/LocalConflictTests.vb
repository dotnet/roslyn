' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    Public Class LocalConflictTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(539939)>
        Public Sub ConflictingLocalWithLocal()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Program
{
    static void Main(string[] args)
    {
        int {|stmt1:$$x|} = 1;
        int {|Conflict:y|} = 2;
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", "y", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(539939)>
        Public Sub ConflictingLocalWithParameter()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Program
{
    static void Main(string[] {|Conflict:args|})
    {
        int {|stmt1:$$x|} = 1;
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="args")

                result.AssertLabeledSpansAre("stmt1", "args", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictingLocalWithForEachRangeVariable()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Program
{
    static void Main(string[] args)
    {
        int {|stmt1:$$x|} = 1;

        foreach (var {|Conflict:y|} in args) { }
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictingLocalWithForLoopVariable()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Program
{
    static void Main(string[] args)
    {
        int {|stmt1:$$x|} = 1;

        for (int {|Conflict:y|} = 0; ; } { }
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictingLocalWithUsingBlockVariable()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Program : IDisposable
{
    static void Main(string[] args)
    {
        int {|stmt1:$$x|} = 1;

        using (var {|Conflict:y|} = new Program()) { }
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictingLocalWithSimpleLambdaParameter()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;

class Program
{
    static void Main(string[] args)
    {
        int {|stmt1:$$x|} = 1;
        Func<int> lambda = {|Conflict:y|} => 42;
    }
}
                            ]]></Document>
                    </Project>
                </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictingLocalWithParenthesizedLambdaParameter()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;

class Program
{
    static void Main(string[] args)
    {
        int {|stmt1:$$x|} = 1;
        Func<int> lambda = ({|Conflict:y|}) => 42;
    }
}
                            ]]></Document>
                    </Project>
                </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictingFromClauseWithLetClause()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using System.Linq;
class C
{
    static void Main(string[] args)
    {
        var temp = from [|$$x|] in "abc"
                   let {|DeclarationConflict:y|} = [|x|].ToString()
                   select {|Conflict:y|};
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="y")

                ' We have two kinds of conflicts here: we flag a declaration conflict on the let:
                result.AssertLabeledSpansAre("DeclarationConflict", type:=RelatedLocationType.UnresolvedConflict)

                ' And also for the y in the select clause. The compiler binds the "y" to the let
                ' clause's y.
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub


        <WpfFact>
        <WorkItem(543407)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenLabelsInSameMethod()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;

public class C
{
    public void Foo()
    {
    {|stmt1:$$Bar|}:;
    {|Conflict:Foo|}:;
    }
}

                            </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")

                result.AssertLabeledSpansAre("stmt1", "Foo", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WpfFact>
        <WorkItem(543407)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenLabelInMethodAndLambda()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;

public class C
{
    public void Foo()
    {
    {|stmt1:$$Bar|}: ;

        Action x = () => { {|Conflict:Foo|}:;};
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")

                result.AssertLabeledSpansAre("stmt1", "Foo", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WpfFact>
        <WorkItem(543407)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenLabelsInLambda()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;

public class C
{
    Action x = () =>
        {
        {|Conflict:Foo|}:;
        {|stmt1:$$Bar|}: ;
        };
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")

                result.AssertLabeledSpansAre("stmt1", "Foo", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WpfFact>
        <WorkItem(543407)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub NoConflictBetweenLabelsInTwoNonNestedLambdas()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;

public class C
{
    public void Foo()
    {
        Action x = () => { Foo:; };
        Action x = () => { {|stmt1:$$Bar|}:; };
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")

                result.AssertLabeledSpansAre("stmt1", "Foo", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(545468)>
        Public Sub NoConflictsWithCatchBlockWithoutExceptionVariable()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Test
{
    static void Main()
    {
        try
        {
            int {|stmt1:$$i|};
        }
        catch (System.Exception)
        {         
        }
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="j")

                result.AssertLabeledSpansAre("stmt1", "j", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(1081066)>
        Public Sub NoConflictsBetweenCatchClauses()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using System;
class Test
{
    static void Main()
    {
        try { } catch (Exception {|stmt1:$$ex|}) { }
        try { } catch (Exception j) { }
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="j")

                result.AssertLabeledSpansAre("stmt1", "j", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(1081066)>
        Public Sub ConflictsWithinCatchClause()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using System;
class Test
{
    static void Main()
    {
        try { } catch (Exception {|stmt1:$$ex|}) { int {|stmt2:j|}; }
        try { } catch (Exception j) { }
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="j")

                result.AssertLabeledSpansAre("stmt1", "j", RelatedLocationType.UnresolvableConflict)
                result.AssertLabeledSpansAre("stmt2", "j", RelatedLocationType.UnresolvableConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(546163)>
        Public Sub NoConflictsWithCatchExceptionWithoutDeclaration()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Test
{
    static void Main()
    {
        try
        {
            int {|stmt1:$$i|};
        }
        catch
        {         
        }
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="j")

                result.AssertLabeledSpansAre("stmt1", "j", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(992721)>
        Public Sub ConflictingLocalWithFieldWithExtensionMethodInvolved()
            Using result = RenameEngineResult.Create(
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
using System.Collections.Generic;
using System.Linq;
namespace ConsoleApplication1
{
    class Program
    {
        private List&lt;object&gt; {|def:_list|};
        public Program(IEnumerable&lt;object&gt; list)
        {
            {|stmt2:_list|} = list.ToList();
            foreach (var i in {|stmt1:$$_list|}.OfType&lt;int&gt;()){}
        }
    }
}
                    </Document>
                </Project>
            </Workspace>, renameTo:="list")

                result.AssertLabeledSpansAre("def", "list", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt1", "foreach (var i in this.list.OfType<int>()){}", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("stmt2", "this.list = list.ToList();", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub
    End Class
End Namespace
