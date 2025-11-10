' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    ''' <summary>
    ''' This test class contains tests which verifies that the rename engine performs correct
    ''' refactorings that do not break user code. These tests call through the
    ''' Renamer.RenameSymbol entrypoint, and thus do not test any behavior with interactive
    ''' rename. The position given with the $$ mark in the tests is just the symbol that is renamed;
    ''' there is no fancy logic applied to it.
    ''' </summary>
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Partial Public Class RenameEngineTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543661")>
        Public Sub CannotRenameNamespaceAlias(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
                               using [|sys|] = System;
                               class Program
                               { 
                                   static void Main(string[] args)    
                                   {
                                       [|$$sys|].Console.Write("test");  
                                   }
                               }
                            </Document>
                       </Project>
                   </Workspace>, host:=host, renameTo:="Sys2")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/4904")>
        Public Sub RenameInaccessibleMember1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
class C
{
    void Main()
    {
        D.[|field|] = 8;
    }
}
 
class D
{
    private static int [|$$field|];
}                             </Document>
                       </Project>
                   </Workspace>, host:=host, renameTo:="_field")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/4904")>
        Public Sub RenameInaccessibleMember2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
class C
{
    void Main()
    {
        D.[|$$field|] = 8;
    }
}
 
class D
{
    private static int [|field|];
}                             </Document>
                       </Project>
                   </Workspace>, host:=host, renameTo:="_field")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/4904")>
        Public Sub RenameNonAttribute1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
[[|NotAnAttribute|]]
class C
{
}
 
class [|$$NotAnAttribute|]
{
}                             </Document>
                       </Project>
                   </Workspace>, host:=host, renameTo:="X")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/4904")>
        Public Sub RenameNonAttribute2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
[[|$$NotAnAttribute|]]
class C
{
}
 
class [|NotAnAttribute|]
{
}                             </Document>
                       </Project>
                   </Workspace>, host:=host, renameTo:="X")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/4904")>
        Public Sub RenameWrongArity1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
class [|$$List|]&lt;T&gt;
{
}
 
class C
{
    [|List|]&lt;int,string&gt; list;
}                             </Document>
                       </Project>
                   </Workspace>, host:=host, renameTo:="X")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/4904")>
        Public Sub RenameWrongArity2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
class [|List|]&lt;T&gt;
{
}
 
class C
{
    [|$$List|]&lt;int,string&gt; list;
}                             </Document>
                       </Project>
                   </Workspace>, host:=host, renameTo:="X")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/4904")>
        Public Sub RenameNotCreatable1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
abstract class [|A|]
{
}
 
class C
{
    void M()
    {
        var v = new [|$$A|]();
    }
}                             </Document>
                       </Project>
                   </Workspace>, host:=host, renameTo:="X")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/4904")>
        Public Sub RenameNotCreatable2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
abstract class [|$$A|]
{
}
 
class C
{
    void M()
    {
        var v = new [|A|]();
    }
}                             </Document>
                       </Project>
                   </Workspace>, host:=host, renameTo:="X")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/4904")>
        Public Sub RenameInvocable1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
class C
{
    void M()
    {
        var [|$$v|] = "";
        {|unresolved:v|}();
    }
}                             </Document>
                       </Project>
                   </Workspace>, host:=host, renameTo:="X")

                result.AssertLabeledSpansAre("unresolved", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/4904")>
        Public Sub RenameInvocable2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
class C
{
    void M()
    {
        var [|v|] = "";
        {|unresolved:$$v|}();
    }
}                             </Document>
                       </Project>
                   </Workspace>, host:=host, renameTo:="X")

                result.AssertLabeledSpansAre("unresolved", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/4904")>
        Public Sub RenameStaticInstance1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
class C
{
    static void M()
    {
        var v = [|$$N|];
    }

    int [|N|];
}                             </Document>
                       </Project>
                   </Workspace>, host:=host, renameTo:="X")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/4904")>
        Public Sub RenameStaticInstance2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
class C
{
    static void M()
    {
        var v = [|N|];
    }

