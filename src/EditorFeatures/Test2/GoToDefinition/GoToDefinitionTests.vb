' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.CSharp.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToDefinition
    Public Class GoToDefinitionTests
        Private Function TestAsync(workspaceDefinition As XElement, Optional expectedResult As Boolean = True) As Tasks.Task
            Return GoToTestHelpers.TestAsync(workspaceDefinition, expectedResult,
                Function(document As Document, cursorPosition As Integer, presenters As IEnumerable(Of Lazy(Of INavigableItemsPresenter)), externalDefinitionProviders As IEnumerable(Of Lazy(Of INavigableDefinitionProvider)))
                    Dim goToDefService = If(document.Project.Language = LanguageNames.CSharp,
                        DirectCast(New CSharpGoToDefinitionService(presenters, externalDefinitionProviders), IGoToDefinitionService),
                        New VisualBasicGoToDefinitionService(presenters, externalDefinitionProviders))

                    Return goToDefService.TryGoToDefinition(document, cursorPosition, CancellationToken.None)
                End Function)
        End Function

#Region "P2P Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestP2PClassReference() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>VBAssembly</ProjectReference>
        <Document>
        using N;

        class CSharpClass
        {
            VBCl$$ass vb
        }
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <Document>
        namespace N
            public class [|VBClass|]
            End Class
        End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

#End Region

