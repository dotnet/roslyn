' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class LocalConflictTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539939")>
        Public Sub ConflictingLocalWithLocal(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
                </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", "y", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539939")>
        Public Sub ConflictingLocalWithParameter(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
                </Workspace>, host:=host, renameTo:="args")

                result.AssertLabeledSpansAre("stmt1", "args", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictingLocalWithForEachRangeVariable(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
                </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictingLocalWithForLoopVariable(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
                </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictingLocalWithUsingBlockVariable(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
                </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictingLocalWithSimpleLambdaParameter(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;

class Program
{
    static void Main(string[] args)
    {
        int {|stmt1:$$x|} = 1;
        Func<int> lambda = y => 42;
    }
}
                            ]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", "y", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictingLocalWithParenthesizedLambdaParameter(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;

class Program
{
    static void Main(string[] args)
    {
        int {|stmt1:$$x|} = 1;
        Func<int> lambda = (y) => 42;
    }
}
                            ]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", "y", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictingFromClauseWithLetClause(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
                </Workspace>, host:=host, renameTo:="y")

                ' We have two kinds of conflicts here: we flag a declaration conflict on the let:
                result.AssertLabeledSpansAre("DeclarationConflict", type:=RelatedLocationType.UnresolvedConflict)

                ' And also for the y in the select clause. The compiler binds the "y" to the let
                ' clause's y.
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543407")>
        <CombinatorialData>
        Public Sub ConflictBetweenLabelsInSameMethod(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;

public class C
{
    public void Goo()
    {
    {|stmt1:$$Bar|}:;
    {|Conflict:Goo|}:;
    }
}

                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Goo")

                result.AssertLabeledSpansAre("stmt1", "Goo", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543407")>
        <CombinatorialData>
        Public Sub ConflictBetweenLabelInMethodAndLambda(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;

public class C
{
    public void Goo()
    {
    {|stmt1:$$Bar|}: ;

        Action x = () => { {|Conflict:Goo|}:;};
    }
}
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Goo")

                result.AssertLabeledSpansAre("stmt1", "Goo", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543407")>
        <CombinatorialData>
        Public Sub ConflictBetweenLabelsInLambda(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;

public class C
{
    Action x = () =>
        {
        {|Conflict:Goo|}:;
        {|stmt1:$$Bar|}: ;
        };
}
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Goo")

                result.AssertLabeledSpansAre("stmt1", "Goo", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543407")>
        <CombinatorialData>
        Public Sub NoConflictBetweenLabelsInTwoNonNestedLambdas(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;

public class C
{
    public void Goo()
    {
        Action x = () => { Goo:; };
        Action x = () => { {|stmt1:$$Bar|}:; };
    }
}
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Goo")

                result.AssertLabeledSpansAre("stmt1", "Goo", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545468")>
        Public Sub NoConflictsWithCatchBlockWithoutExceptionVariable(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
                </Workspace>, host:=host, renameTo:="j")

                result.AssertLabeledSpansAre("stmt1", "j", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1081066")>
        Public Sub NoConflictsBetweenCatchClauses(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
                </Workspace>, host:=host, renameTo:="j")

                result.AssertLabeledSpansAre("stmt1", "j", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1081066")>
        Public Sub ConflictsWithinCatchClause(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
                </Workspace>, host:=host, renameTo:="j")

                result.AssertLabeledSpansAre("stmt1", "j", RelatedLocationType.UnresolvableConflict)
                result.AssertLabeledSpansAre("stmt2", "j", RelatedLocationType.UnresolvableConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546163")>
        Public Sub NoConflictsWithCatchExceptionWithoutDeclaration(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
                </Workspace>, host:=host, renameTo:="j")

                result.AssertLabeledSpansAre("stmt1", "j", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/992721")>
        Public Sub ConflictingLocalWithFieldWithExtensionMethodInvolved(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
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
            </Workspace>, host:=host, renameTo:="list")

                result.AssertLabeledSpansAre("def", "list", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt1", "foreach (var i in this.list.OfType<int>()){}", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("stmt2", "this.list = list.ToList();", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/17177")>
        Public Sub ConflictsBetweenSwitchCaseStatementsWithoutBlocks(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Test
{
    static void Main()
    {
        switch (true)
        {
            case true:
                object {|stmt1:$$i|} = null;
                break;
            case false:
                object {|stmt2:j|} = null;
                break;
        }
    }
}
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="j")

                result.AssertLabeledSpansAre("stmt1", "j", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", "j", RelatedLocationType.UnresolvableConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/17177")>
        Public Sub NoConflictsBetweenSwitchCaseStatementsWithBlocks(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Test
{
    static void Main()
    {
        switch (true)
        {
            case true:
                {
                    object {|stmt1:$$i|} = null;
                    break;
                }
            case false:
                {
                    object j = null;
                    break;
                }
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="j")

                result.AssertLabeledSpansAre("stmt1", "j", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/17177")>
        Public Sub NoConflictsBetweenSwitchCaseStatementFirstStatementWithBlock(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Test
{
    static void Main()
    {
        switch (true)
        {
            case true:
                {
                    object {|stmt1:$$i|} = null;
                    break;
                }
            case false:
                object {|stmt2:j|} = null;
                break;
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="j")

                result.AssertLabeledSpansAre("stmt1", "j", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", "j", RelatedLocationType.UnresolvableConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/17177")>
        Public Sub NoConflictsBetweenSwitchCaseStatementSecondStatementWithBlock(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Test
{
    static void Main()
    {
        switch (true)
        {
            case true:
                object {|stmt1:$$i|} = null;
                break;
            case false:
                {
                    object {|stmt2:j|} = null;
                    break;
                }
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="j")

                result.AssertLabeledSpansAre("stmt1", "j", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", "j", RelatedLocationType.UnresolvableConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/10009531")>
        Public Sub NoConflictingLocalWithLocalFunctionParameter(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using System;

class C
{
    static void Main(string[] args)
    {
        int {|stmt1:$$t|} = 0;

        void ALocalFunction()
        {
            int t1 = 0;
        }
    }
}
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="t1")

                result.AssertLabeledSpansAre("stmt1", "t1", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/10009531")>
        Public Sub NoConflictingLocalWithStaticLocalFunctionParameter(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using System;

class C
{
    static void Main(string[] args)
    {
        int {|stmt1:$$t|} = 0;

        static void ALocalFunction()
        {
            int t1 = 0;
        }
    }
}
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="t1")

                result.AssertLabeledSpansAre("stmt1", "t1", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/10009531")>
        Public Sub NoConflictingLocalWithLocalFunctionLocal(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using System;

class C
{
    static void Main(string[] args)
    {
        int {|stmt1:$$t|} = 0;

        void ALocalFunction(int param)
        {
            int t1 = 0;
        }
    }
}
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="t1")

                result.AssertLabeledSpansAre("stmt1", "t1", RelatedLocationType.NoConflict)
            End Using
        End Sub
    End Class
End Namespace
