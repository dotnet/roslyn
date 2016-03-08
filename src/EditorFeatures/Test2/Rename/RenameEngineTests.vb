' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Rename

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    ''' <summary>
    ''' This test class contains tests which verifies that the rename engine performs correct
    ''' refactorings that do not break user code. These tests call through the
    ''' Renamer.RenameSymbol entrypoint, and thus do not test any behavior with interactive
    ''' rename. The position given with the $$ mark in the tests is just the symbol that is renamed;
    ''' there is no fancy logic applied to it.
    ''' </summary>
    Partial Public Class RenameEngineTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <WorkItem(543661, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543661")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameNamespaceAlias()
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
                   </Workspace>, renameTo:="Sys2")
            End Using
        End Sub

        <WorkItem(813409, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/813409")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameTypeDoesNotRenameGeneratedConstructorCalls()
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
                   </Workspace>, renameTo:="U")

            End Using
        End Sub

        <WorkItem(856078, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/856078")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub UnmodifiedDocumentsAreNotCheckedOutBySourceControl()
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
                    </Workspace>, renameTo:="def")

                Dim originalDocument = result.ConflictResolution.OldSolution.Projects.First().Documents.Where(Function(d) d.FilePath = "Test2.cs").Single()
                Dim newDocument = result.ConflictResolution.NewSolution.Projects.First().Documents.Where(Function(d) d.FilePath = "Test2.cs").Single()
                Assert.Same(originalDocument.State, newDocument.State)
                Assert.Equal(1, result.ConflictResolution.NewSolution.GetChangedDocuments(result.ConflictResolution.OldSolution).Count)
            End Using
        End Sub

        <WorkItem(773400, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773400")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ReferenceConflictInsideDelegateLocalNotRenamed()
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
        Foo {|DeclConflict:x|} = new Foo(FooMeth);
        int [|$$z|] = 1; // Rename z to x
        x({|unresolve2:z|});
    }
}
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="x")

                result.AssertLabeledSpansAre("DeclConflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("unresolve2", "x", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(773673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773673")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMoreThanOneTokenInUnResolvedStatement()
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
                    </Workspace>, renameTo:="C")

                result.AssertLabeledSpansAre("DeclConflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("unresolve1", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("unresolve2", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("unresolve3", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInvocationExpressionAcrossProjects()
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
                                void foo()
                                {
                                    [|ClassA|].Bar();
                                }
                            }
                         </Document>
                    </Project>
                </Workspace>, renameTo:="A")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInvocationExpressionAcrossProjectsWithPartialClass()
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
                                void foo()
                                {
                                    [|ClassA|].Bar();
                                }
                            }
                         </Document>
                    </Project>
                </Workspace>, renameTo:="A")

            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInvocationExpressionAcrossProjectsWithConflictResolve()
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
                                void foo()
                                {
                                    {|resolve2:ClassA|}.Bar();
                                }
                            }
                         </Document>
                    </Project>
                </Workspace>, renameTo:="A")

                result.AssertLabeledSpansAre("resolve", "global::A", RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansAre("resolve2", "X.A.Bar();", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RollBackExpandedConflicts_WithinInvocationExpression()
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
                            </Workspace>, renameTo:="a")

                result.AssertLabeledSpansAre("conflict1", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("conflict2", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("conflict3", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RollBackExpandedConflicts_WithinQuery()
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
                            </Workspace>, renameTo:="y")


                result.AssertLabeledSpansAre("conflict1", "y", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("conflict2", "y", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameOverloadCSharp()
            Dim changingOptions = New Dictionary(Of OptionKey, Object)()
            changingOptions.Add(RenameOptions.RenameOverloads, True)
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
                                            public void [|$$foo|]()
                                            {
                                                [|foo|]();
                                            }

                                            public void [|foo|]&lt;T&gt;()
                                            {
                                                [|foo|]&lt;T&gt;();
                                            }

                                            public void [|foo|](int i)
                                            {
                                                [|foo|](i);
                                            }
                                        }
                                    </Document>
                                </Project>
                            </Workspace>, renameTo:="BarBaz", changedOptionSet:=changingOptions)


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameOverloadVisualBasic()
            Dim changingOptions = New Dictionary(Of OptionKey, Object)()
            changingOptions.Add(RenameOptions.RenameOverloads, True)
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

                                            Public Sub [|$$foo|]()
                                                [|foo|]()
                                            End Sub

                                            Public Sub [|foo|](Of T)()
                                                [|foo|](Of T)()
                                            End Sub

                                            Public Sub [|foo|](s As String)
                                                [|foo|](s)
                                            End Sub

                                            Public Shared Sub [|foo|](d As Double)
                                                [|foo|](d)
                                            End Sub
                                        End Class
                                    </Document>
                                </Project>
                            </Workspace>, renameTo:="BarBaz", changedOptionSet:=changingOptions)


            End Using
        End Sub

        <Fact, WorkItem(761929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/761929"), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictCheckInInvocationCSharp()
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
                </Workspace>, renameTo:="Bar")


            End Using
        End Sub

        <Fact, WorkItem(761922, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/761922"), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictCheckInInvocationVisualBasic()
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
                </Workspace>, renameTo:="Bar")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameType()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>

                            class [|$$Foo|]
                            {
                                void Blah()
                                {
                                    {|stmt1:Foo|} f = new {|stmt1:Foo|}();
                                }
                            }

                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")


                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameTypeAcrossFiles()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#">
                        <Document>
                            class [|$$Foo|]
                            {
                                void Blah()
                                {
                                    {|stmt1:Foo|} f = new {|stmt1:Foo|}();
                                }
                            }
                        </Document>
                        <Document>
                            class FogBar
                            {
                                void Blah()
                                {
                                    {|stmt2:Foo|} f = new {|stmt2:Foo|}();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")


                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameTypeAcrossProjectsAndLanguages()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document>
                            namespace N
                            {
                                 public class [|$$Foo|]
                                 {
                                     void Blah()
                                     {
                                         {|csstmt1:Foo|} f = new {|csstmt1:Foo|}();
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
                                   Dim f = new {|vbstmt1:Foo|}()
                                End Sub
                            End Class
                         </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")


                result.AssertLabeledSpansAre("csstmt1", "BarBaz", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("vbstmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCSharpTypeFromConstructorDefinition()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class [|Foo|]
                            {
                                [|$$Foo|]()
                                {
                                }
                            
                                void Blah()
                                {
                                    {|stmt1:Foo|} f = new {|stmt1:Foo|}();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCSharpTypeFromConstructorUse()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class [|Foo|]
                            {
                                [|Foo|]()
                                {
                                }
                            
                                void Blah()
                                {
                                    {|stmt1:Foo|} f = new {|stmt1:$$Foo|}();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")

                result.AssertLabeledSpansAre("stmt1", replacement:="Bar", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCSharpTypeFromDestructor()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class [|Foo|]
                            {
                                ~[|$$Foo|]()
                                {
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCSharpTypeFromSynthesizedConstructorUse()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                               class [|Foo|]
                               {
                                   void Blah()
                                   {
                                       {|stmt1:Foo|} f = new {|stmt1:$$Foo|}();
                                   }
                               }
                           </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")

                result.AssertLabeledSpansAre("stmt1", replacement:="Bar", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCSharpPredefinedTypeVariables1()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                    using System;

                                    class Program
                                    {
                                        static void Main(string[] args)
                                        {
                                            System.Int32 {|stmt1:$$foofoo|} = 23;
                                        }
                                    }
                                </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCSharpPredefinedTypeVariables2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                    using System;

                                    class Program
                                    {
                                        static void Main(string[] args)
                                        {
                                            Int32 {|stmt1:$$foofoo|} = 23;
                                        }
                                    }
                                </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCSharpPredefinedTypeVariables3()
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
                </Workspace>, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameVisualBasicPredefinedTypeVariables1()
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
                </Workspace>, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameVisualBasicPredefinedTypeVariables2()
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
                </Workspace>, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameVisualBasicPredefinedTypeVariables3()
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
                </Workspace>, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(539801, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539801")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCSharpEnumMemberToContainingEnumName()
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
                </Workspace>, renameTo:="Test")


            End Using
        End Sub

        <WorkItem(539801, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539801")>
        <WorkItem(5886, "https://github.com/dotnet/roslyn/pull/5886")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCSharpEnumToEnumMemberName()
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
                </Workspace>, renameTo:="TestEnum")


            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameVisualBasicTypeFromConstructorUse()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class [|Foo|]
                                Public Sub New()
                                End Sub

                                Public Sub Blah()
                                    Dim x = New {|stmt1:$$Foo|}()
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("stmt1", "Bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameVisualBasicTypeFromSynthesizedConstructorUse()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                           Class [|Foo|]
                               Public Sub Blah()
                                   Dim x = New {|stmt1:$$Foo|}()
                               End Sub
                           End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")

                result.AssertLabeledSpansAre("stmt1", "Bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameNamespace()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Namespace [|$$FooNamespace|]
                            End Namespace
                        </Document>
                        <Document>
                            namespace [|FooNamespace|] { }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBazNamespace")


            End Using
        End Sub

        <Fact>
        <WorkItem(6874, "http://vstfdevdiv:8080/DevDiv_Projects/Roslyn/_workitems/edit/6874")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameVisualBasicEnum()
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
                </Workspace>, renameTo:="BarBaz")


            End Using
        End Sub

        <Fact>
        <WorkItem(6874, "http://vstfdevdiv:8080/DevDiv_Projects/Roslyn/_workitems/edit/6874")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameVisualBasicEnumMember()
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
                </Workspace>, renameTo:="BarBaz")


            End Using
        End Sub

        <Fact>
        <WorkItem(539525, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539525")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub DoNothingRename()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                           class Foo
                           {
                               void [|$$Blah|]()
                               {
                               }
                           }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Blah")


            End Using
        End Sub

        <Fact>
        <WorkItem(553631, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553631")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharpBugfix553631()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Program
{
    void Main(string[] args)
    {
        string {|stmt1:$$s|} = Foo&lt;string, string&gt;();
    }
 
    string Foo&lt;T, S&gt;()
    {
        Return Foo &lt;T, S&gt;();
    }
}
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Blah")

                result.AssertLabeledSpansAre("stmt1", replacement:="Blah", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(553631, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553631")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VisualBasicBugfix553631()
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
        Dim {|stmt1:$$e|} As String = Foo(Of String)()
    End Sub

    Public Function Foo(Of A)() As String
    End Function
End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Blah")

                result.AssertLabeledSpansAre("stmt1", replacement:="Blah", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(541697, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541697")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameExtensionMethod()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            public static class C
                            {
                                public static void [|$$Foo|](this string s) { }
                            }
                            
                            class Program
                            {
                                static void Main()
                                {
                                    "".{|stmt1:Foo|}();
                                }
                            }
                       </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")


                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(542202, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542202")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCSharpIndexerNamedArgument1()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C
                            {
                                int this[int x = 5, int [|y|] = 7] { get { return 0; } set { } }
 
                                void Foo()
                                {
                                    var y = this[{|stmt1:$$y|}: 1];
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")


                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(542106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542106")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCSharpIndexerNamedArgument2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C
                            {
                                int this[int [|$$x|]] { set { } }

                                void Foo()
                                {
                                    this[{|stmt1:x|}: 1] = 0;
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(541928, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541928")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameRangeVariable()
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
                </Workspace>, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(542106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542106")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameVisualBasicParameterizedPropertyNamedArgument()
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

                                Sub Foo()
                                    Me({|stmt1:x|}:=1) = 0
                                End Sub
                            End Class
                       </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")


                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(543340, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543340")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameIndexerParameterFromDeclaration()
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
                </Workspace>, renameTo:="BarBaz")


                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(543340, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543340")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameIndexerParameterFromUseInsideGetAccessor()
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
                </Workspace>, renameTo:="BarBaz")


                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(543340, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543340")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameIndexerParameterFromUseInsideSetAccessor()
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
                </Workspace>, renameTo:="BarBaz")


                result.AssertLabeledSpansAre("stmt1", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", replacement:="BarBaz", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(542492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542492")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamePartialMethodParameter()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>

using System;

public partial class Test
{
    partial void Foo(object [|$$o|])
    {
    }
}

partial class Test
{
    partial void Foo(object [|o|]);
}

                       </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")


            End Using
        End Sub

        <WorkItem(528820, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528820")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameVisualBasicAnonymousKey()
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
                </Workspace>, renameTo:="y")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(542543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542543")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameIncludesPreviouslyInvalidReference()
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
                </Workspace>, renameTo:="x")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(543027, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543027")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameVariableInQueryAsUsingStatement()
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
                </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", replacement:="y", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", replacement:="y", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(543169, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543169")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub LambdaWithOutParameter()
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
                </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("stmt1", replacement:="y", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(543567, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543567")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CascadeBetweenOverridingProperties()
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
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("stmt1", "Foo", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(529799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529799")>
        Public Sub CascadeBetweenImplementedInterfaceEvent()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="VisualBasicAssembly" CommonReferences="true">
                        <Document FilePath="Test.vb">
Class C
    Event E([|$$x|] As Integer)

    Sub Foo()
        RaiseEvent E({|stmt1:x|}:=1)
    End Sub
End Class

                       </Document>
                    </Project>
                </Workspace>, renameTo:="y")


                result.AssertLabeledSpansAre("stmt1", "y", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(543567, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543567")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CascadeBetweenEventParameters()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test.cs">
using System;

interface IFoo
{
    event EventHandler [|$$Foo|];
}

class Bar : IFoo
{
    public event EventHandler [|Foo|];
}

                       </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(531260, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531260")>
        Public Sub DoNotCascadeToMetadataSymbols()
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
                </Workspace>, renameTo:="CloneImpl")


            End Using
        End Sub

        <WorkItem(545473, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545473")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamePartialTypeParameter_CSharp1()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
partial class Class1
{
    partial void foo<$$[|T|]>([|T|] x);
    partial void foo<[|T|]>([|T|] x)
    {
    }
}]]></Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")


            End Using
        End Sub

        <WorkItem(545473, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545473")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamePartialTypeParameter_CSharp2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
partial class Class1
{
    partial void foo<[|T|]>([|T|] x);
    partial void foo<$$[|T|]>([|T|] x)
    {
    }
}]]></Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")


            End Using
        End Sub

        <WorkItem(545472, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545472")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamePartialTypeParameter_VB1()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Public Module Module1
    Partial Private Sub Foo(Of [|$$T|] As Class)(x as [|T|])
    End Sub
    Private Sub foo(Of [|T|] As Class)(x as [|T|])
    End Sub
End Module
]]></Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")


            End Using
        End Sub

        <WorkItem(545472, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545472")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamePartialTypeParameter_VB2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Public Module Module1
    Partial Private Sub Foo(Of [|T|] As Class)(x as [|T|])
    End Sub
    Private Sub foo(Of [|$$T|] As Class)(x as [|T|])
    End Sub
End Module
]]></Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")


            End Using
        End Sub

        <WorkItem(529163, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529163")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub AmbiguousBeforeRenameHandledCorrectly_Bug11516()
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
                </Workspace>, renameTo:="N")


            End Using
        End Sub

        <WorkItem(529163, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529163")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub AmbiguousBeforeRenameHandledCorrectly_Bug11516_2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            namespace Foo
                            {
                                class B
                                { }
                            }

                            namespace Foo.[|$$B|]
                            { }
                            </Document>
                    </Project>
                </Workspace>, renameTo:="N")


            End Using
        End Sub

        <WorkItem(554092, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554092")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamePartialMethods_1_CS()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            partial class Foo
                            {
                                partial void [|F|]();
                            }
                            partial class Foo
                            {
                                partial void [|$$F|]()
                                {
                                    throw new System.Exception("F");
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


            End Using
        End Sub

        <WorkItem(554092, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554092")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamePartialMethods_2_CS()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            partial class Foo
                            {
                                partial void [|$$F|]();
                            }
                            partial class Foo
                            {
                                partial void [|F|]()
                                {
                                    throw new System.Exception("F");
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


            End Using
        End Sub

        <WorkItem(554092, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554092")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamePartialMethods_1_VB()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            partial class Foo
                                partial private sub [|F|]()
                            end class
                            partial class Foo
                                private sub [|$$F|]()
                                    throw new System.Exception("F")
                                end sub
                            end class
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


            End Using
        End Sub

        <WorkItem(554092, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554092")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamePartialMethods_2_VB()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            partial class Foo
                                partial private sub [|$$F|]()
                            end class
                            partial class Foo
                                private sub [|F|]()
                                    throw new System.Exception("F")
                                end sub
                            end class
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


            End Using
        End Sub

        <WorkItem(530740, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530740")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug530740()
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
                                static void Foo(int x) { Console.WriteLine("int"); }
                                static void Foo(MyInt x) { Console.WriteLine("MyInt"); }
                                static void Main()
                                {
                                    Foo((MyInt)0);
                                }
                            }
                            </Document>
                    </Project>
                </Workspace>, renameTo:="MyInt")


            End Using
        End Sub

        <WorkItem(530082, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530082")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMethodThatImplementsInterfaceMethod()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports System

                            Interface I
                                Sub Foo(Optional x As Integer = 0)
                            End Interface

                            Class C
                                Implements I

                                Shared Sub Main()
                                    DirectCast(New C(), I).Foo()
                                End Sub

                                Private Sub [|$$I_Foo|](Optional x As Integer = 0) Implements I.Foo
                                    Console.WriteLine("test")
                                End Sub
                            End Class
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


            End Using
        End Sub

        <WorkItem(529874, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529874")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub DoNotRemoveAttributeSuffixOn__Attribute()
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
                </Workspace>, renameTo:="＿Attribute")


            End Using
        End Sub

        <WorkItem(553315, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553315")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameEventParameterOnUsage()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class C
                            Event E([|x|] As Integer)
 
                            Sub Foo()
                                RaiseEvent E({|stmt1:$$x|}:=1)
                            End Sub
                        End Class
                            </Document>
                    </Project>
                </Workspace>, renameTo:="bar")


                result.AssertLabeledSpansAre("stmt1", "bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(529819, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529819")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCompilerGeneratedBackingFieldForNonCustomEvent()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class B
                                Event [|$$X|]() ' Rename X to Y
                                Sub Foo()
                                    {|stmt1:XEvent|}()
                                End Sub
                            End Class
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Y")


                result.AssertLabeledSpecialSpansAre("stmt1", "YEvent", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(576607, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576607")>
        <WorkItem(529819, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529819")>
        <Fact()>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCompilerGeneratedEventHandlerForNonCustomEvent()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class B
                                Event [|$$X|]() ' Rename X to Y
                                Sub Foo()
                                    Dim y = New B.{|stmt1:XEventHandler|}(AddressOf bar)
                                End Sub

                                Shared Sub bar()
                                End Sub
                            End Class
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Y")


                result.AssertLabeledSpecialSpansAre("stmt1", "YEventHandler", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(576966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576966")>
        <Fact()>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharpRenameParenthesizedFunctionName()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                        class C
                        {
                            void [|$$Foo|]()
                            {
                                (({|stmt1:Foo|}))();
                            }
                        }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("stmt1", "Bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(601123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/601123")>
        <Fact()>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VisualBasicAwaitAsIdentifierInAsyncShouldBeEscaped()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Imports System
                            Module M
                                Async Function methodIt() As Task(Of String)
                                    Dim a As Func(Of String) = AddressOf {|stmt1:Foo|}
                                End Function

                                Function [|$$Foo|]() As String
                                End Function
                            End Module
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Await")

                result.AssertLabeledSpecialSpansAre("stmt1", "[Await]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(601123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/601123")>
        <Fact()>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharpAwaitAsIdentifierInAsyncMethodShouldBeEscaped0()
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
        Func<string> a = {|stmt1:Foo|};
        return null;
    }

    public static string [|$$Foo|]()
    {
        return "";
    }
}
]]>

                        </Document>
                    </Project>
                </Workspace>, renameTo:="await")

                result.AssertLabeledSpansAre("stmt1", "@await", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(601123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/601123")>
        <Fact()>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharpAwaitAsIdentifierInAsyncLambdaShouldBeEscaped()
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
        Func<Task> sin = async () => { {|stmt1:Foo|}(); };
    }

    public int [|$$Foo|]()
    {
        return 0;
    }

}
]]>

                        </Document>
                    </Project>
                </Workspace>, renameTo:="await")

                result.AssertLabeledSpansAre("stmt1", "@await", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(601123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/601123")>
        <Fact()>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharpAwaitAsIdentifierInAsyncLambdaShouldBeEscaped1()
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
        Func<int, Task<int>> sink2 = (async c => { return {|stmt2:Foo|}(); });
    }

    public int [|$$Foo|]()
    {
        return 0;
    }

}
]]>

                        </Document>
                    </Project>
                </Workspace>, renameTo:="await")


                result.AssertLabeledSpansAre("stmt2", "@await", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(601123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/601123")>
        <Fact()>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharpAwaitAsIdentifierInAsyncLambdaShouldNotBeEscaped()
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
        Func<int, int> sink4 = ((c) => { return {|stmt3:Foo|}(); });
    }

    public int [|$$Foo|]()
    {
        return 0;
    }

}
]]>

                        </Document>
                    </Project>
                </Workspace>, renameTo:="await")


                result.AssertLabeledSpansAre("stmt3", "await", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact()>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VBRoundTripMissingModuleName_1()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                        Imports N

                        Module Program
                            Sub Main()
                                Dim mybar As N.{|stmt1:Foo|} = New {|stmt1:Foo|}()
                            End Sub
                        End Module

                        Namespace N
                            Module X
                                Class [|$$Foo|]
                                End Class
                            End Module

                        End Namespace
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("stmt1", "Bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact()>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VBRoundTripMissingModuleName_2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                        Imports N

                        Module Program
                            Sub Main()
                                N.{|stmt1:Foo|}()
                            End Sub
                        End Module

                        Namespace N
                            Module X
                                Sub [|$$Foo|]()
                                End Sub
                            End Module

                        End Namespace
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("stmt1", "Bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(603767, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603767")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug603767_RenamePartialAttributeDeclarationWithDifferentCasingAndCSharpUsage()
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
                </Workspace>, renameTo:="XATTRIBUTe")


                result.AssertLabeledSpansAre("resolved", "XATTRIBUTe", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(603371, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603371")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug603371_VisualBasicRenameAttributeToGlobal()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Imports System

                            &lt;{|escaped:Foo|}&gt;
                            Class {|escaped:$$Foo|} ' Rename Foo to Global
                                Inherits Attribute
                            End Class
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Global")

                result.AssertLabeledSpecialSpansAre("escaped", "[Global]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(603371, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603371")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug603371_VisualBasicRenameAttributeToGlobal_2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Imports System

                            &lt;{|escaped:Foo|}&gt;
                            Class {|escaped:$$Foo|} ' Rename Foo to Global
                                Inherits Attribute
                            End Class
                            </Document>
                    </Project>
                </Workspace>, renameTo:="[global]")

                result.AssertLabeledSpansAre("escaped", "[global]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(602494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602494")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug602494_RenameOverrideWithNoOverriddenMember()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            class C1
                            {
                                override void [|$$Foo|]() {}
                            }
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


            End Using
        End Sub

        <WorkItem(576607, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576607")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug576607()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
Class B
    Event [|$$X|]()
    Sub Foo()
        Dim y = New {|stmt1:XEventHandler|}(AddressOf bar)
    End Sub

    Shared Sub bar()
    End Sub
End Class
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Baz")


                result.AssertLabeledSpecialSpansAre("stmt1", "BazEventHandler", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(529765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug529765_VisualBasicOverrideImplicitPropertyAccessor()
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
                </Workspace>, renameTo:="Z")


                result.AssertLabeledSpecialSpansAre("getaccessor", "get_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(529765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug529765_VisualBasicOverrideImplicitPropertyAccessor_2()
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
                </Workspace>, renameTo:="Z")


                result.AssertLabeledSpecialSpansAre("getaccessor", "Get_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(529765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug529765_VisualBasicOverrideImplicitPropertyAccessor_3()
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
                </Workspace>, renameTo:="Z")


                result.AssertLabeledSpecialSpansAre("getaccessor", "Get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessor", "Set_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(529765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug529765_CrossLanguageOverrideImplicitPropertyAccessor_1()
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
                </Workspace>, renameTo:="Z")


                result.AssertLabeledSpecialSpansAre("getaccessor", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessor", "set_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(529765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug529765_CrossLanguageOverrideImplicitPropertyAccessor_2()
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
                </Workspace>, renameTo:="Z")


                result.AssertLabeledSpecialSpansAre("getaccessor", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessor", "set_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("getaccessorstmt", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessorstmt", "set_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(529765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug529765_CrossLanguageOverrideImplicitPropertyAccessor_3()
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
                </Workspace>, renameTo:="Z")


                result.AssertLabeledSpecialSpansAre("getaccessor", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessor", "set_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("getaccessorstmt", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessorstmt", "set_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(529765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug529765_CrossLanguageOverrideImplicitPropertyAccessor_4()
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
                </Workspace>, renameTo:="Z")


                result.AssertLabeledSpecialSpansAre("getaccessor", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessor", "set_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("getaccessorstmt", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessorstmt", "set_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(529765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529765")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug529765_CrossLanguageOverrideImplicitPropertyAccessor_5()
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
                </Workspace>, renameTo:="Z")


                result.AssertLabeledSpecialSpansAre("getaccessor", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessor", "set_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("getaccessorstmt", "get_Z", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("setaccessorstmt", "set_Z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(610120, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/610120")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug610120_CrossLanguageOverrideImplicitPropertyAccessorConflictWithVBProperty()
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
                </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("conflict", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("getaccessorstmt", "get_y", RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("setaccessorstmt", "set_y", RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("declconflict", "y", RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpecialSpansAre("setaccessor", "set_y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("getaccessor", "get_y", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(612380, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/612380")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug612380_CrossLanguageOverrideImplicitPropertyAccessorCascadesToInterface()
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
                </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("conflict", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("declconflict", "y", RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpecialSpansAre("getaccessor", "get_y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("getaccessorstmt", "get_y", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(866094, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/866094")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug866094()
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
                </Workspace>, renameTo:="X")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameDynamicallyBoundFunction()
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
                </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("Stmt1", "Bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(529989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529989")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharp_RenameToIdentifierWithUnicodeEscaping()
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
                </Workspace>, renameTo:="M\u0061in")


                result.AssertLabeledSpansAre("stmt1", "M\u0061in", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(576966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576966")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharp_RenameParenthesizedMethodNames()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document>
class C
{
    void [|$$Foo|]()
    {
        (({|stmt1:Foo|}))();
    }
}
                            </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("stmt1", "Bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(632052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632052")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharp_RenameParameterUsedInObjectInitializer()
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
                </Workspace>, renameTo:="n")


                result.AssertLabeledSpansAre("stmt1", "n", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", "n", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(624092, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624092")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub LocationsIssue()
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
                </Workspace>, renameTo:="D")


                result.AssertLabeledSpansAre("resolved", "Dim c = New N.M.D()", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameParam1()
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
                    </Workspace>, renameTo:="pargs")


            End Using
        End Sub

        <WorkItem(569103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameParam1_VisualBasic()
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
                    </Workspace>, renameTo:="pargs")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameParam2()
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
                    </Workspace>, renameTo:="if")

                result.AssertLabeledSpecialSpansAre("Resolve", "@if", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("ResolveWithoutAt", "if", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(569103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameParam2_VisualBasic()
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
                    </Workspace>, renameTo:="[If]")

                result.AssertLabeledSpansAre("Resolve", <![CDATA[[If]]]>.Value, RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameTypeParam1()
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
    [|T|] Foo<U, P>(U x, [|T|] z) { return z; }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="D")

            End Using
        End Sub

        <WorkItem(569103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameTypeParam1_VisualBasic()
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
    Private Function Foo(Of U, P)(x As U, z As [|T|]) As [|T|]
        Return z
    End Function
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="D")


            End Using
        End Sub

        <WorkItem(624310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624310")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug624310_VBCascadeLambdaParameterInFieldInitializer()
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
                </Workspace>, renameTo:="z")

                result.AssertLabeledSpansAre("fieldinit", "z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(624310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624310")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug624310_CSNoCascadeLambdaParameterInFieldInitializer()
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
                </Workspace>, renameTo:="z")

                result.AssertLabeledSpansAre("fieldinit", "z", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(633582, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633582")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug633582_CSDoNotAddParenthesesInExpansionForParenthesizedBinaryExpression()
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
                </Workspace>, renameTo:="xxx")

                result.AssertLabeledSpansAre("stmt", "xxx", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(622086, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/622086")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/9412")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug622086_CSRenameExplicitInterfaceImplementation()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document><![CDATA[
namespace X
{
    interface [|$$IFoo|]
    {
        void Clone();
    }

    class Baz : [|IFoo|]
    {
        void [|IFoo|].Clone()
        {

        }
    }
}
]]>]
                        </Document>
                    </Project>
                </Workspace>, renameTo:="IFooEx")


            End Using
        End Sub

        <WorkItem(529803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529803")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamingEventCascadesToCSUsingEventHandlerDelegate()
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

                </Workspace>, renameTo:="Y")


                result.AssertLabeledSpecialSpansAre("handler", "YEventHandler", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(529803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529803")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamingCompilerGeneratedDelegateTypeForEventCascadesBackToEvent_1()
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

                </Workspace>, renameTo:="Y")


                result.AssertLabeledSpecialSpansAre("handler", "YEventHandler", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(529803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529803")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamingCompilerGeneratedDelegateTypeForEventCascadesBackToEvent_2()
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

                </Workspace>, renameTo:="Y")


                result.AssertLabeledSpecialSpansAre("handler", "YEventHandler", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(655621, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/655621")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamingCompilerGeneratedDelegateTypeForEventCascadesBackToEvent_3()
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

                </Workspace>, renameTo:="Y")


                result.AssertLabeledSpecialSpansAre("handler", "YEventHandler", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(655621, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/655621")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamingCompilerGeneratedDelegateTypeForEventCascadesBackToEvent_4()
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

                </Workspace>, renameTo:="Y")


                result.AssertLabeledSpecialSpansAre("handler", "YEventHandler", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(655621, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/655621"), WorkItem(762094, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/762094")>
        <WpfFact(Skip:="762094"), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamingCompilerGeneratedDelegateTypeForEventCascadesBackToEvent_5()
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

                </Workspace>, renameTo:="Y")


                result.AssertLabeledSpecialSpansAre("handler", "YEventHandler", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(627297, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug622086_CSRenameVarNotSupported()
            Assert.ThrowsAny(Of ArgumentException)(Sub()
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
                </Workspace>, renameTo:="Int32")
                                                   End Sub)
        End Sub

        <WorkItem(627297, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug622086_CSRenameCustomTypeNamedVarSupported()
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
                </Workspace>, renameTo:="bar")


                result.AssertLabeledSpansAre("stmt", "bar", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(627297, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug622086_CSRenameDynamicNotSupported()
            Assert.ThrowsAny(Of ArgumentException)(Sub()
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
                </Workspace>, renameTo:="Int32")
                                                   End Sub)
        End Sub

        <WorkItem(627297, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug622086_CSRenameToVarSupported()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document><![CDATA[
class [|Program|]
{
    static void Main(string[] args)
    {
        {|stmt:$$Program|} x = null;
    }
}
]]>]
                        </Document>
                    </Project>
                </Workspace>, renameTo:="var")


                result.AssertLabeledSpansAre("stmt", "var", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(627297, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug622086_CSRenameToVarSupportedButConflictsWithOtherVar()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document><![CDATA[
class [|Program|]
{
    static void Main(string[] args)
    {
        {|stmt:$$Program|} x = null;
        {|conflict:var|} y = 23;
    }
}
]]>]
                        </Document>
                    </Project>
                </Workspace>, renameTo:="var")


                result.AssertLabeledSpansAre("stmt", "var", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(627297, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug622086_CSRenameToVarSupportedButDoesntConflictsWithOtherVarOfSameType()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document><![CDATA[
class [|Program|]
{
    static void Main(string[] args)
    {
        {|stmt1:$$Program|} x = null;
        var y = new {|stmt2:Program|}();
    }
}
]]>]
                        </Document>
                    </Project>
                </Workspace>, renameTo:="var")


                result.AssertLabeledSpansAre("stmt1", "var", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", "var", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(627297, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug622086_CSRenameToDynamicSupported()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document><![CDATA[
class [|Program|]
{
    static void Main(string[] args)
    {
        {|stmt:$$Program|} x = null;
    }
}
]]>]
                        </Document>
                    </Project>
                </Workspace>, renameTo:="dynamic")


                result.AssertLabeledSpansAre("stmt", "dynamic", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(627297, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug622086_CSRenameToDynamicSupportedButConflictsWithOtherDynamic_1()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document><![CDATA[
class [|Program|]
{
    static void Main(string[] args)
    {
        {|stmt:$$Program|} x = null;
        {|conflict:dynamic|} y = 23;
    }
}
]]>]
                        </Document>
                    </Project>
                </Workspace>, renameTo:="dynamic")


                result.AssertLabeledSpansAre("stmt", "dynamic", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(627297, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627297")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug622086_CSRenameToDynamicSupportedButConflictsWithOtherDynamic_2()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                        <Document><![CDATA[
class [|Program|]
{
    static void Main(string[] args)
    {
        {|stmt1:$$Program|} x = null;
        {|conflict:dynamic|} y = new {|stmt2:Program|}();
    }
}
]]>]
                        </Document>
                    </Project>
                </Workspace>, renameTo:="dynamic")


                result.AssertLabeledSpansAre("stmt1", "dynamic", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", "dynamic", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(608988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608988")>
        <WorkItem(608989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608989")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameNamespaceInVbFromCSharReference()
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

                </Workspace>, renameTo:="Foo")


            End Using
        End Sub

#Region "Cref"

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameTypeFromCref()
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
                </Workspace>, renameTo:="y")


            End Using
        End Sub

        <WorkItem(569103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameTypeFromCref_VisualBasic()
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
                </Workspace>, renameTo:="Y")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMemberFromCref()
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
                </Workspace>, renameTo:="y")


            End Using
        End Sub

        <WorkItem(569103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMemberFromCref_VisualBasic()
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
                </Workspace>, renameTo:="Y")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCrefFromMember()
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
                </Workspace>, renameTo:="y")


            End Using
        End Sub

        <WorkItem(569103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCrefFromMember_VisualBasic()
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
                </Workspace>, renameTo:="y")


            End Using
        End Sub

        <WorkItem(546952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546952")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameIncludingCrefContainingAttribute()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test.cs">
                            <![CDATA[
using System;
/// <summary>
/// <see cref="{|yAttribute:FooAttribute|}" />
/// </summary>
[{|y:Foo|}]
class {|yAttribute:F$$ooAttribute|} : Attribute
{
}

]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="yAttribute")

                result.AssertLabeledSpansAre("yAttribute", replacement:="yAttribute", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("y", replacement:="y", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(546952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546952")>
        <WorkItem(569103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameIncludingCrefContainingAttribute_VisualBasic()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="VisualBasicAssembly" CommonReferences="true">
                        <Document>
                            <![CDATA[
''' <summary>
''' <see cref="{|YAttribute:$$FooAttribute|}" />
''' </summary>
<{|Y:Foo|}>
Class {|YAttribute:FooAttribute|}
	Inherits Attribute