#Region "Normal CSharp Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGoToDefinition() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|] { }
            class OtherClass { Some$$Class obj; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGoToDefinitionSameClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|] { Some$$Class someObject; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGoToDefinitionNestedClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Outer
            {
              class [|Inner|]
              {
              }

              In$$ner someObj;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionDifferentFiles() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class OtherClass { SomeClass obj; }
        </Document>
        <Document>
            class OtherClass2 { Some$$Class obj2; };
        </Document>
        <Document>
            class [|SomeClass|] { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionPartialClasses() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            partial class nothing { };
        </Document>
        <Document>
            partial class [|OtherClass|] { int a; }
        </Document>
        <Document>
            partial class [|OtherClass|] { int b; };
        </Document>
        <Document>
            class ConsumingClass { Other$$Class obj; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|] { int x; };
        </Document>
        <Document>
            class ConsumingClass
            {
                void foo()
                {
                    Some$$Class x;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(900438, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/900438")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionPartialMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            partial class Test
            {
                partial void M();
            }
        </Document>
        <Document>
            partial class Test
            {
                void Foo()
                {
                    var t = new Test();
                    t.M$$();
                }

                partial void [|M|]()
                {
                    throw new NotImplementedException();
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionOnMethodCall1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void [|M|]() { }
                void M(int i) { }
                void M(int i, string s) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M();
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionOnMethodCall2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void M() { }
                void [|M|](int i, string s) { }
                void M(int i) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M(0, "text");
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionOnMethodCall3() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void M() { }
                void M(int i, string s) { }
                void [|M|](int i) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M(0);
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionOnMethodCall4() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void M() { }
                void M(int i, string s) { }
                void M(int i) { }
                void [|M|](string s, int i) { }

                void Call()
                {
                    $$M("text", 0);
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionOnConstructor1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|C|]
            {
                C() { }

                $$C c = new C();
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(3376, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionOnConstructor2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                [|C|]() { }

                C c = new $$C();
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionWithoutExplicitConstruct() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|C|]
            {
                void Method()
                {
                    C c = new $$C();
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionOnLocalVariable1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void method()
                {
                    int [|x|] = 2, y, z = $$x * 2;
                    y = 10;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionOnLocalVariable2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void method()
                {
                    int x = 2, [|y|], z = x * 2;
                    $$y = 10;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionOnLocalField() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                int [|_X|] = 1, _Y;
                void method()
                {
                    _$$X = 8;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionOnAttributeClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            [FlagsAttribute]
            class [|C|]
            {
                $$C c;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionTouchLeft() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|]
            {
                $$SomeClass c;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionTouchRight() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|]
            {
                SomeClass$$ c;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionOnGenericTypeParameterInPresenceOfInheritedNestedTypeWithSameName() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            class B
            {
                public class T { }
            }
            class C<[|T|]> : B
            {
                $$T x;
            }]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(538765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538765")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGotoDefinitionThroughOddlyNamedType() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            class [|dynamic|] { }
            class C : dy$$namic { }
        ]]></Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGoToDefinitionOnConstructorInitializer1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private int v;
    public Program() : $$this(4)
    {
    }

    public [|Program|](int v)
    {
        this.v = v;
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGoToDefinitionOnExtensionMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
           class Program
           {
               static void Main(string[] args)
               {
                    "1".$$TestExt();
               }
           }

           public static class Ex
           {
              public static void TestExt<T>(this T ex) { }
              public static void [|TestExt|](this string ex) { }
           }]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(542004, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542004")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpTestLambdaParameter() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    delegate int D2(int i, int j);
    static void Main()
    {
        D2 d = (int [|i1|], int i2) => { return $$i1 + i2; };
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpTestLabel() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    void M()
    {
    [|Foo|]:
        int Foo;
        goto $$Foo;
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpGoToDefinitionFromCref() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            /// <see cref="$$SomeClass"/>
            class [|SomeClass|] 
            { 
            }]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

#End Region

#Region "CSharp Venus Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpVenusGotoDefinition() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            #line 1 "CSForm1.aspx"
            public class [|_Default|]
            {
               _Defa$$ult a;
            #line default
            #line hidden
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(545324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545324")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpFilterGotoDefResultsFromHiddenCodeForUIPresenters() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            public class [|_Default|]
            {
            #line 1 "CSForm1.aspx"
               _Defa$$ult a;
            #line default
            #line hidden
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(545324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545324")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpDoNotFilterGotoDefResultsFromHiddenCodeForApis() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            public class [|_Default|]
            {
            #line 1 "CSForm1.aspx"
               _Defa$$ult a;
            #line default
            #line hidden
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

#End Region

#Region "CSharp Script Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpScriptGoToDefinition() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class [|SomeClass|] { }
            class OtherClass { Some$$Class obj; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpScriptGoToDefinitionSameClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class [|SomeClass|] { Some$$Class someObject; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpScriptGoToDefinitionNestedClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class Outer
            {
              class [|Inner|]
              {
              }

              In$$ner someObj;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpScriptGotoDefinitionDifferentFiles() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class OtherClass { SomeClass obj; }
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            class OtherClass2 { Some$$Class obj2; };
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            class [|SomeClass|] { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpScriptGotoDefinitionPartialClasses() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            partial class nothing { };
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            partial class [|OtherClass|] { int a; }
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            partial class [|OtherClass|] { int b; };
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            class ConsumingClass { Other$$Class obj; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpScriptGotoDefinitionMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class [|SomeClass|] { int x; };
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            class ConsumingClass
            {
                void foo()
                {
                    Some$$Class x;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpScriptGotoDefinitionOnMethodCall1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class C
            {
                void [|M|]() { }
                void M(int i) { }
                void M(int i, string s) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M();
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpScriptGotoDefinitionOnMethodCall2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class C
            {
                void M() { }
                void [|M|](int i, string s) { }
                void M(int i) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M(0, "text");
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpScriptGotoDefinitionOnMethodCall3() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class C
            {
                void M() { }
                void M(int i, string s) { }
                void [|M|](int i) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M(0);
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpScriptGotoDefinitionOnMethodCall4() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class C
            {
                void M() { }
                void M(int i, string s) { }
                void M(int i) { }
                void [|M|](string s, int i) { }

                void Call()
                {
                    $$M("text", 0);
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(989476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/989476")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpPreferNongeneratedSourceLocations() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document FilePath="Nongenerated.cs">
partial class [|C|]
{
    void M() 
    { 
        $$C c;
    }
}
        </Document>
        <Document FilePath="Generated.g.i.cs">
partial class C
{
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(workspace)
        End Function

        <WorkItem(989476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/989476")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpUseGeneratedSourceLocationsIfNoNongeneratedLocationsAvailable() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document FilePath="Generated.g.i.cs">
class [|C|]
{
}
        </Document>
        <Document FilePath="Nongenerated.g.i.cs">
class D
{
    void M()
    {
        $$C c;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(workspace)
        End Function

#End Region

#Region "Normal Visual Basic Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicGoToDefinition() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
            End Class
            Class OtherClass
                Dim obj As Some$$Class
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(541105, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541105")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicPropertyBackingField() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Property [|P|] As Integer
    Sub M()
          Me.$$_P = 10
    End Sub
End Class 
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicGoToDefinitionSameClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
                Dim obj As Some$$Class
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicGoToDefinitionNestedClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Outer
                Class [|Inner|]
                End Class
                Dim obj as In$$ner
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicGotoDefinitionDifferentFiles() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class OtherClass
                Dim obj As SomeClass 
            End Class
        </Document>
        <Document>
            Class OtherClass2
                Dim obj As Some$$Class
            End Class
        </Document>
        <Document>
            Class [|SomeClass|] 
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicGotoDefinitionPartialClasses() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            DummyClass 
            End Class
        </Document>
        <Document>
            Partial Class [|OtherClass|]
                Dim a As Integer 
            End Class
        </Document>
        <Document>
            Partial Class [|OtherClass|]
                Dim b As Integer 
            End Class
        </Document>
        <Document>
            Class ConsumingClass
                Dim obj As Other$$Class 
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicGotoDefinitionMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
                Dim x As Integer 
            End Class
        </Document>
        <Document>
            Class ConsumingClass
                Sub foo()
                    Dim obj As Some$$Class
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(900438, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/900438")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicGotoDefinitionPartialMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Partial Class Customer
                Private Sub [|OnNameChanged|]()

                End Sub
            End Class
        </Document>
        <Document>
            Partial Class Customer
                Sub New()
                    Dim x As New Customer()
                    x.OnNameChanged$$()
                End Sub
                Partial Private Sub OnNameChanged()

                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicTouchLeft() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
                Dim x As Integer 
            End Class
        </Document>
        <Document>
            Class ConsumingClass
                Sub foo()
                    Dim obj As $$SomeClass
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicTouchRight() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
                Dim x As Integer 
            End Class
        </Document>
        <Document>
            Class ConsumingClass
                Sub foo()
                    Dim obj As SomeClass$$
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(542872, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542872")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicMe() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class B
    Sub New()
    End Sub
End Class
 
Class [|C|]
    Inherits B
 
    Sub New()
        MyBase.New()
        MyClass.Foo()
        $$Me.Bar()
    End Sub
 
    Private Sub Bar()
    End Sub
 
    Private Sub Foo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(542872, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542872")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicMyClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class B
    Sub New()
    End Sub
End Class
 
Class [|C|]
    Inherits B
 
    Sub New()
        MyBase.New()
        $$MyClass.Foo()
        Me.Bar()
    End Sub
 
    Private Sub Bar()
    End Sub
 
    Private Sub Foo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(542872, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542872")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicMyBase() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class [|B|]
    Sub New()
    End Sub
End Class
 
Class C
    Inherits B
 
    Sub New()
        $$MyBase.New()
        MyClass.Foo()
        Me.Bar()
    End Sub
 
    Private Sub Bar()
    End Sub
 
    Private Sub Foo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

#End Region

#Region "Venus Visual Basic Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicVenusGotoDefinition() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            #ExternalSource ("Default.aspx", 1)
            Class [|Program|]
                Sub Main(args As String())
                    Dim f As New Pro$$gram()
                End Sub
            End Class
            #End ExternalSource
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(545324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545324")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicFilterGotoDefResultsFromHiddenCodeForUIPresenters() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|Program|]
                Sub Main(args As String())
            #ExternalSource ("Default.aspx", 1)
                    Dim f As New Pro$$gram()
                End Sub
            End Class
            #End ExternalSource
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(545324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545324")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicDoNotFilterGotoDefResultsFromHiddenCodeForApis() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|Program|]
                Sub Main(args As String())
            #ExternalSource ("Default.aspx", 1)
                    Dim f As New Pro$$gram()
                End Sub
            End Class
            #End ExternalSource
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function
#End Region

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicTestThroughExecuteCommand() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
                Dim x As Integer 
            End Class
        </Document>
        <Document>
            Class ConsumingClass
                Sub foo()
                    Dim obj As SomeClass$$
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicGoToDefinitionOnExtensionMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Class Program
    Private Shared Sub Main(args As String())
        Dim i As String = "1"
        i.Test$$Ext()
    End Sub
End Class

Module Ex
    <System.Runtime.CompilerServices.Extension()>
    Public Sub TestExt(Of T)(ex As T)
    End Sub
    <System.Runtime.CompilerServices.Extension()>
    Public Sub [|TestExt|](ex As string)
    End Sub
End Module]]>]
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(542220, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542220")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpTestAliasAndTarget1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using [|AliasedSomething|] = X.Something;
 
namespace X
{
    class Something { public Something() { } }
}
 
class Program
{
    static void Main(string[] args)
    {
        $$AliasedSomething x = new AliasedSomething();
        X.Something y = new X.Something();
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(542220, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542220")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpTestAliasAndTarget2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using [|AliasedSomething|] = X.Something;
 
namespace X
{
    class Something { public Something() { } }
}
 
class Program
{
    static void Main(string[] args)
    {
        AliasedSomething x = new $$AliasedSomething();
        X.Something y = new X.Something();
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(542220, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542220")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpTestAliasAndTarget3() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using AliasedSomething = X.Something;
 
namespace X
{
    class [|Something|] { public Something() { } }
}
 
class Program
{
    static void Main(string[] args)
    {
        AliasedSomething x = new AliasedSomething();
        X.$$Something y = new X.Something();
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(542220, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542220")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCSharpTestAliasAndTarget4() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using AliasedSomething = X.Something;
 
namespace X
{
    class Something { public [|Something|]() { } }
}
 
class Program
{
    static void Main(string[] args)
    {
        AliasedSomething x = new AliasedSomething();
        X.Something y = new X.$$Something();
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(543218, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543218")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicQueryRangeVariable() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim arr = New Integer() {4, 5}
        Dim q3 = From [|num|] In arr Select $$num
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(529060, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529060")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestVisualBasicGotoConstant() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module M
    Sub Main()
label1: GoTo $$200
[|200|]:    GoTo label1
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(545661, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545661")>
        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/10132"), Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCrossLanguageParameterizedPropertyOverride() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj">
        <Document>
Public Class A
    Public Overridable ReadOnly Property X(y As Integer) As Integer
        [|Get|]
        End Get
    End Property
End Class
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>VBProj</ProjectReference>
        <Document>
class B : A
{
    public override int get_X(int y)
    {
        return base.$$get_X(y);
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(866094, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/866094")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestCrossLanguageNavigationToVBModuleMember() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj">
        <Document>
Public Module A
    Public Sub [|M|]()
    End Sub
End Module
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>VBProj</ProjectReference>
        <Document>
class C
{
    static void N()
    {
        A.$$M();
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

#Region "Show notification tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestShowNotificationVB() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class SomeClass
            End Class
            Cl$$ass OtherClass
                Dim obj As SomeClass
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, expectedResult:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestShowNotificationCS() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class SomeClass { }
            cl$$ass OtherClass
            {
                SomeClass obj;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, expectedResult:=False)
        End Function

        <WorkItem(546341, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546341")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestGoToDefinitionOnGlobalKeyword() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                gl$$obal::System.String s;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, expectedResult:=False)
        End Function

        <WorkItem(902119, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/902119")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestGoToDefinitionOnInferredFieldInitializer() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Public Class Class2
    Sub Test()
        Dim var1 = New With {Key .var2 = "Bob", Class2.va$$r3}
    End Sub
 
    Shared Property [|var3|]() As Integer
        Get
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property
End Class

        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(885151, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/885151")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Async Function TestGoToDefinitionGlobalImportAlias() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <ProjectReference>VBAssembly</ProjectReference>
        <CompilationOptions>
            <GlobalImport>Foo = Importable.ImportMe</GlobalImport>
        </CompilationOptions>
        <Document>
Public Class Class2
    Sub Test()
        Dim x as Fo$$o
    End Sub
End Class

        </Document>
    </Project>

    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBAssembly">
        <Document>
Namespace Importable
    Public Class [|ImportMe|]
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function
#End Region

    End Class
End Namespace