    int [|$$N|];
}                             </Document>
                       </Project>
                   </Workspace>, host:=host, renameTo:="X")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/813409")>
        Public Sub RenameTypeDoesNotRenameGeneratedConstructorCalls(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
struct [|$$Type|]
{
    [|Type|](int x) : this()
    { }
}
                            </Document>
                       </Project>
                   </Workspace>, host:=host, renameTo:="U")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/856078")>
        Public Sub UnmodifiedDocumentsAreNotCheckedOutBySourceControl(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document FilePath="Test1.cs">
class C
{
    int [|$$abc|] = 5;
}
                            </Document>
                            <Document FilePath="Test2.cs">
class C2
{
    int abc = 5;
}
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="def")

                Dim originalDocument = result.ConflictResolution.OldSolution.Projects.First().Documents.Where(Function(d) d.Name = "Test2.cs").Single()
                Dim newDocument = result.ConflictResolution.NewSolution.Projects.First().Documents.Where(Function(d) d.Name = "Test2.cs").Single()
                Assert.Same(originalDocument.State, newDocument.State)
                Assert.Equal(1, result.ConflictResolution.NewSolution.GetChangedDocuments(result.ConflictResolution.OldSolution).Count)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773400")>
        Public Sub ReferenceConflictInsideDelegateLocalNotRenamed(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    public delegate void Goo(int x);
    public void GooMeth(int x)
    {
 
    }
    public void Sub()
    {
        Goo {|DeclConflict:x|} = new Goo(GooMeth);
        int [|$$z|] = 1; // Rename z to x
        x({|unresolve2:z|});
    }
}
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="x")

                result.AssertLabeledSpansAre("DeclConflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("unresolve2", "x", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773673")>
        Public Sub RenameMoreThanOneTokenInUnResolvedStatement(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class {|DeclConflict:C|}
{
    public void Sub()
    {
        {|unresolve1:D|} d = new {|unresolve2:D|}();
    }
}
class {|unresolve3:$$D|} // Rename to C
{
 
}
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="C")

                result.AssertLabeledSpansAre("DeclConflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("unresolve1", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("unresolve2", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("unresolve3", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameInvocationExpressionAcrossProjects(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test1.cs">
                            public class [|$$ClassA|]
                            {
                                public static void  Bar()
                                {
                                }
                            }
                        </Document>
                    </Project>
                    <Project Language="C#" CommonReferences="true">
                        <ProjectReference>CSharpAssembly</ProjectReference>
                        <Document FilePath="Test2.cs">
                            public class B
                            {
                                void goo()
                                {
                                    [|ClassA|].Bar();
                                }
                            }
                         </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="A")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameInvocationExpressionAcrossProjectsWithPartialClass(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test1.cs">
                            public partial class [|$$ClassA|]
                            {
                                public static void  Bar()
                                {
                                }
                            }

                            public partial class [|ClassA|]
                            {
                                
                            }
                        </Document>
                    </Project>
                    <Project Language="C#" CommonReferences="true">
                        <ProjectReference>CSharpAssembly</ProjectReference>
                        <Document FilePath="Test2.cs">
                            public class B
                            {
                                void goo()
                                {
                                    [|ClassA|].Bar();
                                }
                            }
                         </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="A")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameInvocationExpressionAcrossProjectsWithConflictResolve(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test1.cs">
                            namespace X
                            {
                                public class [|$$ClassA|]
                                {
                                    public {|resolve:A|}.B B;
                                    public static void Bar()
                                    {
                                    }
                                }
                            }

                            namespace A
                            {
                                public class B { }
                            }
                        </Document>
                    </Project>
                    <Project Language="C#" CommonReferences="true">
                        <ProjectReference>CSharpAssembly</ProjectReference>
                        <Document FilePath="Test2.cs">
                            using X;

                            public class B
                            {
                                void goo()
                                {
                                    {|resolve2:ClassA|}.Bar();
                                }
                            }
                         </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="A")

                result.AssertLabeledSpansAre("resolve", "global::A", RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansAre("resolve2", "X.A.Bar();", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RollBackExpandedConflicts_WithinInvocationExpression(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                            <Workspace>
                                <Project Language="C#" CommonReferences="true">
                                    <Document>
                                        using System;
                                        using System.Collections.Generic;
                                        using System.Linq;
                                        using System.Threading.Tasks;
 
                                        class Program
                                        {
                                            static void Main(string[] {|conflict1:a|})
                                            {
                                                var [|$$b|] = Convert.{|conflict3:ToDouble|}({|conflict2:a|}[0]);
                                            }
                                        }

                                    </Document>
                                </Project>
                            </Workspace>, host:=host, renameTo:="a")

                result.AssertLabeledSpansAre("conflict1", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("conflict2", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("conflict3", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RollBackExpandedConflicts_WithinQuery(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                            <Workspace>
                                <Project Language="Visual Basic" CommonReferences="true">
                                    <Document>
                                        Class C
                                            Dim [|$$x|] As String() ' rename x to y
                                            Dim {|conflict1:y|} = From x In {|conflict2:x|} Select x 
                                        End Class
                                    </Document>
                                </Project>
                            </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("conflict1", "y", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("conflict2", "y", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameOverloadCSharp(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameOverloads:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                            <Workspace>
                                <Project Language="C#" CommonReferences="true">
                                    <Document>
                                        using System;
                                        using System.Collections.Generic;
                                        using System.Linq;
                                        using System.Threading.Tasks;

                                        class Program
                                        {
                                            public void [|$$goo|]()
                                            {
                                                [|goo|]();
                                            }

                                            public void [|goo|]&lt;T&gt;()
                                            {
                                                [|goo|]&lt;T&gt;();
                                            }

                                            public void [|goo|](int i)
                                            {
                                                [|goo|](i);
                                            }
                                        }
                                    </Document>
                                </Project>
                            </Workspace>, host:=host, renameTo:="BarBaz", renameOptions:=renameOptions)

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameOverloadVisualBasic(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameOverloads:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                            <Workspace>
                                <Project Language="Visual Basic" CommonReferences="true">
                                    <Document>
                                        Imports System
                                        Imports System.Collections.Generic
                                        Imports System.Linq

                                        Public Class Program
                                            Sub Main(args As String())

                                            End Sub

                                            Public Sub [|$$goo|]()
                                                [|goo|]()
                                            End Sub

                                            Public Sub [|goo|](Of T)()
                                                [|goo|](Of T)()
                                            End Sub

                                            Public Sub [|goo|](s As String)
                                                [|goo|](s)
                                            End Sub

                                            Public Shared Sub [|goo|](d As Double)
                                                [|goo|](d)
                                            End Sub
                                        End Class
                                    </Document>
                                </Project>
                            </Workspace>, host:=host, renameTo:="BarBaz", renameOptions:=renameOptions)

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/761929"), CombinatorialData>
        Public Sub ConflictCheckInInvocationCSharp(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                        using System;
                        using System.Collections.Generic;
                        using System.Linq;
                        using System.Text;
                        using System.Threading.Tasks;

                        namespace ConsoleApplication5
                        {
                            class [|$$Program2|]
                            {
                                private static Func&lt;[|Program2|], bool> func = null;

                                private static void Main23(object v)
                                {
                                    func(new [|Program2|]());
                                }

                            }
                        }
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/761922"), CombinatorialData>
        Public Sub ConflictCheckInInvocationVisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                        Imports System
                        Imports System.Collections.Generic
                        Imports System.Linq

                        Namespace ns
                            Class [|Program2|]
                                Private Shared func As Func(Of [|Program2|], Boolean) = Nothing
                                Private Shared Sub Main2343(args As String())
                                    func(New [|$$Program2|]())
                                End Sub
                            End Class
                        End Namespace
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameType(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>

                            class [|$$Goo|]
                            {
                                void Blah()
                                {
                                    {|stmt1:Goo|} f = new {|stmt1:Goo|}();
                                }
                            }

                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameTypeAcrossFiles(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class [|$$Goo|]
                            {
                                void Blah()
                                {
                                    {|stmt1:Goo|} f = new {|stmt1:Goo|}();
                                }
                            }
                        </Document>
                        <Document>
                            class FogBar
                            {
                                void Blah()
                                {
                                    {|stmt2:Goo|} f = new {|stmt2:Goo|}();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameTypeAcrossFiles_WithoutCommonReferences(host As RenameTestHost)
            ' without a reference to mscorlib, compiler can't find types like System.Void.  This causes it to have
            ' overload resolution errors for `new Goo();` which causes rename to not update the constructor calls.  This
            ' should not normally ever hit a realistic user scenario.  The test exists just to document our behavior
            ' here.
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#">
                        <Document>
                            class [|$$Goo|]
                            {
                                void Blah()
                                {
                                    {|stmt1:Goo|} f = new {|conflict:Goo|}();
                                }
                            }
                        </Document>
                        <Document>
                            class FogBar
                            {
                                void Blah()
                                {
                                    {|stmt2:Goo|} f = new {|conflict:Goo|}();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameTypeAcrossProjectsAndLanguages(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document>
                            namespace N
                            {
                                 public class [|$$Goo|]
                                 {
                                     void Blah()
                                     {
                                         {|csstmt1:Goo|} f = new {|csstmt1:Goo|}();
                                     }
                                 }
                            }
                         </Document>
                    </Project>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <ProjectReference>CSharpAssembly</ProjectReference>
                        <Document>
                            Imports N
                            Class FogBar
                                Sub Blah()
                                   Dim f = new {|vbstmt1:Goo|}()
                                End Sub
                            End Class
                         </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("csstmt1", "BarBaz", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("vbstmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCSharpTypeFromConstructorDefinition(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class [|Goo|]
                            {
                                [|$$Goo|]()
                                {
                                }
                            
                                void Blah()
                                {
                                    {|stmt1:Goo|} f = new {|stmt1:Goo|}();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCSharpTypeFromConstructorUse(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class [|Goo|]
                            {
                                [|Goo|]()
                                {
                                }
                            
                                void Blah()
                                {
                                    {|stmt1:Goo|} f = new {|stmt1:$$Goo|}();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("stmt1", replacement:="Bar", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCSharpTypeFromDestructor(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class [|Goo|]
                            {
                                ~[|$$Goo|]()
                                {
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCSharpTypeFromSynthesizedConstructorUse(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                               class [|Goo|]
                               {
                                   void Blah()
                                   {
                                       {|stmt1:Goo|} f = new {|stmt1:$$Goo|}();
                                   }
                               }
                           </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("stmt1", replacement:="Bar", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCSharpPredefinedTypeVariables1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                    using System;

                                    class Program
                                    {
                                        static void Main(string[] args)
                                        {
                                            System.Int32 {|stmt1:$$goofoo|} = 23;
                                        }
                                    }
                                </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCSharpPredefinedTypeVariables2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                    using System;

                                    class Program
                                    {
                                        static void Main(string[] args)
                                        {
                                            Int32 {|stmt1:$$goofoo|} = 23;
                                        }
                                    }
                                </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCSharpPredefinedTypeVariables3(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                    using System;

                                    class Program
                                    {
                                        static void Main(string[] args)
                                        {
                                            int {|stmt1:$$fogbar|} = 45;
                                        }
                                    }
                                </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameVisualBasicPredefinedTypeVariables1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Module Program
                                Sub Main(args As String())
                                    Dim {|stmt1:$$a|} As System.Int32 = 1
                                End Sub
                            End Module
                                </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameVisualBasicPredefinedTypeVariables2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Module Program
                                Sub Main(args As String())
                                    Dim {|stmt1:$$a|} As Int32 = 1
                                End Sub
                            End Module
                                </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameVisualBasicPredefinedTypeVariables3(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Module Program
                                Sub Main(args As String())
                                    Dim {|stmt1:$$a|} As Integer = 1
                                End Sub
                            End Module
                                </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539801")>
        Public Sub RenameCSharpEnumMemberToContainingEnumName(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                enum Test
                                {
                                    [|$$FogBar|],
                                };
                           </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Test")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539801")>
        <WorkItem("https://github.com/dotnet/roslyn/pull/5886")>
        Public Sub RenameCSharpEnumToEnumMemberName(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            enum [|$$FogBar|]
                            {
                                TestEnum,
                            };
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="TestEnum")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameVisualBasicTypeFromConstructorUse(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class [|Goo|]
                                Public Sub New()
                                End Sub

                                Public Sub Blah()
                                    Dim x = New {|stmt1:$$Goo|}()
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("stmt1", "Bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameVisualBasicTypeFromSynthesizedConstructorUse(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                           Class [|Goo|]
                               Public Sub Blah()
                                   Dim x = New {|stmt1:$$Goo|}()
                               End Sub
                           End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("stmt1", "Bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameNamespace(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Namespace [|$$GooNamespace|]
                            End Namespace
                        </Document>
                        <Document>
                            namespace [|GooNamespace|] { }
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBazNamespace")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv_Projects/Roslyn/_workitems/edit/6874")>
        <CombinatorialData>
        Public Sub RenameVisualBasicEnum(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                           Enum [|$$Test|]
                               One
                               Two
                           End Enum
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv_Projects/Roslyn/_workitems/edit/6874")>
        <CombinatorialData>
        Public Sub RenameVisualBasicEnumMember(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                           Enum Test
                               [|$$One|]
                               Two
                           End Enum
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539525")>
        <CombinatorialData>
        Public Sub DoNothingRename(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                           class Goo
                           {
                               void [|$$Blah|]()
                               {
                               }
                           }
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Blah")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553631")>
        <CombinatorialData>
        Public Sub CSharpBugfix553631(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Program
{
    void Main(string[] args)
    {
        string {|stmt1:$$s|} = Goo&lt;string, string&gt;();
    }
 
    string Goo&lt;T, S&gt;()
    {
        Return Goo &lt;T, S&gt;();
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Blah")

                result.AssertLabeledSpansAre("stmt1", replacement:="Blah", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553631")>
        <CombinatorialData>
        Public Sub VisualBasicBugfix553631(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Main(args As String())

    End Sub

    Public Sub Bar()
        Dim {|stmt1:$$e|} As String = Goo(Of String)()
    End Sub

    Public Function Goo(Of A)() As String
    End Function
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Blah")

                result.AssertLabeledSpansAre("stmt1", replacement:="Blah", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541697")>
        <CombinatorialData>
        Public Sub RenameExtensionMethod(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            public static class C
                            {
                                public static void [|$$Goo|](this string s) { }
                            }
                            
                            class Program
                            {
                                static void Main()
                                {
                                    "".{|stmt1:Goo|}();
                                }
                            }
                       </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542202")>
        <CombinatorialData>
        Public Sub RenameCSharpIndexerNamedArgument1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C
                            {
                                int this[int x = 5, int [|y|] = 7] { get { return 0; } set { } }
 
                                void Goo()
                                {
                                    var y = this[{|stmt1:$$y|}: 1];
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542106")>
        <CombinatorialData>
        Public Sub RenameCSharpIndexerNamedArgument2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C
                            {
                                int this[int [|$$x|]] { set { } }

                                void Goo()
                                {
                                    this[{|stmt1:x|}: 1] = 0;
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541928")>
        <CombinatorialData>
        Public Sub RenameRangeVariable(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Linq;
                            class C
                            {
                                static void Main(string[] args)
                                {
                                    var temp = from x in "abc"
                                               let {|stmt1:y|} = x.ToString()
                                               select {|stmt1:$$y|} into w
                                               select w;
                                }
                            }
                       </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542106")>
        <CombinatorialData>
        Public Sub RenameVisualBasicParameterizedPropertyNamedArgument(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class C
                                Default Property Item([|$$x|] As Integer) As Integer
                                    Get
                                        Return 42
                                    End Get
                                    Set
                                    End Set
                                End Property

                                Sub Goo()
                                    Me({|stmt1:x|}:=1) = 0
                                End Sub
                            End Class
                       </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543340")>
        <CombinatorialData>
        Public Sub RenameIndexerParameterFromDeclaration(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Program
{
    int this[int [|$$x|], int y]
    {
        get { return {|stmt1:x|} + y; }
        set { value = {|stmt2:x|} + y; }
    }
}                       </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543340")>
        <CombinatorialData>
        Public Sub RenameIndexerParameterFromUseInsideGetAccessor(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Program
{
    int this[int [|x|], int y]
    {
        get { return {|stmt1:$$x|} + y; }
        set { value = {|stmt2:x|} + y; }
    }
}                       </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543340")>
        <CombinatorialData>
        Public Sub RenameIndexerParameterFromUseInsideSetAccessor(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Program
{
    int this[int [|x|], int y]
    {
        get { return {|stmt1:x|} + y; }
        set { value = {|stmt2:$$x|} + y; }
    }
}                       </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542492")>
        <CombinatorialData>
        Public Sub RenamePartialMethodParameter(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>

using System;

public partial class Test
{
    partial void Goo(object [|$$o|])
    {
    }
}

partial class Test
{
    partial void Goo(object [|o|]);
}

                       </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542492")>
        <CombinatorialData>
        Public Sub RenameExtendedPartialMethodParameter(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>

using System;

public partial class Test
{
    public partial void Goo(object [|$$o|])
    {
    }
}

partial class Test
{
    public partial void Goo(object [|o|]);
}

                       </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528820")>
        Public Sub RenameVisualBasicAnonymousKey(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Option Infer On
                            Imports System
                            Module Program
                                Sub Main(args As String())
                                    Dim namedCust = New With {.Name = "Blue Yonder Airlines",
                                                              .[|$$City|] = "Snoqualmie"}

                                End Sub
                            End Module
                           </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="y")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542543")>
        <CombinatorialData>
        Public Sub RenameIncludesPreviouslyInvalidReference(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>

                            using System;
                            class Program
                            {
                                int [|$$y|] = 2;
                                static void Main(string[] args)
                                {
                                    {|stmt1:x|} = 1;
                                }
                            }

                       </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="x")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543027")>
        <CombinatorialData>
        Public Sub RenameVariableInQueryAsUsingStatement(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;
using System.Linq;

class Program
{
    public static void Main(string[] args)
    {
        MyManagedType {|stmt1:mnObj1|} = new MyManagedType();
        using ({|stmt2:m$$nObj1|})
        {
        }
    }
}

class MyManagedType : System.IDisposable
{
    public void Dispose()
    {
    }
}
                       </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", replacement:="y", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", replacement:="y", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543169")>
        <CombinatorialData>
        Public Sub LambdaWithOutParameter(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;
using System.Linq;

class D
{
    static void Main(string[] args)
    {
        string[] str = new string[] { };
        var s = str.Where(out {|stmt1:$$x|} =>
        {
            return {|stmt1:x|} == "1";
        });
    }
}
                       </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", replacement:="y", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543567")>
        <CombinatorialData>
        Public Sub CascadeBetweenOverridingProperties(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="VisualBasicAssembly" CommonReferences="true">
                        <Document FilePath="Test.vb">
Module Program
    Sub Main(args As String())
    End Sub
End Module
 
Class C
    Public Overridable ReadOnly Property [|$$Total|]() As Double
        Get
            Return 42
        End Get
    End Property
End Class
 
Class M
    Inherits C
 
    Public Overrides ReadOnly Property [|Total|]() As Double
        Get
            Return MyBase.{|stmt1:Total|} * rate
        End Get
    End Property
End Class
                       </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Goo")

                result.AssertLabeledSpansAre("stmt1", "Goo", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529799")>
        Public Sub CascadeBetweenImplementedInterfaceEvent(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="VisualBasicAssembly" CommonReferences="true">
                        <Document FilePath="Test.vb">
Class C
    Event E([|$$x|] As Integer)

    Sub Goo()
        RaiseEvent E({|stmt1:x|}:=1)
    End Sub
End Class

                       </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", "y", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543567")>
        <CombinatorialData>
        Public Sub CascadeBetweenEventParameters(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;

interface IGoo
{
    event EventHandler [|$$Goo|];
}

class Bar : IGoo
{
    public event EventHandler [|Goo|];
}

                       </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531260")>
        Public Sub DoNotCascadeToMetadataSymbols(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Public Class C
                                Implements System.ICloneable
                                Public Function [|$$Clone|]() As Object Implements System.ICloneable.Clone
                                    Throw New System.NotImplementedException()
                                End Function
                            End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="CloneImpl")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545473")>
        Public Sub RenamePartialTypeParameter_CSharp1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
partial class Class1
{
    partial void goo<$$[|T|]>([|T|] x);
    partial void goo<[|T|]>([|T|] x)
    {
    }
}]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545473")>
        Public Sub RenamePartialTypeParameter_CSharp2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
partial class Class1
{
    partial void goo<[|T|]>([|T|] x);
    partial void goo<$$[|T|]>([|T|] x)
    {
    }
}]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545472")>
        Public Sub RenamePartialTypeParameter_VB1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Public Module Module1
    Partial Private Sub Goo(Of [|$$T|] As Class)(x as [|T|])
    End Sub
    Private Sub goo(Of [|T|] As Class)(x as [|T|])
    End Sub
End Module
]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545472")>
        Public Sub RenamePartialTypeParameter_VB2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Public Module Module1
    Partial Private Sub Goo(Of [|T|] As Class)(x as [|T|])
    End Sub
    Private Sub goo(Of [|$$T|] As Class)(x as [|T|])
    End Sub
End Module
]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529163")>
        Public Sub AmbiguousBeforeRenameHandledCorrectly_Bug11516(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                                Namespace NS    
                                Module M    
                                End Module
                                End Namespace 

                                Namespace NS.[|$$M|]
                                End Namespace
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="N")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529163")>
        Public Sub AmbiguousBeforeRenameHandledCorrectly_Bug11516_2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            namespace Goo
                            {
                                class B
                                { }
                            }

                            namespace Goo.[|$$B|]
                            { }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="N")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554092")>
        Public Sub RenamePartialMethods_1_CS(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            partial class Goo
                            {
                                partial void [|F|]();
                            }
                            partial class Goo
                            {
                                partial void [|$$F|]()
                                {
                                    throw new System.Exception("F");
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554092")>
        Public Sub RenameExtendedPartialMethods_1_CS(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            partial class Goo
                            {
                                public partial void [|F|]();
                            }
                            partial class Goo
                            {
                                public partial void [|$$F|]()
                                {
                                    throw new System.Exception("F");
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554092")>
        Public Sub RenamePartialMethods_2_CS(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            partial class Goo
                            {
                                partial void [|$$F|]();
                            }
                            partial class Goo
                            {
                                partial void [|F|]()
                                {
                                    throw new System.Exception("F");
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554092")>
        Public Sub RenameExtendedPartialMethods_2_CS(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            partial class Goo
                            {
                                public partial void [|$$F|]();
                            }
                            partial class Goo
                            {
                                public partial void [|F|]()
                                {
                                    throw new System.Exception("F");
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554092")>
        Public Sub RenamePartialMethods_1_VB(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            partial class Goo
                                partial private sub [|F|]()
                            end class
                            partial class Goo
                                private sub [|$$F|]()
                                    throw new System.Exception("F")
                                end sub
                            end class
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554092")>
        Public Sub RenamePartialMethods_2_VB(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            partial class Goo
                                partial private sub [|$$F|]()
                            end class
                            partial class Goo
                                private sub [|F|]()
                                    throw new System.Exception("F")
                                end sub
                            end class
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530740")>
        Public Sub Bug530740(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System;
                            using MyInt = System.[|Int32|];
 
                            namespace System
                            {
                                public struct [|$$Int32|]
                                {
                                    public static implicit operator MyInt(int x)
                                    {
                                        return default(MyInt);
                                    }
                                }
                            }
 
                            class A
                            {
                                static void Goo(int x) { Console.WriteLine("int"); }
                                static void Goo(MyInt x) { Console.WriteLine("MyInt"); }
                                static void Main()
                                {
                                    Goo((MyInt)0);
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="MyInt")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530082")>
        Public Sub RenameMethodThatImplementsInterfaceMethod(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports System

                            Interface I
                                Sub Goo(Optional x As Integer = 0)
                            End Interface

                            Class C
                                Implements I

                                Shared Sub Main()
                                    DirectCast(New C(), I).Goo()
                                End Sub

                                Private Sub [|$$I_Goo|](Optional x As Integer = 0) Implements I.Goo
                                    Console.WriteLine("test")
                                End Sub
                            End Class
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Goo")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529874")>
        Public Sub DoNotRemoveAttributeSuffixOn__Attribute(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports System

                            &lt;[|＿Attribute2|]&gt; ' full width _!
                            Class [|$$＿Attribute2|]
                                Inherits Attribute
                            End Class

                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="＿Attribute")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553315")>
        Public Sub RenameEventParameterOnUsage(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class C
                            Event E([|x|] As Integer)
 
                            Sub Goo()
                                RaiseEvent E({|stmt1:$$x|}:=1)
                            End Sub
                        End Class
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="bar")

                result.AssertLabeledSpansAre("stmt1", "bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529819")>
        Public Sub RenameCompilerGeneratedBackingFieldForNonCustomEvent(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class B
                                Event [|$$X|]() ' Rename X to Y
                                Sub Goo()
                                    {|stmt1:XEvent|}()
                                End Sub
                            End Class
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Y")

                result.AssertLabeledSpecialSpansAre("stmt1", "YEvent", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576607")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529819")>
        <CombinatorialData>
        Public Sub RenameCompilerGeneratedEventHandlerForNonCustomEvent(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class B
                                Event [|$$X|]() ' Rename X to Y
                                Sub Goo()
                                    Dim y = New B.{|stmt1:XEventHandler|}(AddressOf bar)
                                End Sub

                                Shared Sub bar()
                                End Sub
                            End Class
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Y")

                result.AssertLabeledSpecialSpansAre("stmt1", "YEventHandler", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576966")>
        <CombinatorialData>
        Public Sub CSharpRenameParenthesizedFunctionName(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                        class C
                        {
                            void [|$$Goo|]()
                            {
                                (({|stmt1:Goo|}))();
                            }
                        }
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("stmt1", "Bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/601123")>
        <CombinatorialData>
        Public Sub VisualBasicAwaitAsIdentifierInAsyncShouldBeEscaped(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports System
                            Module M
                                Async Function methodIt() As Task(Of String)
                                    Dim a As Func(Of String) = AddressOf {|stmt1:Goo|}
                                End Function

                                Function [|$$Goo|]() As String
                                End Function
                            End Module
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Await")

                result.AssertLabeledSpecialSpansAre("stmt1", "[Await]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/601123")>
        <CombinatorialData>
        Public Sub CSharpAwaitAsIdentifierInAsyncMethodShouldBeEscaped0(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            <![CDATA[
using System;
using System.Threading.Tasks;

class M
{
    async public Task<string> methodIt()
    {
        Func<string> a = {|stmt1:Goo|};
        return null;
    }

    public static string [|$$Goo|]()
    {
        return "";
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="await")

                result.AssertLabeledSpansAre("stmt1", "@await", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/601123")>
        <CombinatorialData>
        Public Sub CSharpAwaitAsIdentifierInAsyncLambdaShouldBeEscaped(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            <![CDATA[
using System;
using System.Threading.Tasks;
class s
{
    public void abc()
    {
        Func<Task> sin = async () => { {|stmt1:Goo|}(); };
    }

    public int [|$$Goo|]()
    {
        return 0;
    }

}
]]>
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="await")

                result.AssertLabeledSpansAre("stmt1", "@await", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/601123")>
        <CombinatorialData>
        Public Sub CSharpAwaitAsIdentifierInAsyncLambdaShouldBeEscaped1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            <![CDATA[
using System;
using System.Threading.Tasks;
class s
{
    public void abc()
    {
        Func<int, Task<int>> sink2 = (async c => { return {|stmt2:Goo|}(); });
    }

    public int [|$$Goo|]()
    {
        return 0;
    }

}
]]>
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="await")

                result.AssertLabeledSpansAre("stmt2", "@await", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/601123")>
        <CombinatorialData>
        Public Sub CSharpAwaitAsIdentifierInAsyncLambdaShouldNotBeEscaped(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            <![CDATA[
using System;
using System.Threading.Tasks;
class s
{
    public void abc()
    {
        Func<int, int> sink4 = ((c) => { return {|stmt3:Goo|}(); });
    }

    public int [|$$Goo|]()
    {
        return 0;
    }

}
]]>
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="await")

                result.AssertLabeledSpansAre("stmt3", "await", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub VBRoundTripMissingModuleName_1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                        Imports N

                        Module Program
                            Sub Main()
                                Dim mybar As N.{|stmt1:Goo|} = New {|stmt1:Goo|}()
                            End Sub
                        End Module

                        Namespace N
                            Module X
                                Class [|$$Goo|]
                                End Class
                            End Module

                        End Namespace
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("stmt1", "Bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub VBRoundTripMissingModuleName_2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                        Imports N

                        Module Program
                            Sub Main()
                                N.{|stmt1:Goo|}()
                            End Sub
                        End Module

                        Namespace N
                            Module X
                                Sub [|$$Goo|]()
                                End Sub
                            End Module

                        End Namespace
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("stmt1", "Bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603767")>
        Public Sub Bug603767_RenamePartialAttributeDeclarationWithDifferentCasingAndCSharpUsage(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Imports System
                            
                            Public Class [|XAttribute|]
                                Inherits Attribute
                            End Class
 
                            Partial Public Class [|$$XATTRIBUTE|]
                                Inherits Attribute
                            End Class
                            </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
                            [{|resolved:X|}]
                            class C { }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="XATTRIBUTe")

                result.AssertLabeledSpansAre("resolved", "XATTRIBUTe", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603371")>
        Public Sub Bug603371_VisualBasicRenameAttributeToGlobal(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Imports System

                            &lt;{|escaped:Goo|}&gt;
                            Class {|escaped:$$Goo|} ' Rename Goo to Global
                                Inherits Attribute
                            End Class
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Global")

                result.AssertLabeledSpecialSpansAre("escaped", "[Global]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603371")>
        Public Sub Bug603371_VisualBasicRenameAttributeToGlobal_2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Imports System

                            &lt;{|escaped:Goo|}&gt;
                            Class {|escaped:$$Goo|} ' Rename Goo to Global
                                Inherits Attribute
                            End Class
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="[global]")

                result.AssertLabeledSpansAre("escaped", "[global]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602494")>
        Public Sub Bug602494_RenameOverrideWithNoOverriddenMember(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            class C1
                            {
                                override void [|$$Goo|]() {}
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576607")>
        Public Sub Bug576607(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
Class B
    Event [|$$X|]()
    Sub Goo()
        Dim y = New {|stmt1:XEventHandler|}(AddressOf bar)
    End Sub

    Shared Sub bar()
    End Sub
End Class
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Baz")

                result.AssertLabeledSpecialSpansAre("stmt1", "BazEventHandler", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        Public Sub Bug529765_VisualBasicOverrideImplicitPropertyAccessor(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Public Class A
                                Public Overridable ReadOnly Property [|$$X|](y As Integer) As Integer
                                    Get
                                        Return 0
                                    End Get
                                End Property
                            End Class

                            Public Class C
                                Inherits A

                                Public Overrides Function {|getaccessor:get_X|}(Y As Integer) As Integer

                                End Function
                            End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Z")

                result.AssertLabeledSpecialSpansAre("getaccessor", "get_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        Public Sub Bug529765_VisualBasicOverrideImplicitPropertyAccessor_2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Public Class A
                                Public Overridable ReadOnly Property [|X|](y As Integer) As Integer
                                    Get
                                        Return 0
                                    End Get
                                End Property
                            End Class

                            Public Class C
                                Inherits A

                                Public Overrides Function {|getaccessor:$$Get_X|}(Y As Integer) As Integer

                                End Function
                            End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Z")

                result.AssertLabeledSpecialSpansAre("getaccessor", "Get_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        Public Sub Bug529765_VisualBasicOverrideImplicitPropertyAccessor_3(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Public Class A
                                Public Overridable ReadOnly Property [|X|](y As Integer) As Integer
                                    Get
                                        Return 0
                                    End Get
                                    
                                    Set
                                    End Set
                                End Property
                            End Class

                            Public Class C
                                Inherits A

                                Public Overrides Function {|getaccessor:$$Get_X|}(Y As Integer) As Integer

                                End Function

                                Public Overrides Sub {|setaccessor:Set_X|}(index as integer, Y As Integer)

                                End Sub

                            End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Z")

                result.AssertLabeledSpecialSpansAre("getaccessor", "Get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessor", "Set_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        Public Sub Bug529765_CrossLanguageOverrideImplicitPropertyAccessor_1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Public Class A
                                Public Overridable Property [|$$X|](y As Integer) As Integer
                                    Get
                                        Return 0
                                    End Get

                                    Set
                                    End Set
                                End Property

                                Public Shared Sub Main()
                                End Sub
                            End Class
                            </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
                            class B : A
                            {
                                public override int {|getaccessor:get_X|}(int y)
                                {
                                }

                                public override void {|setaccessor:set_X|}(int y, int value)
                                {
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Z")

                result.AssertLabeledSpecialSpansAre("getaccessor", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessor", "set_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        Public Sub Bug529765_CrossLanguageOverrideImplicitPropertyAccessor_2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Public Class A
                                Public Overridable Property [|$$X|](y As Integer) As Integer
                                    Get
                                        Return 0
                                    End Get

                                    Set
                                    End Set
                                End Property

                                Public Shared Sub Main()
                                End Sub
                            End Class
                            </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
                            class B : A
                            {
                                public override int {|getaccessor:get_X|}(int y)
                                {
                                    return base.{|getaccessorstmt:get_X|}(y);
                                }

                                public override void {|setaccessor:set_X|}(int y, int value)
                                {
                                    base.{|setaccessorstmt:set_X|}(y, value);
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Z")

                result.AssertLabeledSpecialSpansAre("getaccessor", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessor", "set_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("getaccessorstmt", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessorstmt", "set_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        Public Sub Bug529765_CrossLanguageOverrideImplicitPropertyAccessor_3(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Public Class A
                                Public Overridable Property [|X|](y As Integer) As Integer
                                    Get
                                        Return 0
                                    End Get

                                    Set
                                    End Set
                                End Property

                                Public Shared Sub Main()
                                End Sub
                            End Class
                            </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
                            class B : A
                            {
                                public override int {|getaccessor:get_X|}(int y)
                                {
                                    return base.{|getaccessorstmt:get_X|}(y);
                                }

                                public override void {|setaccessor:set_X|}(int y, int value)
                                {
                                    base.{|setaccessorstmt:$$set_X|}(y, value);
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Z")

                result.AssertLabeledSpecialSpansAre("getaccessor", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessor", "set_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("getaccessorstmt", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessorstmt", "set_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        Public Sub Bug529765_CrossLanguageOverrideImplicitPropertyAccessor_4(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Public Class A
                                Public Overridable Property [|X|](y As Integer) As Integer
                                    Get
                                        Return 0
                                    End Get

                                    Set
                                    End Set
                                End Property

                                Public Shared Sub Main()
                                End Sub
                            End Class

                            Public Class B
                                Inherits A

                                Public Overrides Property [|$$X|](y As Integer) As Integer
                                    Get
                                        Return 0
                                    End Get

                                    Set
                                    End Set
                                End Property

                                Public Shared Sub Main()
                                End Sub
                            End Class

                            </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
                            class B : A
                            {
                                public override int {|getaccessor:get_X|}(int y)
                                {
                                    return base.{|getaccessorstmt:get_X|}(y);
                                }

                                public override void {|setaccessor:set_X|}(int y, int value)
                                {
                                    base.{|setaccessorstmt:set_X|}(y, value);
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Z")

                result.AssertLabeledSpecialSpansAre("getaccessor", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessor", "set_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("getaccessorstmt", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessorstmt", "set_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        Public Sub Bug529765_CrossLanguageOverrideImplicitPropertyAccessor_5(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Public Class A
                                Public Overridable Property [|X|](y As Integer) As Integer
                                    Get
                                        Return 0
                                    End Get

                                    Set
                                    End Set
                                End Property
                            End Class

                            Public Class B
                                Inherits A

                                Public Overrides Property [|X|](y As Integer) As Integer
                                    Get
                                        Return 0
                                    End Get

                                    Set
                                    End Set
                                End Property
                            End Class
                            </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
                            class B : A
                            {
                                public override int {|getaccessor:get_X|}(int y)
                                {
                                    return base.{|getaccessorstmt:get_X|}(y);
                                }

                                public override void {|setaccessor:$$set_X|}(int y, int value)
                                {
                                    base.{|setaccessorstmt:set_X|}(y, value);
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Z")

                result.AssertLabeledSpecialSpansAre("getaccessor", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessor", "set_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("getaccessorstmt", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessorstmt", "set_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/610120")>
        Public Sub Bug610120_CrossLanguageOverrideImplicitPropertyAccessorConflictWithVBProperty(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Public Class A
                                Public Overridable Property {|conflict:X|}({|declconflict:y|} As Integer) As Integer
                                    Get
                                        Return 0
                                    End Get

                                    Set
                                    End Set
                                End Property

                                Public Shared Sub Main()
                                End Sub
                            End Class

                            Public Class B
                                Inherits A

                                Public Overrides Property {|conflict:X|}({|declconflict:y|} As Integer) As Integer
                                    Get
                                        Return 0
                                    End Get

                                    Set
                                    End Set
                                End Property
                            End Class
                            </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
                            class B : A
                            {
                                public override int {|getaccessor:get_X|}(int y)
                                {
                                    return base.{|getaccessorstmt:get_X|}(y);
                                }

                                public override void {|setaccessor:$$set_X|}(int y, int value)
                                {
                                    base.{|setaccessorstmt:set_X|}(y, value);
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("conflict", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("getaccessorstmt", "get_y", RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("setaccessorstmt", "set_y", RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("declconflict", "y", RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpecialSpansAre("setaccessor", "set_y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("getaccessor", "get_y", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/612380")>
        Public Sub Bug612380_CrossLanguageOverrideImplicitPropertyAccessorCascadesToInterface(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Public Class A
                                Public Overridable Property {|conflict:$$X|}({|declconflict:y|} As Integer) As Integer
                                    Get
                                        Return 0
                                    End Get

                                    Set
                                    End Set
                                End Property

                                Public Shared Sub Main()
                                End Sub
                            End Class
                            </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
                            class B : A
                            {
                                public override int {|getaccessor:get_X|}(int y)
                                {
                                    return base.{|getaccessorstmt:get_X|}(y);
                                }
                            }


                            interface I
                            {
                                int {|getaccessor:get_X|}(int y);
                            }

                            class C : B, I
                            {
                                public override int {|getaccessor:get_X|}(int y)
                                {
                                    return base.{|getaccessor:get_X|}(y);
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("conflict", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("declconflict", "y", RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpecialSpansAre("getaccessor", "get_y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("getaccessorstmt", "get_y", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/866094")>
        Public Sub Bug866094(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Public Module A
                                Public Sub [|M|]()
                                End Sub
                            End Module
                        </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
                            class C
                            {
                                static void N()
                                {
                                    A.[|$$M|]();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="X")
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameDynamicallyBoundFunction(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document>
Imports System
class A
{
    class B
    {
        public void [|Boo|](int d) { } 
    }
    void Bar()
    {
        B b = new B();
        dynamic d = 1.5f;
        b.{|Stmt1:$$Boo|}(d); 
    }
}                 
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("Stmt1", "Bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529989")>
        Public Sub CSharp_RenameToIdentifierWithUnicodeEscaping(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document>
public class A
{
    public static void [|$$Main|]()
    {
        A.{|stmt1:Main|}();
    }
}                 
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="M\u0061in")

                result.AssertLabeledSpansAre("stmt1", "M\u0061in", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576966")>
        Public Sub CSharp_RenameParenthesizedMethodNames(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document>
class C
{
    void [|$$Goo|]()
    {
        (({|stmt1:Goo|}))();
    }
}
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("stmt1", "Bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632052")>
        Public Sub CSharp_RenameParameterUsedInObjectInitializer(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document>
using System; 
class Program
{
    void M(int [|$$p|])
    {
        var v1 = new C() { tuple = Tuple.Create({|stmt1:p|}) };
        Tuple&lt;int&gt; v2 = Tuple.Create({|stmt2:p|});
    }
}
class C 
{     
    public Tuple&lt;int&gt; tuple;   
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="n")

                result.AssertLabeledSpansAre("stmt1", "n", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", "n", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624092")>
        Public Sub LocationsIssue(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document>
namespace N
{
    public class D { }
}
                        </Document>
                    </Project>
                    <Project Language="Visual Basic" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
Namespace N
    Module M
        Public Class [|$$C|] 'Rename C to D      

        End Class
    End Module
End Namespace

Module Program
    Sub Main(args As String())
        Dim d = New N.D()
        Dim c = New N.{|resolved:C|}()
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="D")

                result.AssertLabeledSpansAre("resolved", "Dim c = New N.M.D()", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameParam1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using System;
class Program
{
    /// <summary>
    /// <paramref name="[|args|]" />
    /// </summary>
    /// <param name="[|args|]"></param>
    static void Main(string[] [|$$args|])
    {

    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="pargs")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        Public Sub RenameParam1_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class Program
	''' <summary>
	''' <paramref name="[|args|]" />
	''' </summary>
	''' <param name="[|args|]"></param>
	Private Shared Sub Main([|$$args|] As String())
	End Sub
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="pargs")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameParam2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
                                using System;
                                class Program
                                {
                                    /// <summary>
                                    /// <paramref name="{|ResolveWithoutAt:args|}" />
                                    /// </summary>
                                    /// <param name="{|ResolveWithoutAt:$$args|}"></param>
                                    static void Main(string[] {|Resolve:args|})
                                    {

                                    }
                                }
                                ]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="if")

                result.AssertLabeledSpecialSpansAre("Resolve", "@if", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("ResolveWithoutAt", "if", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        Public Sub RenameParam2_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class Program
	''' <summary>
	''' <paramref name="{|Resolve:args|}" />
	''' </summary>
	''' <param name="{|Resolve:$$args|}"></param>
	Private Shared Sub Main({|Resolve:args|} As String())
	End Sub
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="[If]")

                result.AssertLabeledSpansAre("Resolve", <![CDATA[[If]]]>.Value, RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameTypeParam1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
/// <summary>
/// <typeparamref name="[|$$T|]"/>
/// </summary>
/// <typeparam name="[|T|]"></typeparam>
class B<[|T|]>
{
    /// <summary>
    /// <typeparamref name="[|T|]"/>
    /// </summary>
    /// <typeparam name="U"></typeparam>
    /// <typeparam name="P"></typeparam>
    /// <param name="x"></param>
    /// <param name="z"></param>
    [|T|] Goo<U, P>(U x, [|T|] z) { return z; }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        Public Sub RenameTypeParam1_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
''' <summary>
''' <typeparamref name="[|$$T|]"/>
''' </summary>
''' <typeparam name="[|T|]"></typeparam>
Class B(Of [|T|])
    ''' <summary>
    ''' <typeparamref name="[|T|]"/>
    ''' </summary>
    ''' <typeparam name="U"></typeparam>
    ''' <typeparam name="P"></typeparam>
    ''' <param name="x"></param>
    ''' <param name="z"></param>
    Private Function Goo(Of U, P)(x As U, z As [|T|]) As [|T|]
        Return z
    End Function
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624310")>
        Public Sub Bug624310_VBCascadeLambdaParameterInFieldInitializer(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Option Strict On
 
                            Module M
                                Dim field As Object = If(True, Function({|fieldinit:$$y|} As String) {|fieldinit:y|}.ToUpper(), Function({|fieldinit:y|} As String) {|fieldinit:y|}.ToLower())
 
                                Sub Main()
                                    Dim local As Object = If(True, Function(x As String) x.ToUpper(), Function(x As String) x.ToLower())
                                End Sub
                            End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="z")

                result.AssertLabeledSpansAre("fieldinit", "z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624310")>
        Public Sub Bug624310_CSNoCascadeLambdaParameterInFieldInitializer(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document><![CDATA[
using System;
class Program
{
    public object O = true ? (Func<string, string>)((string {|fieldinit:$$x|}) => {return {|fieldinit:x|}.ToUpper(); }) : (Func<string, string>)((string x) => {return x.ToLower(); });
}
]]>]
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="z")

                result.AssertLabeledSpansAre("fieldinit", "z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633582")>
        Public Sub Bug633582_CSDoNotAddParenthesesInExpansionForParenthesizedBinaryExpression(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document><![CDATA[
using System.Linq;

class Program
{
    void aaa()
    {
            var bbb =
                from x in new[] { 1, 2, 3 }
                where ((1 + x > 'a'))
                join {|stmt:$$ddd|} in new[] { 5, 2, 9, 4, } on 7 equals {|stmt:ddd|} + 5 into eee
                select eee;
    }
}
]]>]
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="xxx")

                result.AssertLabeledSpansAre("stmt", "xxx", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory(Skip:="https://github.com/dotnet/roslyn/issues/9412")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/622086")>
        <CombinatorialData>
        Public Sub Bug622086_CSRenameExplicitInterfaceImplementation(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document><![CDATA[
namespace X
{
    interface [|$$IGoo|]
    {
        void Clone();
    }

    class Baz : [|IGoo|]
    {
        void [|IGoo|].Clone()
        {

        }
    }
}
]]>]
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="IGooEx")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/8139")>
        Public Sub RenameExplicitInterfaceImplementationFromDifferentProject(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <ProjectReference>Project2</ProjectReference>
                        <Document>
class Program
{
    static void Main(string[] args)
    {
        Test(new Class1());
    }

    static void Test(IInterface i)
    {
        i.[|M|]();
    }
}
                        </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <Document>
public class Class1 : IInterface
{
    void IInterface.[|$$M|]() { }  // Rename 'M' from here
}

public interface IInterface
{
    void [|M|]();
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Goo")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529803")>
        Public Sub RenamingEventCascadesToCSUsingEventHandlerDelegate(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
Public Interface IA
    Event [|$$X|]() ' Rename X to Y
End Interface

Class C
    Implements IA

    Public Event [|X|] As IA.{|handler:XEventHandler|} Implements IA.[|X|]
End Class
                        </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
using System;

class Program : IA
{
    event IA.{|handler:XEventHandler|} IA.[|X|]
    {
        add
        {
            throw new NotImplementedException();
        }

        remove
        {
            throw new NotImplementedException();
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Y")

                result.AssertLabeledSpecialSpansAre("handler", "YEventHandler", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529803")>
        Public Sub RenamingCompilerGeneratedDelegateTypeForEventCascadesBackToEvent_1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
Public Interface IA
    Event [|X|]() ' Rename X to Y
End Interface

Class C
    Implements IA
    Dim A As IA.{|handler:XEventHandler|}
    Public Event [|$$X|] Implements IA.[|X|]
End Class
                        </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
using System;

class Program : IA
{
    event IA.{|handler:XEventHandler|} IA.[|X|]
    {
        add
        {
            throw new NotImplementedException();
        }

        remove
        {
            throw new NotImplementedException();
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Y")

                result.AssertLabeledSpecialSpansAre("handler", "YEventHandler", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529803")>
        Public Sub RenamingCompilerGeneratedDelegateTypeForEventCascadesBackToEvent_2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
Public Interface IA
    Event [|X|]() ' Rename X to Y
End Interface

Class C
    Implements IA

    Public Event [|X|] As IA.{|handler:XEventHandler|} Implements IA.[|X|]
End Class
                        </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
using System;

class Program : IA
{
    event IA.{|handler:$$XEventHandler|} IA.[|X|]
    {
        add
        {
            throw new NotImplementedException();
        }

        remove
        {
            throw new NotImplementedException();
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Y")

                result.AssertLabeledSpecialSpansAre("handler", "YEventHandler", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/655621")>
        Public Sub RenamingCompilerGeneratedDelegateTypeForEventCascadesBackToEvent_3(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
Public Interface IA
    Event [|$$X|]() ' Rename X to Y
End Interface
                        </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
using System;
class C
{
     object x = new IA.{|handler:XEventHandler|}(() => { });
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Y")

                result.AssertLabeledSpecialSpansAre("handler", "YEventHandler", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/655621")>
        Public Sub RenamingCompilerGeneratedDelegateTypeForEventCascadesBackToEvent_4(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
Public Interface IA
    Event [|X|]() ' Rename X to Y
End Interface
                        </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
using System;
class C
{
     object x = new IA.{|handler:$$XEventHandler|}(() => { });
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Y")

                result.AssertLabeledSpecialSpansAre("handler", "YEventHandler", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/655621"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/762094")>
        <WpfTheory, CombinatorialData>
        Public Sub RenamingCompilerGeneratedDelegateTypeForEventCascadesBackToEvent_5(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
Public Interface IA
    Event [|X|]() ' Rename X to Y
End Interface
                        </Document>
                    </Project>
                    <Project Language="Visual Basic" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
Imports System
Class C
     public x as new IA.{|handler:$$XEventHandler|}( Sub() Console.WriteLine() )
Class C
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Y")

                result.AssertLabeledSpecialSpansAre("handler", "YEventHandler", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        Public Sub Bug622086_CSRenameVarNotSupported(host As RenameTestHost)
            Dim result = RenameEngineResult.Create(_outputHelper,
<Workspace>
    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
        <Document><![CDATA[
using System;

namespace X
{
    class Baz
    {
        void M()
        {
            [|$$var|] x = new Int32();
        }
    }
}
]]>]
                        </Document>
    </Project>
</Workspace>, host:=host, renameTo:="Int32", expectFailure:=True)
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        Public Sub Bug622086_CSRenameCustomTypeNamedVarSupported(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document><![CDATA[
using System;

namespace X
{
    class [|var|]
    {}

    class Baz
    {
        void M()
        {
            {|stmt:$$var|} x = new {|stmt:var|}();;
        }
    }
}
]]>]
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="bar")

                result.AssertLabeledSpansAre("stmt", "bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        Public Sub Bug622086_CSRenameDynamicNotSupported(host As RenameTestHost)
            Dim result = RenameEngineResult.Create(_outputHelper,
<Workspace>
    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
        <Document><![CDATA[
using System;

namespace X
{
    class Baz
    {
        void M()
        {
            [|$$dynamic|] x = new Int32();
        }
    }
}
]]>]
                        </Document>
    </Project>
</Workspace>, host:=host, renameTo:="Int32", expectFailure:=True)
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        Public Sub Bug622086_CSRenameToVarNotSupportedNoConflictsWithOtherVar(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document><![CDATA[
class {|Conflict:Program|}
{
    static void Main(string[] args)
    {
        {|Conflict:$$Program|} x = null;
        var y = 23;
    }
}
]]>]
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="var")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        Public Sub Bug622086_CSRenameToDynamicNotSupported(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document><![CDATA[
class {|Conflict:Program|}
{
    static void Main(string[] args)
    {
        {|Conflict:$$Program|} x = null;
    }
}
]]>]
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="dynamic")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        Public Sub Bug622086_CSRenameToDynamicNotSupported2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document><![CDATA[
class {|Conflict:Program|}
{
    static void Main(string[] args)
    {
        {|Conflict:$$Program|} x = null;
        dynamic y = 23;
    }
}
]]>]
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="dynamic")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        Public Sub Bug622086_CSRenameToDynamicSupportedButConflictsWithOtherDynamic_2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document><![CDATA[
class {|Conflict:Program|}
{
    static void Main(string[] args)
    {
        {|Conflict:$$Program|} x = null;
        dynamic y = new {|Conflict:Program|}();
    }
}
]]>]
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="dynamic")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608988")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608989")>
        Public Sub RenameNamespaceInVbFromCSharReference(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
Namespace [|N|]
    Public Class X
    End Class
End Namespace
                        </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
class Y : [|$$N|].X { }
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Goo")

            End Using
        End Sub

#Region "Cref"

        <Theory, CombinatorialData>
        Public Sub RenameTypeFromCref(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test.cs">
                            <![CDATA[
class [|Program|]
{
    ///  <see cref="[|$$Program|]"/> to start the program.
    static void Main(string[] args)
    {
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="y")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        Public Sub RenameTypeFromCref_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="VisualBasicAssembly" CommonReferences="true">
                        <Document>
                            <![CDATA[
Class [|Program|]
	'''  <see cref="[|$$Program|]"/> to start the program.
	Private Shared Sub Main(args As String())
	End Sub
End Class
]]>
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Y")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameMemberFromCref(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test.cs">
                            <![CDATA[
class Program
{
    ///  <see cref="Program.[|$$Main|]"/> to start the program.
    static void [|Main|](string[] args)
    {
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="y")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        Public Sub RenameMemberFromCref_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="VisualBasicAssembly" CommonReferences="true">
                        <Document>
                            <![CDATA[
Class Program
	'''  <see cref="Program.[|$$Main|]"/> to start the program.
	Private Shared Sub [|Main|](args As String())
	End Sub
End Class
]]>
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Y")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCrefFromMember(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test.cs">
                            <![CDATA[
class Program
{
    ///  <see cref="Program.[|Main|]"/> to start the program.
    static void [|$$Main|](string[] args)
    {
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="y")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        Public Sub RenameCrefFromMember_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="VisualBasicAssembly" CommonReferences="true">
                        <Document>
                            <![CDATA[
Class Program
	'''  <see cref="Program.[|Main|]"/> to start the program.
	Private Shared Sub [|$$Main|](args As String())
	End Sub
End Class
]]>
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="y")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546952")>
        Public Sub RenameIncludingCrefContainingAttribute(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test.cs">
                            <![CDATA[
using System;
/// <summary>
/// <see cref="{|yAttribute:GooAttribute|}" />
/// </summary>
[{|y:Goo|}]
class {|yAttribute:G$$ooAttribute|} : Attribute
{
}

]]>
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="yAttribute")

                result.AssertLabeledSpansAre("yAttribute", replacement:="yAttribute", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("y", replacement:="y", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546952")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        Public Sub RenameIncludingCrefContainingAttribute_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="VisualBasicAssembly" CommonReferences="true">
                        <Document>
                            <![CDATA[
''' <summary>
''' <see cref="{|YAttribute:$$GooAttribute|}" />
''' </summary>
<{|Y:Goo|}>
Class {|YAttribute:GooAttribute|}
	Inherits Attribute
End Class
]]>
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="YAttribute")

                result.AssertLabeledSpansAre("YAttribute", replacement:="YAttribute", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Y", replacement:="Y", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546952")>
        Public Sub RenameFromCrefContainingAttribute(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test.cs">
                            <![CDATA[
using System;
/// <summary>
/// <see cref="{|yAttribute:GooA$$ttribute|}" />
/// </summary>
[{|y:Goo|}]
class {|yAttribute:GooAttribute|} : Attribute
{
}

]]>
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="yAttribute")

                result.AssertLabeledSpansAre("yAttribute", replacement:="yAttribute", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("y", replacement:="y", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546952"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        Public Sub RenameFromCrefContainingAttribute_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="VisualBasicAssembly" CommonReferences="true">
                        <Document FilePath="Test.vb">
                            <![CDATA[
Imports System
''' <summary>
''' <see cref="{|yAttribute:GooA$$ttribute|}" />
''' </summary>
<{|y:Goo|}>
Class {|yAttribute:GooAttribute|} : Inherits Attribute
End Class
]]>
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="yAttribute")

                result.AssertLabeledSpansAre("yAttribute", replacement:="yAttribute", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("y", replacement:="y", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531015")>
        Public Sub RenameCrefTypeParameter(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test.cs">
                            <![CDATA[
/// <see cref="C{[|$$T|]}.M({|Conflict:T|})"/>
class C<T> { }
]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="K")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/640373")>
        Public Sub RenameCref1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using System;
class Tester
{
    private int {|Resolve:$$x|};
    /// <summary>
    /// <see cref="Tester.{|Resolve:x|}"/>
    /// </summary>
    public int X
    {
        get
        {
            return {|Resolve:x|}; 
        }

        set
        {
            {|Resolve:x|} = value;
        }
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="if")

                result.AssertLabeledSpecialSpansAre("Resolve", "@if", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/640373")>
        Public Sub RenameCref1_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Imports System
Class Tester
    Private {|Resolve1:$$_x|} As Integer
    ''' <summary>
    ''' <see cref="Tester.{|Resolve2:_x|}"/>
    ''' </summary>
    Public Property X As Integer
        Get
            Return {|Resolve1:_x|}; 
        End Get

        Set
            {|Resolve1:_x|} = value;
        End Set
    End Property
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="If")

                result.AssertLabeledSpecialSpansAre("Resolve1", "[If]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("Resolve2", "If", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using System;
class Tester
{
    private int [|$$x|];
    /// <summary>
    /// <see cref="[|x|]"/>
    /// </summary>
    public int X
    {
        get
        {
            return {|Stmt1:x|}; 
        }

        set
        {
            {|Stmt2:x|} = value;
        }
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("Stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Stmt2", "y", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref2_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Imports System
Class Tester
    Private [|$$_x|] As Integer
    ''' <summary>
    ''' <see cref="[|_x|]"/>
    ''' </summary>
    Public Property X As Integer
        Get
            Return {|Stmt1:_x|}
        End Get

        Set
            {|Stmt2:_x|} = value
        End Set
    End Property
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("Stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Stmt2", "y", RelatedLocationType.NoConflict)

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref3(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
class C<[|$$T|]>
{
    /// <summary>
    /// <see cref="C{T}"/>
    /// </summary>
    /// <param name="x"></param>
    void Goo(C<dynamic> x, C<string> y) { }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref3_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class C(Of [|$$T|])
    ''' <summary>
    ''' <see cref="C(Of [|T|])"/>
    ''' </summary>
    ''' <param name="x"></param>
    Sub Goo(x As C(Of Object), y As C(Of String))
    End Sub
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref4(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
class C<T>
{
    /// <summary>
    /// <see cref="C{[|$$T|]}"/>
    /// <see cref="C{T}.Bar(T)" />
    /// </summary>
    /// <param name="x"></param>
    void Goo(C<dynamic> x, C<string> y) { }
    void Bar(T x) { }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref4_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class C(Of [|T|])
    ''' <summary>
    ''' <see cref="C(Of [|$$T|])"/>
    ''' <see cref="C(Of [|T|]).Bar(Of [|T|])" />
    ''' </summary>
    ''' <param name="x"></param>
    Sub Goo(x As C(Of Object), y As C(Of String))
    End Sub
    Sub Bar(x As [|T|])
    End Sub
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref5(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
class {|Resolve:C|}<T>
{
    /// <summary>
    /// <see cref="{|Resolve:C|}{T}"/>
    /// </summary>
    /// <param name="x"></param>
    void Goo({|Resolve:$$C|}<dynamic> x, {|Resolve:C|}<string> y) { }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="string")

                result.AssertLabeledSpecialSpansAre("Resolve", "@string", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref5_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class {|Resolve:C|}(Of T)
    ''' <summary>
    ''' <see cref="{|Resolve:C|}(Of T)"/>
    ''' </summary>
    ''' <param name="x"></param>
    Sub Goo(x As {|Resolve:$$C|}(Of Object), y As {|Resolve:C|}(Of String))
    End Sub
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="String")

                result.AssertLabeledSpecialSpansAre("Resolve", "[String]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref6(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
namespace Test
{
    /// <summary>
    /// <seealso cref="global::[|$$C|]"/>
    /// </summary>
    class C
    {
        void Goo() { }
    }
}
class [|C|]
{

}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref6_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Namespace Test
    ''' <summary>
    ''' <seealso cref="Global.[|$$C|]"/>
    ''' </summary>
    Class C
        Sub Goo()
        End Sub
    End Class
End Namespace
Class [|C|]

End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref7(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
namespace Test
{
    /// <summary>
    /// <see cref="P"/>
    /// <see cref="P.[|K|]"/>
    /// </summary>
    public class C
    {
        /// <summary>
        /// <see cref="P"/>
        /// <see cref="[|K|]"/>
        /// </summary>
        public class P
        {
            /// <summary>
            /// <see cref="[|$$K|]"/>
            /// </summary>
            public class [|K|]
            {

            }
        }      
    }
    /// <summary>
    /// <see cref="C.P.[|K|]"/>
    /// </summary>
    public class P
    {

    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="C")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref7_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Namespace Test
    ''' <summary>
    ''' <see cref="P"/>
    ''' <see cref="P.[|K|]"/>
    ''' </summary>
    Class C
        ''' <summary>
        ''' <see cref="P"/>
        ''' <see cref="[|K|]"/>
        ''' </summary>
        Class P
            ''' <summary>
            ''' <see cref="[|$$K|]"/>
            ''' </summary>
            Class [|K|]
            End Class
        End Class   
    End Class
    ''' <summary>
    ''' <see cref="C.P.[|K|]"/>
    ''' </summary>
    Class P
    End Class
End Namespace]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="C")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref8(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
interface I
{
    void [|Bar|]();
}
abstract class C
{
    public abstract void [|Bar|]();
}
class B : C, I
{
    /// <summary>
    /// <see cref="I.[|Bar|]()"/>
    /// <see cref="[|$$Bar|]()"/>
    /// <see cref="C.[|Bar|]()"/>
    /// </summary>
    public override void [|Bar|]()
    {
        throw new NotImplementedException();
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref8_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Interface I
    Sub [|Bar|]
End Interface
MustInherit Class C
    Public MustOverride Sub [|Bar|]()
End Class
Class B : Inherits C : Implements I
    ''' <summary>
    ''' <see cref="I.[|Bar|]()"/>
    ''' <see cref="[|$$Bar|]()"/>
    ''' <see cref="B.[|Bar|]()"/>
    ''' <see cref="C.[|Bar|]()"/>
    ''' </summary>
    Public Overrides Sub [|Bar|]() Implements I.[|Bar|]
        Throw new NotImplementedException()
    End Sub
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

            End Using
        End Sub

        <WpfTheory(Skip:="640502"), CombinatorialData>
        Public Sub RenameCref9(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
interface I
{
    void [|Goo|]();
}
abstract class C
{
    public void [|Goo|]() { }
}

class B : C, I
{
    /// <summary>
    /// <see cref="I.[|Goo|]()"/>
    /// <see cref="[|Goo|]()"/>
    /// <see cref="C.[|Goo|]()"/>
    /// </summary>
    public void Bar()
    {
        [|$$Goo|]();
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Baz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref9_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Interface I
    Sub {|Interface:Goo|}()
End Interface
MustInherit Class C
    Public Sub [|Goo|]()
    End Sub
End Class
Class B : Inherits C : Implements I
    ''' <summary>
    ''' <see cref="I.{|Interface:Goo|}()"/>
    ''' <see cref="[|Goo|]()"/>
    ''' <see cref="C.[|Goo|]()"/>
    ''' </summary>
    Public Sub Bar() Implements I.Goo
        [|$$Goo|]()
    End Sub
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Baz")

                result.AssertLabeledSpansAre("Interface", "Goo")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref10(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
class C
{
    delegate void [|$$Del|](int x);
    /// <summary>
    /// <see cref="[|Del|]"/>
    /// <see cref="B.Del" />
    /// </summary>
    void Goo()
    {
        [|Del|] d;
    }
    class B
    {
        /// <summary>
        /// <see cref="C.[|Del|]"/>
        /// </summary>
        /// <param name="x"></param>
        delegate void Del(int x);
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Bel")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref10_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class C
    Delegate Sub [|$$Del|](x As Integer)
    ''' <summary>
    ''' <see cref="[|Del|]"/>
    ''' <see cref="B.Del" />
    ''' </summary>
    Sub Goo()
        Dim d As [|Del|]
    End Sub
    Class B
        ''' <summary>
        ''' <see cref="C.[|Del|]"/>
        ''' </summary>
        ''' <param name="x"></param>
        Delegate Sub Del(x As Integer)
    End Class
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Bel")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref11(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
class Test
{
    /// <summary>
    /// <see cref="[|P|]"/>
    /// </summary>
    /// <param name="x"></param>
    public delegate void Del(int x);
    public event Del [|P|];
    void Sub()
    {
        [|$$P|](1);
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Bel")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref11_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class Test
    ''' <summary>
    ''' <see cref="[|P|]"/>
    ''' </summary>
    Public Event [|P|](x As Integer)
    Sub Subroutine()
        RaiseEvent [|$$P|](1)
    End Sub
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Bel")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref12(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using System;

class Program
{
    /// <summary>
    /// <see cref="{|resolve:Bar|}"></see>
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {   
    }

    static void [|$$Goo|]()
    {
    }
}

class Bar
{
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("resolve", "global::Bar", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref12_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Imports System

Class Program
    ''' <summary>
    ''' <see cref="{|resolve:Bar|}"></see>
    ''' </summary>
    ''' <param name="args"></param>
    Shared Sub Main(args As String())
    End Sub

    Shared Sub [|$$Goo|]()
    End Sub
End Class

Class Bar
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("resolve", "Global.Bar", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref13(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using System;

class Program
{
    /// <summary>
    /// <see cref="{|resolve:Bar|}"></see>
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {   
    }

    public class [|$$Baz|]
    {
    }
}

public class Bar
{
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("resolve", "global::Bar", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref13_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Imports System
Class Program
    ''' <summary>
    ''' <see cref="{|resolve:Bar|}"></see>
    ''' </summary>
    ''' <param name="args"></param>
    Shared Sub Main(args As String())
    End Sub

    Public Class [|$$Baz|]
    End Class
End Class

Public Class Bar
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("resolve", "Global.Bar", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref14(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
public class A
{
    public class B
    {
        public class C
        {
            /// <summary>
            /// <see cref=" {|resolve:D|}"/>
            /// </summary>
            static void [|$$goo|]()
            {

            }

        }

        public class D
        {

        }
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")

                result.AssertLabeledSpansAre("resolve", "B.D", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCref14_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Public Class A
    Public Class B
        Public Class C
            ''' <summary>
            ''' <see cref="{|resolve:D|}"/>
            ''' </summary>
            Shared Sub [|$$goo|]()
            End Sub
        End Class
        Public Class D
        End Class
    End Class
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")

                result.AssertLabeledSpansAre("resolve", "B.D", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673562")>
        Public Sub RenameCref15(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
namespace N
{
    class C
    {
        /// <summary>
        /// <see cref="{|Resolve:N|}.C"/>
        /// </summary>
        void Sub()
        { }
    }
    namespace [|$$K|] // Rename K to N
    {
        class C
        { }
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="N")

                result.AssertLabeledSpansAre("Resolve", "global::N", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673562")>
        Public Sub RenameCref15_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Namespace N
    Class C
        ''' <summary>
        ''' <see cref="{|Resolve:N|}.C"/>
        ''' </summary>
        Sub Subroutine()
        End Sub
    End Class
    Namespace [|$$K|] ' Rename K to N
        Class C
        End Class
    End Namespace
End Namespace]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="N")

                result.AssertLabeledSpansAre("Resolve", "Global.N.C", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673667"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760850")>
        Public Sub RenameCref17(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using System;
class P
{
    /// <summary>
    /// <see cref="[|b|]"/>
    /// </summary>
    Action<int> [|$$b|] = (int x) => { }; // Rename b to a
    class B
    {
        /// <summary>
        /// <see cref="{|Resolve:b|}"/>
        /// </summary>
        void a()
        {
        }
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="a")

                result.AssertLabeledSpansAre("Resolve", "P.a", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673667"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760850")>
        Public Sub RenameCref17_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Imports System
Class P
    ''' <summary>
    ''' <see cref="[|_b|]"/>
    ''' </summary>
    Private [|$$_b|] As Action(Of Integer) = Sub(x As Integer) 
                                             End Sub ' Rename _b to a
    Class B
        ''' <summary>
        ''' <see cref="{|Resolve:_b|}"/>
        ''' </summary>
        Sub a()
        End Sub
    End Class
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="a")

                result.AssertLabeledSpansAre("Resolve", "P.a", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673667")>
        Public Sub RenameCref18(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
namespace N
{
    using K = {|Resolve:N|}.C;
    class C
    {

    }
    /// <summary>
    /// <see cref="[|D|]"/>
    /// </summary>
    class [|$$D|]
    {
        class C
        {
            [|D|] x;
        }
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="N")

                result.AssertLabeledSpansAre("Resolve", "global::N", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCrefWithUsing1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using R1 = N1.C1;
namespace N1
{
    class C1
    {
        public class C2 { }
    }
    namespace N2
    {
        /// <summary>
        /// <see cref="{|Resolve:R1|}.C2"/>
        /// </summary>
        class [|$$K|]
        {
            class C2 { }
        }
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="R1")

                result.AssertLabeledSpansAre("Resolve", "C1", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        Public Sub RenameCrefWithUsing1_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Imports R1 = N1.C1
Namespace N1
    Class C1
        Public Class C2
        End Class
    End Class

    Namespace N2
        ''' <summary>
        ''' <see cref="{|Resolve:R1|}.C2"/>
        ''' </summary>
        Class [|$$K|]
            Private Class C2
            End Class
        End Class
    End Namespace
End Namespace
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="R1")

                result.AssertLabeledSpansAre("Resolve", "C1.C2", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCrefWithUsing2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using R1 = N1;
namespace N1
{
    public class C1
    {
        public class C2 { }
    }
    
}
namespace N2
{
    class N1
    {
        class C2
        {
            /// <summary>
            /// <see cref="{|Resolve:R1|}.C1.C2"/>
            /// </summary>
            class C3 { }
            class [|$$K|] { }
        }
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="R1")

                result.AssertLabeledSpansAre("Resolve", "global::N1", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767163")>
        Public Sub RenameCrefWithUsing2_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Imports R1 = N1
Namespace N1
    Public Class C1
        Public Class C2
        End Class
    End Class
End Namespace
Namespace N2
    Class N1
        Class C2
            ''' <summary>
            ''' <see cref="{|Resolve:R1|}.C1.C2"/>
            ''' </summary>
            Class C3
            End Class
            Class [|$$K|]
            End Class
        End Class
    End Class
End Namespace
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="R1")

                result.AssertLabeledSpansAre("Resolve", "Global.N1.C1.C2", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCrefWithInterface(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
namespace N
{
    interface [|I|]
    {
        void Goo();
    }
    class C : [|I|]
    {
        /// <summary>
        /// <see cref="{|Resolve:$$I|}.Goo"/>
        /// </summary>
        public void Goo() { }
        class K
        {

        }
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="K")

                result.AssertLabeledSpansAre("Resolve", "N.K", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767163")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        Public Sub RenameCrefWithInterface_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Namespace N
    Interface [|I|]
        Sub Goo()
    End Interface
    Class C : Implements {|Resolve1:I|}
        ''' <summary>
        ''' <see cref="{|Resolve2:$$I|}.Goo"/>
        ''' </summary>
        Public Sub Goo(host as TestHost) Implements {|Resolve1:I|}.Goo
        End Sub
        Class K
        End Class
    End Class
End Namespace]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="K")

                result.AssertLabeledSpansAre("Resolve1", "N.K", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("Resolve2", "N.K.Goo", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameCrefCrossAssembly(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <ProjectReference>VBAssembly</ProjectReference>
                            <Document><![CDATA[
class D : [|N|].C
{
    /// <summary>
    /// <see cref="{|Resolve:N|}.C.Goo"/>
    /// </summary>
    public void Sub()
    {
        Goo();
    }
    class R
    {
        class C
        {
            public void Goo() { }
        }
    }

}]]>
                            </Document>
                        </Project>
                        <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBAssembly">
                            <Document>
Namespace [|$$N|]
    Public Class C
        Public Sub Goo()

        End Sub
    End Class
End Namespace
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="R")

                result.AssertLabeledSpansAre("Resolve", "global::R", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767163")>
        Public Sub RenameCrefCrossAssembly_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <ProjectReference>CSAssembly</ProjectReference>
                            <Document><![CDATA[
Class D : Inherits {|Resolve1:N|}.C
    ''' <summary>
    ''' <see cref="{|Resolve2:N|}.C.Goo"/>
    ''' </summary>
    Public Sub Subroutine()
        Goo()
    End Sub
    Class R
        Class C
            Public Sub Goo()
            End Sub
        End Class
    End Class
End Class]]>
                            </Document>
                        </Project>
                        <Project Language="C#" CommonReferences="true" AssemblyName="CSAssembly">
                            <Document><![CDATA[
namespace [|$$N|]
{
    public class C
    {
        public void Goo() { }
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="R")

                result.AssertLabeledSpansAre("Resolve1", "Global.R", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("Resolve2", "Global.R.C.Goo", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673809")>
        Public Sub RenameStaticConstructorInsideCref1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
class [|P|]
{
    /// <summary>
    /// <see cref="[|P|].[|P|]"/>
    /// </summary>
    static [|$$P|]()
    {

    }
    /// <summary>
    /// <see cref="[|P|].[|P|]"/>
    /// </summary>
    public [|P|]() { }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Q")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673809")>
        Public Sub RenameStaticConstructorInsideCref1_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class [|$$P|]
    ''' <summary>
    ''' <see cref="[|P|].New()"/>
    ''' </summary>
    Shared Sub New()
    End Sub
    ''' <summary>
    ''' <see cref="[|P|].New()"/>
    ''' </summary>
    Public Sub New()
    End Sub
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Q")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameStaticConstructorInsideCref2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
class [|P|]
{
    /// <summary>
    /// <see cref="[|P|].[|$$P|]"/>
    /// </summary>
    static [|P|]()
    {

    }
    /// <summary>
    /// <see cref="[|P|].[|P|]"/>
    /// </summary>
    public [|P|]() { }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Q")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673809")>
        Public Sub RenameStaticConstructorInsideCref2_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class [|P|]
    ''' <summary>
    ''' <see cref="[|P|].New()"/>
    ''' </summary>
    Shared Sub New()
    End Sub
    ''' <summary>
    ''' <see cref="[|$$P|].New()"/>
    ''' </summary>
    Public Sub New()
    End Sub
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Q")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameInheritanceCref1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
class P
{
    public int y;
}
class Q : P
{
    int [|$$x|];
    /// <summary>
    /// <see cref="y"/> 
    /// </summary>
    void Sub()
    {
            
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="y")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameInheritanceCref1_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class P
    Public y As Integer
End Class
Class Q : Inherits P
    Private [|$$x|] As Integer
    ''' <summary>
    ''' <see cref="{|Resolve:y|}"/> 
    ''' </summary>
    Sub Subroutine()
    End Sub
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("Resolve", "P.y", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673858"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/666167")>
        Public Sub RenameGenericTypeCrefWithConflict(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
namespace N
{
    using K = C<int>;
    class C<T>
    {
        /// <summary>
        /// <see cref="C{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        class D<T>
        {
            /// <summary>
            /// <see cref="{|Resolve:K|}.D{int}"/>
            /// </summary>
            /// <typeparam name="P"></typeparam>
            class [|$$C|] // Rename C To K
            {
                class D<T> { }
            }
            class C<T>
            {
            }
        }
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="K")

                result.AssertLabeledSpansAre("Resolve", "N.C{int}", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673858"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/666167"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768000")>
        Public Sub RenameGenericTypeCrefWithConflict_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Imports K = N.C(Of Integer)
Namespace N
    Class C(Of T)
        ''' <summary>
        ''' <see cref="C(Of T)"/>
        ''' </summary>
        ''' <typeparam name="T"></typeparam>
        Class D(Of T)
            ''' <summary>
            ''' <see cref="{|Resolve:K|}.D(Of Integer)"/>
            ''' </summary>
            ''' <typeparam name="P"></typeparam>
            Class [|$$C|] ' Rename C To K
                Class D(Of T)
                End Class
            End Class
            Class C(Of T)            
            End Class
        End Class
    End Class
End Namespace]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="K")

                result.AssertLabeledSpansAre("Resolve", "N.C(Of Integer).D(Of Integer)", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub
#End Region

        <Theory, CombinatorialData>
        Public Sub RenameNestedNamespaceToParent1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
namespace N
{
    namespace [|$$K|]
    {

    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="N")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673641")>
        Public Sub RenameNestedNamespaceToParent2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
namespace N
{
    class C
    {
        void Goo()
        {
            {|Resolve:N|}.C x;
        }
    }
    namespace [|$$K|]
    {   
        class C { }
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="N")

                result.AssertLabeledSpansAre("Resolve", "global::N.C x;", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673809")>
        Public Sub RenameStaticConstructor(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
class [|Simple|]
{
    static [|$$Simple|]() // Rename simple here to p
    {
    }
    public [|Simple|]()
    {
    }
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="P")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/675882")>
        Public Sub SingleOnlyLocalDeclarationInsideLambdaRecurseInfinitely(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Module Lambdas
    Sub Main()
    End Sub
    Public l1 = Sub()
                    Dim x = 1
                End Sub
    Public L3 = Function([|x|] As Integer) [|$$x|] Mod 2
End Module

]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="xx")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/641231")>
        Public Sub RenameDoNotReplaceBaseConstructorToken_CSharp(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
class [|A|] 
{}
class B : [|A|]
{
    public B() : base()
    {
    }
}
class Program
{
    static void Main()
    {
        [|$$A|] a = new B();
    }
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="P")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/641231")>
        Public Sub RenameDoNotReplaceBaseConstructorToken_VisualBasic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Module Program
    Sub Main(args As String())
        Dim Asa As [|$$A|] = New B()
    End Sub
End Module

Class [|A|]

End Class

Class B
    Inherits [|A|]
    Public Sub New()
        MyBase.New()
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674762")>
        Public Sub RenameMergedNamespaceAcrossProjects(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Namespace [|$$N|]
    Public Class X
    End Class
End Namespace
]]>
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                            <ProjectReference>Project1</ProjectReference>
                            <Document>
class Y : [|N|].X { }
namespace [|N|] { }
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674764")>
        Public Sub RenameMergedNamespaceAcrossProjects_1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Namespace [|N|]
    Public Class X
    End Class
End Namespace
]]>
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                            <ProjectReference>Project1</ProjectReference>
                            <Document>
class Y : [|$$N|].X { }
namespace [|N|] { }
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716278")>
        Public Sub RenameMainWithoutAssertFailureVB(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub [|$$Main|](args As String())
        
    End Sub
End Module
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716278")>
        Public Sub RenameMainWithoutAssertFailureCSharp(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void [|$$Main|](string[] args)
    {
        
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="D")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/719062")>
        Public Sub RenameEscapedIdentifierToUnescapedIdentifier(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim [|[dim]$$|] = 4
    End Sub
End Module
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/719062")>
        Public Sub RenameEscapedIdentifierToUnescapedIdentifierKeyword(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim {|iden1:[dim]$$|} = 4
    End Sub
End Module
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="do")

                result.AssertLabeledSpecialSpansAre("iden1", "[do]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767187")>
        Public Sub RenameEscapedTypeNew(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Class [|[New]|]
End Class

Class C
    Public Sub F()
        Dim a = New [|$$[New]|]
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767187")>
        Public Sub RenameConstructorInCref(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
/// <see cref="[|C|].[|$$C|]">
class [|C|]
{
  public [|C|]() {}
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1009633")>
        Public Sub RenameWithTryCatchBlock1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Class C
    Sub M()
        Dim [|$$msg|] = "hello"
        Try
        Catch
            Dim newMsg = [|msg|]
        End Try
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="msg2")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1009633")>
        Public Sub RenameWithTryCatchBlock2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Class C
    Sub M()
        Dim [|$$msg|] = "hello"
        Try
        Catch ex As Exception
            Dim newMsg = [|msg|]
        End Try
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="msg2")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/8297")>
        Public Sub RenameVBClassNamedNew(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Class [|[$$New]|]
    Sub New()
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="New2")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25655")>
        Public Sub RenameClassWithNoCompilationProjectReferencingProject(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="NoCompilation" CommonReferences="false">
                        <ProjectReference>CSharpProject</ProjectReference>
                        <Document>
                            // a no-compilation document
                        </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="CSharpProject" CommonReferences="true">
                        <Document>
                            public class [|$$A|]
                            {
                                public [|A|]()
                                {
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="B")
            End Using
        End Sub

#Region "Rename in strings/comments"

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStrings(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Module [|$$Program|]
    ''' <summary>
    ''' Program PROGRAM! Program!
    ''' </summary>
    ''' <param name="args"></param>
    Sub Main(args As String())
        ' Program PROGRAM! Program!
        Dim renamed = {|RenameInString:"Program Program!"|}    ' Program
        Dim notRenamed = "PROGRAM! Program1 ProgramProgram"
    End Sub
End Module
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="NewProgram", renameOptions:=renameOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """NewProgram NewProgram!""")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStrings_CSharp(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
public class [|$$Program|]
{
	/// <summary>
	/// Program PROGRAM! Program!
    /// <Program></Program>
	/// </summary>
	public static void Main(string[] args)
	{
		// Program PROGRAM! Program!
		var renamed = {|RenameInString:"Program Program!"|};
		var notRenamed = "PROGRAM! Program1 ProgramProgram";
	}
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="NewProgram", renameOptions:=renameOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """NewProgram NewProgram!""")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInComments(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Module [|$$Program|]
    ''' <summary>
    '''{|RenameInComment1: Program PROGRAM! Program!|}
    ''' </summary>
    ''' <param name="args"></param>
    Sub Main(args As String())
        {|RenameInComment2:' Program PROGRAM! Program!|}
        Dim renamed = "Program Program!"    {|RenameInComment3:' Program|}
        Dim notRenamed = "PROGRAM! Program1 ProgramProgram"
    End Sub
End Module
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="NewProgram", renameOptions:=renameOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "' NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment3", "' NewProgram")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInComments_CSharp(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
public class [|$$Program|]
{
	/// <summary>
	///{|RenameInComment1: Program PROGRAM! Program!|}
    /// </summary>
	public static void Main(string[] args)
	{
		{|RenameInComment2:// Program PROGRAM! Program!|}
		var renamed = "Program Program!";
		var notRenamed = "PROGRAM! Program1 ProgramProgram";
	}
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="NewProgram", renameOptions:=renameOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "// NewProgram PROGRAM! NewProgram!")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInComments_XmlName(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
''' <summary>
''' <{|RenameInComment:Program|}> </{|RenameInComment:Program|}>
''' </summary>
Public Class [|$$Program|]
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="NewProgram", renameOptions:=renameOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment", "NewProgram")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInComments_XmlName2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
' Should not rename XmlName if not renaming in comments

''' <summary>
''' <Program> </Program>
''' </summary>
Public Class [|$$Program|]
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="NewProgram")

                Assert.Equal(1, result.ConflictResolution.RelatedLocations.Count)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInComments_XmlName_CSharp(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
/// <summary>
/// <{|RenameInComment:Program|}> </{|RenameInComment:Program|}>
/// </summary>
public class [|$$Program|]
{
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="NewProgram", renameOptions:=renameOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment", "NewProgram")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInComments_XmlName_CSharp2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[

// Should not rename XmlName if not renaming in comments

/// <summary>
/// <Program> </Program>
/// </summary>
public class [|$$Program|]
{
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="NewProgram")

                Assert.Equal(1, result.ConflictResolution.RelatedLocations.Count)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStringsAndComments(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True, RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Module [|$$Program|]
    ''' <summary>
    '''{|RenameInComment1: Program PROGRAM! Program!|}
    ''' </summary>
    ''' <param name="args"></param>
    Sub Main(args As String())
        {|RenameInComment2:' Program PROGRAM! Program!|}
        {|RenameInComment2:' Program PROGRAM! Program!|}
        Dim renamed = {|RenameInString:"Program Program!"|}    {|RenameInComment3:' Program|}
        Dim notRenamed = "PROGRAM! Program1 ProgramProgram"
    End Sub
End Module
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="NewProgram", renameOptions:=renameOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """NewProgram NewProgram!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "' NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment3", "' NewProgram")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStringsAndComments_CSharp(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True, RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
public class [|$$Program|]
{
	/// <summary>
	///{|RenameInComment1: Program PROGRAM! Program!|}
    /// </summary>
	public static void Main(string[] args)
	{
		{|RenameInComment2:// Program PROGRAM! Program!|}
		var renamed = {|RenameInString:"Program Program!"|};
		var notRenamed = "PROGRAM! Program1 ProgramProgram";
	}
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="NewProgram", renameOptions:=renameOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """NewProgram NewProgram!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "// NewProgram PROGRAM! NewProgram!")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStringsAndComments_SmallerReplacementString(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True, RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Module [|$$Program|]
    ''' <summary>
    '''{|RenameInComment1: Program PROGRAM! Program!|}
    ''' </summary>
    ''' <param name="args"></param>
    Sub Main(args As String())
        {|RenameInComment2:' Program PROGRAM! Program!|}
        Dim renamed = {|RenameInString:"Program Program!"|}    {|RenameInComment3:' Program|}
        Dim notRenamed = "PROGRAM! Program1 ProgramProgram"
    End Sub
End Module
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="P", renameOptions:=renameOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """P P!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " P PROGRAM! P!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "' P PROGRAM! P!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment3", "' P")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStringsAndComments_SmallerReplacementString_CSharp(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True, RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
public class [|$$Program|]
{
	/// <summary>
	///{|RenameInComment1: Program PROGRAM! Program!|}
    /// </summary>
	public static void Main(string[] args)
	{
		{|RenameInComment2:// Program PROGRAM! Program!|}
		var renamed = {|RenameInString:"Program Program!"|};
		var notRenamed = "PROGRAM! Program1 ProgramProgram";
	}
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="P", renameOptions:=renameOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """P P!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " P PROGRAM! P!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "// P PROGRAM! P!")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStringsAndComments_AnotherSourceFile(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True, RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Module [|$$Program|]
End Module
]]>
                            </Document>
                            <Document><![CDATA[
Class AnotherFile
    ''' <summary>
    '''{|RenameInComment1: Program PROGRAM! Program!|}
    ''' </summary>
    ''' <param name="args"></param>
    Sub Main(args As String())
        {|RenameInComment2:' Program PROGRAM! Program!|}
        Dim renamed = {|RenameInString:"Program Program!"|}    {|RenameInComment3:' Program|}
        Dim notRenamed = "PROGRAM! Program1 ProgramProgram"
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="P", renameOptions:=renameOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """P P!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " P PROGRAM! P!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "' P PROGRAM! P!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment3", "' P")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStringsAndComments_AnotherSourceFile_CSharp(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True, RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
public class [|$$Program|]
{
}
]]>
                            </Document>
                            <Document><![CDATA[
public class AnotherFile
{
	/// <summary>
	///{|RenameInComment1: Program PROGRAM! Program!|}
    /// </summary>
	public static void Main(string[] args)
	{
		{|RenameInComment2:// Program PROGRAM! Program!|}
		var renamed = {|RenameInString:"Program Program!"|};
		var notRenamed = "PROGRAM! Program1 ProgramProgram";
	}
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="NewProgram", renameOptions:=renameOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """NewProgram NewProgram!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "// NewProgram PROGRAM! NewProgram!")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStringsAndComments_AnotherProject(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True, RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Public Class [|$$Program|]
End Class
]]>
                            </Document>
                        </Project>
                        <Project Language="Visual Basic" AssemblyName="Project2" CommonReferences="true">
                            <ProjectReference>Project1</ProjectReference>
                            <Document><![CDATA[
' Renames should occur in referencing projects where rename symbol is accessible.

Class ReferencingProject
    ''' <summary>
    '''{|RenameInComment1: Program PROGRAM! Program!|}
    ''' </summary>
    ''' <param name="args"></param>
    Sub Main(args As String())
        {|RenameInComment2:' Program PROGRAM! Program!|}
        {|RenameInComment2:' Program PROGRAM! Program!|}
        Dim renamed = {|RenameInString:"Program Program!"|}    {|RenameInComment3:' Program|}
        Dim notRenamed = "PROGRAM! Program1 ProgramProgram"
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="NewProgram", renameOptions:=renameOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """NewProgram NewProgram!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "' NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment3", "' NewProgram")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStringsAndComments_AnotherProject_CSharp(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True, RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
public class [|$$Program|]
{
}
]]>
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="Project2" CommonReferences="true">
                            <ProjectReference>Project1</ProjectReference>
                            <Document><![CDATA[
// Renames should occur in referencing projects where rename symbol is accessible.

public class AnotherFile
{
	/// <summary>
	///{|RenameInComment1: Program PROGRAM! Program!|}
    /// </summary>
	public static void Main(string[] args)
	{
		{|RenameInComment2:// Program PROGRAM! Program!|}
		var renamed = {|RenameInString:"Program Program!"|};
		var notRenamed = "PROGRAM! Program1 ProgramProgram";
	}
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="NewProgram", renameOptions:=renameOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """NewProgram NewProgram!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "// NewProgram PROGRAM! NewProgram!")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStringsAndComments_AnotherProject2(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True, RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[

Namespace N
    Private Class [|$$Program|]
    End Class
End Namespace
]]>
                            </Document>
                        </Project>
                        <Project Language="Visual Basic" AssemblyName="ReferencingProjectButSymbolNotVisible" CommonReferences="true">
                            <ProjectReference>Project1</ProjectReference>
                            <Document><![CDATA[
' Renames should not occur in referencing projects where rename symbol is not accessible.
' Type "Program" is not accessible in this project

Imports N

Class ReferencingProjectButSymbolNotVisible
    ''' <summary>
    ''' Program PROGRAM! Program!
    ''' </summary>
    ''' <param name="args"></param>
    Sub Main(args As String())
        ' Program PROGRAM! Program!
        ' Program PROGRAM! Program!
        Dim renamed = "Program Program!"    ' Program
        Dim notRenamed = "PROGRAM! Program1 ProgramProgram"
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                        <Project Language="Visual Basic" AssemblyName="NotReferencingProject" CommonReferences="true">
                            <Document><![CDATA[
' Renames should not occur in non-referencing projects.
' Type "Program" is not visible in this project

Imports N

Class NotReferencingProject
    ''' <summary>
    ''' Program PROGRAM! Program!
    ''' </summary>
    ''' <param name="args"></param>
    Sub Main(args As String())
        ' Program PROGRAM! Program!
        ' Program PROGRAM! Program!
        Dim renamed = "Program Program!"    ' Program
        Dim notRenamed = "PROGRAM! Program1 ProgramProgram"
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="NewProgram", renameOptions:=renameOptions)

                Assert.Equal(1, result.ConflictResolution.RelatedLocations.Count)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStringsAndComments_AnotherProject_CSharp2(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True, RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
namespace N
{
    private class [|$$Program|]
    {
    }
}
]]>
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="ReferencingProjectButSymbolNotVisible" CommonReferences="true">
                            <ProjectReference>Project1</ProjectReference>
                            <Document><![CDATA[
// Renames should not occur in referencing projects where rename symbol is not accessible.
// Type "Program" is not accessible in this project

using N;

public class ReferencingProjectButSymbolNotVisible
{
	/// <summary>
	/// Program PROGRAM! Program!
    /// </summary>
	public static void Main(string[] args)
	{
		// Program PROGRAM! Program!
		var renamed = "Program Program!";
		var notRenamed = "PROGRAM! Program1 ProgramProgram";
	}
}
]]>
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="NotReferencingProject" CommonReferences="true">
                            <Document><![CDATA[
// Renames should not occur in non-referencing projects.
// Type "Program" is not visible in this project

public class NotReferencingProject
{
	/// <summary>
	/// Program PROGRAM! Program!
    /// </summary>
	public static void Main(string[] args)
	{
		// Program PROGRAM! Program!
		var renamed = "Program Program!";
		var notRenamed = "PROGRAM! Program1 ProgramProgram";
	}
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="NewProgram", renameOptions:=renameOptions)

                Assert.Equal(1, result.ConflictResolution.RelatedLocations.Count)
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStringsAndComments_WithResolvableConflict(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True, RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Module M
    Public Sub [|$$Goo|](Of T)(ByVal x As T) {|RenameInComment1:' Rename Goo to Bar|}
    End Sub
End Module

Class C
    Public Sub Bar(ByVal x As String)
    End Sub
    Class M
        Public Shared Bar As Action(Of String) = Sub(ByVal x As String)
                                                 End Sub
    End Class
    Public Sub Test()
        {|stmt1:Goo|}("1")

        {|RenameInComment2:' Goo GOO! Goo!|}
        Dim renamed = {|RenameInString:"Goo Goo!"|}    {|RenameInComment3:' Goo|}
        Dim notRenamed = "GOO! Goo1 GooGoo"
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Bar", renameOptions:=renameOptions)

                result.AssertLabeledSpansAre("stmt1", "Call Global.M.Bar(""1"")", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", "' Rename Bar to Bar")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """Bar Bar!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "' Bar GOO! Bar!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment3", "' Bar")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStringsAndComments_WithResolvableConflict_CSharp(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True, RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
class Goo
{
    int goo;
    void Blah(int [|$$bar|])
    {
        {|stmt2:goo|} = {|stmt1:bar|};

		{|RenameInComment:// bar BAR! bar!|}
		var renamed = {|RenameInString:"bar bar!"|};
		var notRenamed = "BAR! bar1 barbar";
    }
}
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="goo", renameOptions:=renameOptions)

                result.AssertLabeledSpansAre("stmt1", "this.goo = goo;", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", "this.goo = goo;", RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """goo goo!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment", "// goo BAR! goo!")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStringsAndComments_WithUnresolvableConflict(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True, RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Option Explicit Off
Module Program
    Function [|$$Goo|]
        {|Conflict:Bar|} = 1

        {|RenameInComment1:' Goo GOO! Goo!|}
        Dim renamed = {|RenameInString:"Goo Goo!"|}    {|RenameInComment2:' Goo|}
        Dim notRenamed = "GOO! Goo1 GooGoo"
    End Function
End Module
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Bar", renameOptions:=renameOptions)

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """Bar Bar!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", "' Bar GOO! Bar!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "' Bar")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Sub RenameInStringsAndComments_WithUnresolvableConflict_CSharp(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameInStrings:=True, RenameInComments:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
class {|Conflict:goo|}
{
    int [|$$bar|];

	{|RenameInComment:// bar BAR! bar!|}
	var renamed = {|RenameInString:"bar bar!"|};
	var notRenamed = "BAR! bar1 barbar";
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="goo", renameOptions:=renameOptions)

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """goo goo!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment", "// goo BAR! goo!")
            End Using
        End Sub

#End Region

#Region "Rename In NameOf"

        <Theory, CombinatorialData>
        Public Sub RenameMethodWithNameof_NoOverloads_CSharp(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
class C
{
    void [|$$M|]()
    {
        nameof([|M|]).ToString();
    }
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Mo")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameMethodWithNameof_WithOverloads_CSharp(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
class C
{
    void [|$$M|]()
    {
        nameof(M).ToString();
    }

    void M(int x)
    {
    }
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Mo")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameMethodWithNameof_WithOverloads_WithRenameOverloadsOption_CSharp(host As RenameTestHost)
            Dim renameOptions = New SymbolRenameOptions(RenameOverloads:=True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
class C
{
    void [|$$M|]()
    {
        nameof([|M|]).ToString();
    }

    void [|M|](int x)
    {
    }
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Mo", renameOptions:=renameOptions)

            End Using
        End Sub
#End Region

        <WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/28474")>
        <CombinatorialData>
        Public Sub HandleProjectsWithoutCompilations(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpProject" CommonReferences="true">
                        <Document>
                            public interface IGoo
                            {
                                void [|$$Goo|]();
                            }
                            public class C : IGoo
                            {
                                public void [|Goo|]() {}
                            }
                        </Document>
                    </Project>
                    <Project Language="NoCompilation" CommonReferences="false">
                        <ProjectReference>CSharpProject</ProjectReference>
                        <Document>
                            // a no-compilation document
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Cat")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/44070")>
        Public Sub RenameTypeParameterFromCRef(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
class C
{
    /// <summary>
    /// <see cref="Goo{$$[|X|]}([|X|])"/>
    /// </summary>
    void Goo<T>(T t) { }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/883")>
        Public Sub RenameAnonymousTypePropertyInDeclaration(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = new
        {
            [|$$Prop|] = 3,
            args,
        };

        Console.WriteLine(x.[|Prop|]);
        Console.WriteLine(x.args);
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Property")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/883")>
        Public Sub RenameAnonymousTypePropertyInAccess(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = new
        {
            [|Prop|] = 3,
            args,
        };

        Console.WriteLine(x.[|$$Prop|]);
        Console.WriteLine(x.args);
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Property")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/883")>
        Public Sub RenameAnonymousTypePropertyMultipleOccurrences(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = new { [|$$Name|] = "Test" };
        var y = new { [|Name|] = "Test2" };
        
        Console.WriteLine(x.[|Name|]);
        Console.WriteLine(y.[|Name|]);
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="FullName")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/67640")>
        Public Sub RenameNamedTypeAlias(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using [|$$S1|] = System.String;

namespace Test
{
    class Program
    {
        void M([|S1|] s)
        {
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="RenamedString")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/67640")>
        Public Sub RenameTupleAlias(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using [|$$TupleExample|] = (string, int);

namespace ConsoleApp
{
    internal class Class1
    {
        public void F([|TupleExample|] x)
        {
            [|TupleExample|] a = ("sdf", 42);
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="RenamedTupleExample")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/67640")>
        Public Sub RenameArrayAlias(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using [|$$arrayExample|] = int[];

namespace ConsoleApp
{
    internal class Class1
    {
        public void F([|arrayExample|] x)
        {
            [|arrayExample|] b = new[] { 3, 4, 5 };
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="RenamedArrayExample")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/67640")>
        Public Sub RenameTupleAliasFromUsage(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using [|TupleExample|] = (string, int);

namespace ConsoleApp
{
    internal class Class1
    {
        public void F([|$$TupleExample|] x)
        {
            [|TupleExample|] a = ("sdf", 42);
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="RenamedTupleExample")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/67640")>
        Public Sub RenameArrayAliasFromUsage(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using [|arrayExample|] = int[];

namespace ConsoleApp
{
    internal class Class1
    {
        public void F([|$$arrayExample|] x)
        {
            [|arrayExample|] b = new[] { 3, 4, 5 };
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="RenamedArrayExample")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/67640")>
        Public Sub RenamePointerAlias(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using [|$$IntPtr|] = int*;

namespace ConsoleApp
{
    internal class Class1
    {
        public unsafe void F([|IntPtr|] x)
        {
            [|IntPtr|] p = null;
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="RenamedIntPtr")
            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/67640")>
        Public Sub RenameDynamicAlias(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using [|$$DynType|] = dynamic;

namespace ConsoleApp
{
    internal class Class1
    {
        public void F([|DynType|] x)
        {
            [|DynType|] d = 42;
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="RenamedDynType")
            End Using
        End Sub
    End Class
End Namespace