End Class
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="YAttribute")

                result.AssertLabeledSpansAre("YAttribute", replacement:="YAttribute", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Y", replacement:="Y", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(546952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546952")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameFromCrefContainingAttribute()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test.cs">
                            <![CDATA[
using System;
/// <summary>
/// <see cref="{|yAttribute:FooA$$ttribute|}" />
/// </summary>
[{|y:Foo|}]
class {|yAttribute:FooAttribute|} : Attribute
{
}

]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="yAttribute")

                result.AssertLabeledSpansAre("yAttribute", replacement:="yAttribute", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("y", replacement:="y", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(546952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546952"), WorkItem(569103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameFromCrefContainingAttribute_VisualBasic()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="VisualBasicAssembly" CommonReferences="true">
                        <Document FilePath="Test.vb">
                            <![CDATA[
Imports System
''' <summary>
''' <see cref="{|yAttribute:FooA$$ttribute|}" />
''' </summary>
<{|y:Foo|}>
Class {|yAttribute:FooAttribute|} : Inherits Attribute
End Class
]]>
                        </Document>
                    </Project>
                </Workspace>, renameTo:="yAttribute")

                result.AssertLabeledSpansAre("yAttribute", replacement:="yAttribute", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("y", replacement:="y", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(531015, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531015")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCrefTypeParameter()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test.cs">
                            <![CDATA[
/// <see cref="C{[|$$T|]}.M({|Conflict:T|})"/>
class C<T> { }
]]></Document>
                    </Project>
                </Workspace>, renameTo:="K")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(640373, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/640373")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref1()
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
                    </Workspace>, renameTo:="if")

                result.AssertLabeledSpecialSpansAre("Resolve", "@if", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(640373, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/640373")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref1_VisualBasic()
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
                    </Workspace>, renameTo:="If")

                result.AssertLabeledSpecialSpansAre("Resolve1", "[If]", RelatedLocationType.NoConflict)
                result.AssertLabeledSpecialSpansAre("Resolve2", "If", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref2()
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
                    </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("Stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Stmt2", "y", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref2_VisualBasic()
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
                    </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("Stmt1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Stmt2", "y", RelatedLocationType.NoConflict)

            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref3()
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
    void Foo(C<dynamic> x, C<string> y) { }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="D")

            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref3_VisualBasic()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class C(Of [|$$T|])
    ''' <summary>
    ''' <see cref="C(Of [|T|])"/>
    ''' </summary>
    ''' <param name="x"></param>
    Sub Foo(x As C(Of Object), y As C(Of String))
    End Sub
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="D")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref4()
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
    void Foo(C<dynamic> x, C<string> y) { }
    void Bar(T x) { }
}]]>

                            </Document>
                        </Project>
                    </Workspace>, renameTo:="D")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref4_VisualBasic()
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
    Sub Foo(x As C(Of Object), y As C(Of String))
    End Sub
    Sub Bar(x As [|T|])
    End Sub
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="D")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref5()
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
    void Foo({|Resolve:$$C|}<dynamic> x, {|Resolve:C|}<string> y) { }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="string")

                result.AssertLabeledSpecialSpansAre("Resolve", "@string", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref5_VisualBasic()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class {|Resolve:C|}(Of T)
    ''' <summary>
    ''' <see cref="{|Resolve:C|}(Of T)"/>
    ''' </summary>
    ''' <param name="x"></param>
    Sub Foo(x As {|Resolve:$$C|}(Of Object), y As {|Resolve:C|}(Of String))
    End Sub
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="String")

                result.AssertLabeledSpecialSpansAre("Resolve", "[String]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref6()
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
        void Foo() { }
    }
}
class [|C|]
{

}]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="D")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref6_VisualBasic()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Namespace Test
    ''' <summary>
    ''' <seealso cref="Global.[|$$C|]"/>
    ''' </summary>
    Class C
        Sub Foo()
        End Sub
    End Class
End Namespace
Class [|C|]

End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="D")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref7()
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
                    </Workspace>, renameTo:="C")


            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref7_VisualBasic()
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
                    </Workspace>, renameTo:="C")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref8()
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
                    </Workspace>, renameTo:="Foo")


            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref8_VisualBasic()
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
                    </Workspace>, renameTo:="Foo")


            End Using
        End Sub

        <WpfFact(Skip:="640502"), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref9()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
interface I
{
    void [|Foo|]();
}
abstract class C
{
    public void [|Foo|]() { }
}

class B : C, I
{
    /// <summary>
    /// <see cref="I.[|Foo|]()"/>
    /// <see cref="[|Foo|]()"/>
    /// <see cref="C.[|Foo|]()"/>
    /// </summary>
    public void Bar()
    {
        [|$$Foo|]();
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Baz")


            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref9_VisualBasic()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Interface I
    Sub {|Interface:Foo|}()
End Interface
MustInherit Class C
    Public Sub [|Foo|]()
    End Sub
End Class
Class B : Inherits C : Implements I
    ''' <summary>
    ''' <see cref="I.{|Interface:Foo|}()"/>
    ''' <see cref="[|Foo|]()"/>
    ''' <see cref="C.[|Foo|]()"/>
    ''' </summary>
    Public Sub Bar() Implements I.Foo
        [|$$Foo|]()
    End Sub
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Baz")

                result.AssertLabeledSpansAre("Interface", "Foo")

            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref10()
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
    void Foo()
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
                    </Workspace>, renameTo:="Bel")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref10_VisualBasic()
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
    Sub Foo()
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
                    </Workspace>, renameTo:="Bel")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref11()
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
                    </Workspace>, renameTo:="Bel")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref11_VisualBasic()
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
                    </Workspace>, renameTo:="Bel")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref12()
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

    static void [|$$Foo|]()
    {
    }
}

class Bar
{
}]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("resolve", "global::Bar", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref12_VisualBasic()
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

    Shared Sub [|$$Foo|]()
    End Sub
End Class

Class Bar
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("resolve", "Global.Bar", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref13()
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
                    </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("resolve", "global::Bar", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref13_VisualBasic()
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
                    </Workspace>, renameTo:="Bar")


                result.AssertLabeledSpansAre("resolve", "Global.Bar", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref14()
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
            static void [|$$foo|]()
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
                    </Workspace>, renameTo:="D")


                result.AssertLabeledSpansAre("resolve", "B.D", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref14_VisualBasic()
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
            Shared Sub [|$$foo|]()
            End Sub
        End Class
        Public Class D
        End Class
    End Class
End Class]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="D")


                result.AssertLabeledSpansAre("resolve", "B.D", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(673562, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673562")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref15()
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
                    </Workspace>, renameTo:="N")


                result.AssertLabeledSpansAre("Resolve", "global::N", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(673562, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673562")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref15_VisualBasic()
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
                    </Workspace>, renameTo:="N")


                result.AssertLabeledSpansAre("Resolve", "Global.N.C", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(673667, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673667"), WorkItem(760850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760850")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref17()
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
                    </Workspace>, renameTo:="a")


                result.AssertLabeledSpansAre("Resolve", "P.a", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <WorkItem(673667, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673667"), WorkItem(760850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760850")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref17_VisualBasic()
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
                    </Workspace>, renameTo:="a")


                result.AssertLabeledSpansAre("Resolve", "P.a", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <WorkItem(673667, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673667")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCref18()
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
                    </Workspace>, renameTo:="N")


                result.AssertLabeledSpansAre("Resolve", "global::N", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCrefWithUsing1()
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
                    </Workspace>, renameTo:="R1")


                result.AssertLabeledSpansAre("Resolve", "C1", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(569103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCrefWithUsing1_VisualBasic()
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
                    </Workspace>, renameTo:="R1")


                result.AssertLabeledSpansAre("Resolve", "C1.C2", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCrefWithUsing2()
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
                    </Workspace>, renameTo:="R1")


                result.AssertLabeledSpansAre("Resolve", "global::N1", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(767163, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767163")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCrefWithUsing2_VisualBasic()
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
                    </Workspace>, renameTo:="R1")


                result.AssertLabeledSpansAre("Resolve", "Global.N1.C1.C2", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCrefWithInterface()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
namespace N
{
    interface [|I|]
    {
        void Foo();
    }
    class C : [|I|]
    {
        /// <summary>
        /// <see cref="{|Resolve:$$I|}.Foo"/>
        /// </summary>
        public void Foo() { }
        class K
        {

        }
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="K")


                result.AssertLabeledSpansAre("Resolve", "N.K", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <WorkItem(767163, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767163")>
        <WorkItem(569103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCrefWithInterface_VisualBasic()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Namespace N
    Interface [|I|]
        Sub Foo()
    End Interface
    Class C : Implements {|Resolve1:I|}
        ''' <summary>
        ''' <see cref="{|Resolve2:$$I|}.Foo"/>
        ''' </summary>
        Public Sub Foo() Implements {|Resolve1:I|}.Foo
        End Sub
        Class K
        End Class
    End Class
End Namespace]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="K")


                result.AssertLabeledSpansAre("Resolve1", "N.K", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("Resolve2", "N.K.Foo", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCrefCrossAssembly()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <ProjectReference>VBAssembly</ProjectReference>
                            <Document><![CDATA[
class D : [|N|].C
{
    /// <summary>
    /// <see cref="{|Resolve:N|}.C.Foo"/>
    /// </summary>
    public void Sub()
    {
        Foo();
    }
    class R
    {
        class C
        {
            public void Foo() { }
        }
    }

}]]>
                            </Document>
                        </Project>
                        <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBAssembly">
                            <Document>
Namespace [|$$N|]
    Public Class C
        Public Sub Foo()

        End Sub
    End Class
End Namespace
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="R")


                result.AssertLabeledSpansAre("Resolve", "global::R", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <WorkItem(767163, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767163")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCrefCrossAssembly_VisualBasic()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <ProjectReference>CSAssembly</ProjectReference>
                            <Document><![CDATA[
Class D : Inherits {|Resolve1:N|}.C
    ''' <summary>
    ''' <see cref="{|Resolve2:N|}.C.Foo"/>
    ''' </summary>
    Public Sub Subroutine()
        Foo()
    End Sub
    Class R
        Class C
            Public Sub Foo()
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
        public void Foo() { }
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="R")


                result.AssertLabeledSpansAre("Resolve1", "Global.R", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("Resolve2", "Global.R.C.Foo", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <WorkItem(673809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673809")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameStaticConstructorInsideCref1()
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
                    </Workspace>, renameTo:="Q")


            End Using
        End Sub

        <WorkItem(673809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673809")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameStaticConstructorInsideCref1_VisualBasic()
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
                    </Workspace>, renameTo:="Q")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameStaticConstructorInsideCref2()
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
                    </Workspace>, renameTo:="Q")


            End Using
        End Sub

        <WorkItem(673809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673809")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameStaticConstructorInsideCref2_VisualBasic()
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
                    </Workspace>, renameTo:="Q")


            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInheritanceCref1()
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
                    </Workspace>, renameTo:="y")

            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInheritanceCref1_VisualBasic()
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
                    </Workspace>, renameTo:="y")


                result.AssertLabeledSpansAre("Resolve", "P.y", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(673858, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673858"), WorkItem(666167, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/666167")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameGenericTypeCrefWithConflict()
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
                    </Workspace>, renameTo:="K")


                result.AssertLabeledSpansAre("Resolve", "N.C{int}", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(673858, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673858"), WorkItem(666167, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/666167"), WorkItem(768000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768000")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameGenericTypeCrefWithConflict_VisualBasic()
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
                    </Workspace>, renameTo:="K")


                result.AssertLabeledSpansAre("Resolve", "N.C(Of Integer).D(Of Integer)", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub
#End Region

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameNestedNamespaceToParent1()
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
                    </Workspace>, renameTo:="N")


            End Using
        End Sub

        <WorkItem(673641, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673641")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameNestedNamespaceToParent2()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
namespace N
{
    class C
    {
        void Foo()
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
                    </Workspace>, renameTo:="N")


                result.AssertLabeledSpansAre("Resolve", "global::N.C x;", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <WorkItem(673809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673809")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameStaticConstructor()
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
                    </Workspace>, renameTo:="P")


            End Using
        End Sub

        <WorkItem(675882, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/675882")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub SingleOnlyLocalDeclarationInsideLambdaRecurseInfinitely()
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
                    </Workspace>, renameTo:="xx")


            End Using
        End Sub

        <WorkItem(641231, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/641231")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameDontReplaceBaseConstructorToken_CSharp()
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
                    </Workspace>, renameTo:="P")

            End Using
        End Sub

        <WorkItem(641231, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/641231")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameDontReplaceBaseConstructorToken_VisualBasic()
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
                    </Workspace>, renameTo:="D")

            End Using
        End Sub

        <WorkItem(674762, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674762")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMergedNamespaceAcrossProjects()
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
                    </Workspace>, renameTo:="D")

            End Using
        End Sub

        <WorkItem(674764, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674764")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMergedNamespaceAcrossProjects_1()
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
                    </Workspace>, renameTo:="D")

            End Using
        End Sub

        <WorkItem(716278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716278")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMainWithoutAssertFailureVB()
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
                    </Workspace>, renameTo:="D")

            End Using
        End Sub

        <WorkItem(716278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716278")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMainWithoutAssertFailureCSharp()
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
                </Workspace>, renameTo:="D")

            End Using
        End Sub

        <WorkItem(719062, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/719062")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameEscapedIdentifierToUnescapedIdentifier()
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
                    </Workspace>, renameTo:="D")

            End Using
        End Sub

        <WorkItem(719062, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/719062")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameEscapedIdentifierToUnescapedIdentifierKeyword()
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
                    </Workspace>, renameTo:="do")

                result.AssertLabeledSpecialSpansAre("iden1", "[do]", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WorkItem(767187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767187")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameEscapedTypeNew()
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
                    </Workspace>, renameTo:="D")
            End Using
        End Sub

        <WorkItem(767187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767187")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameConstructorInCref()
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
                    </Workspace>, renameTo:="D")
            End Using
        End Sub

        <WorkItem(1009633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1009633")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameWithTryCatchBlock1()
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
                    </Workspace>, renameTo:="msg2")
            End Using
        End Sub

        <WorkItem(1009633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1009633")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameWithTryCatchBlock2()
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
                    </Workspace>, renameTo:="msg2")
            End Using
        End Sub

#Region "Rename in strings/comments"

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStrings()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, False)
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
                    </Workspace>, renameTo:="NewProgram", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """NewProgram NewProgram!""")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStrings_CSharp()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, False)
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
                    </Workspace>, renameTo:="NewProgram", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """NewProgram NewProgram!""")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInComments()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, False)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
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
                    </Workspace>, renameTo:="NewProgram", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "' NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment3", "' NewProgram")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInComments_CSharp()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, False)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
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
                    </Workspace>, renameTo:="NewProgram", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "// NewProgram PROGRAM! NewProgram!")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInComments_XmlName()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, False)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
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
                    </Workspace>, renameTo:="NewProgram", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment", "NewProgram")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInComments_XmlName2()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, False)
            renamingOptions.Add(RenameOptions.RenameInComments, False)
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
                    </Workspace>, renameTo:="NewProgram", changedOptionSet:=renamingOptions)

                Assert.Equal(1, result.ConflictResolution.RelatedLocations.Count)
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInComments_XmlName_CSharp()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, False)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
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
                    </Workspace>, renameTo:="NewProgram", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment", "NewProgram")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInComments_XmlName_CSharp2()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, False)
            renamingOptions.Add(RenameOptions.RenameInComments, False)
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
                    </Workspace>, renameTo:="NewProgram", changedOptionSet:=renamingOptions)

                Assert.Equal(1, result.ConflictResolution.RelatedLocations.Count)
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStringsAndComments()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
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
                    </Workspace>, renameTo:="NewProgram", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """NewProgram NewProgram!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "' NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment3", "' NewProgram")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStringsAndComments_CSharp()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
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
                    </Workspace>, renameTo:="NewProgram", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """NewProgram NewProgram!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "// NewProgram PROGRAM! NewProgram!")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStringsAndComments_SmallerReplacementString()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
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
                    </Workspace>, renameTo:="P", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """P P!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " P PROGRAM! P!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "' P PROGRAM! P!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment3", "' P")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStringsAndComments_SmallerReplacementString_CSharp()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
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
                    </Workspace>, renameTo:="P", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """P P!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " P PROGRAM! P!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "// P PROGRAM! P!")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStringsAndComments_AnotherSourceFile()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
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
                    </Workspace>, renameTo:="P", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """P P!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " P PROGRAM! P!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "' P PROGRAM! P!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment3", "' P")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStringsAndComments_AnotherSourceFile_CSharp()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
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
                    </Workspace>, renameTo:="NewProgram", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """NewProgram NewProgram!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "// NewProgram PROGRAM! NewProgram!")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStringsAndComments_AnotherProject()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
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
                    </Workspace>, renameTo:="NewProgram", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """NewProgram NewProgram!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "' NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment3", "' NewProgram")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStringsAndComments_AnotherProject_CSharp()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
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
                    </Workspace>, renameTo:="NewProgram", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """NewProgram NewProgram!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", " NewProgram PROGRAM! NewProgram!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "// NewProgram PROGRAM! NewProgram!")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStringsAndComments_AnotherProject2()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
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
                    </Workspace>, renameTo:="NewProgram", changedOptionSet:=renamingOptions)

                Assert.Equal(1, result.ConflictResolution.RelatedLocations.Count)
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStringsAndComments_AnotherProject_CSharp2()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
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
                    </Workspace>, renameTo:="NewProgram", changedOptionSet:=renamingOptions)

                Assert.Equal(1, result.ConflictResolution.RelatedLocations.Count)
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStringsAndComments_WithResolvableConflict()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Module M
    Public Sub [|$$Foo|](Of T)(ByVal x As T) {|RenameInComment1:' Rename Foo to Bar|}
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
        {|stmt1:Foo|}("1")

        {|RenameInComment2:' Foo FOO! Foo!|}
        Dim renamed = {|RenameInString:"Foo Foo!"|}    {|RenameInComment3:' Foo|}
        Dim notRenamed = "FOO! Foo1 FooFoo"
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Bar", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansAre("stmt1", "Call Global.M.Bar(""1"")", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", "' Rename Bar to Bar")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """Bar Bar!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "' Bar FOO! Bar!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment3", "' Bar")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStringsAndComments_WithResolvableConflict_CSharp()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
class Foo
{
    int foo;
    void Blah(int [|$$bar|])
    {
        {|stmt2:foo|} = {|stmt1:bar|};

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
                    </Workspace>, renameTo:="foo", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansAre("stmt1", "this.foo = foo;", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", "this.foo = foo;", RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """foo foo!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment", "// foo BAR! foo!")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStringsAndComments_WithUnresolvableConflict()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
Option Explicit Off
Module Program
    Function [|$$Foo|]
        {|Conflict:Bar|} = 1

        {|RenameInComment1:' Foo FOO! Foo!|}
        Dim renamed = {|RenameInString:"Foo Foo!"|}    {|RenameInComment2:' Foo|}
        Dim notRenamed = "FOO! Foo1 FooFoo"
    End Function
End Module
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Bar", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """Bar Bar!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment1", "' Bar FOO! Bar!")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment2", "' Bar")
            End Using
        End Sub

        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInStringsAndComments_WithUnresolvableConflict_CSharp()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameInStrings, True)
            renamingOptions.Add(RenameOptions.RenameInComments, True)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document><![CDATA[
class {|Conflict:foo|}
{
    int [|$$bar|];

	{|RenameInComment:// bar BAR! bar!|}
	var renamed = {|RenameInString:"bar bar!"|};
	var notRenamed = "BAR! bar1 barbar";
}
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="foo", changedOptionSet:=renamingOptions)

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)

                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInString", """foo foo!""")
                result.AssertLabeledSpansInStringsAndCommentsAre("RenameInComment", "// foo BAR! foo!")
            End Using
        End Sub

#End Region

#Region "Rename In NameOf"

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMethodWithNameof_NoOverloads_CSharp()
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
                    </Workspace>, renameTo:="Mo")

            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMethodWithNameof_WithOverloads_CSharp()
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
                    </Workspace>, renameTo:="Mo")

            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMethodWithNameof_WithOverloads_WithRenameOverloadsOption_CSharp()
            Dim renamingOptions = New Dictionary(Of OptionKey, Object)()
            renamingOptions.Add(RenameOptions.RenameOverloads, True)
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
                    </Workspace>, renameTo:="Mo", changedOptionSet:=renamingOptions)

            End Using
        End Sub
#End Region

    End Class
End Namespace
