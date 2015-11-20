' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    Public Class CallsGraphQueryTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function CallsSimpleTests() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
    class A
    {
        public A() { }
        public void Run() { }
        static void $$Main(string[] args) { new A().Run(); }
    }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithMarkedSymbolNode()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New CallsGraphQuery(), GraphContextDirection.Source)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=A Member=(Name=Main OverloadingParameters=[(@2 Namespace=System Type=(Name=String ArrayRank=1 ParentType=String))]))" Category="CodeSchema_Method" CodeSchemaProperty_IsPrivate="True" CodeSchemaProperty_IsStatic="True" CommonLabel="Main" Icon="Microsoft.VisualStudio.Method.Private" Label="Main"/>
                            <Node Id="(@1 Type=A Member=.ctor)" Category="CodeSchema_Method" CodeSchemaProperty_IsConstructor="True" CodeSchemaProperty_IsPublic="True" CommonLabel="A" Icon="Microsoft.VisualStudio.Method.Public" Label="A"/>
                            <Node Id="(@1 Type=A Member=Run)" Category="CodeSchema_Method" CodeSchemaProperty_IsPublic="True" CommonLabel="Run" Icon="Microsoft.VisualStudio.Method.Public" Label="Run"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=A Member=(Name=Main OverloadingParameters=[(@2 Namespace=System Type=(Name=String ArrayRank=1 ParentType=String))]))" Target="(@1 Type=A Member=.ctor)" Category="CodeSchema_Calls"/>
                            <Link Source="(@1 Type=A Member=(Name=Main OverloadingParameters=[(@2 Namespace=System Type=(Name=String ArrayRank=1 ParentType=String))]))" Target="(@1 Type=A Member=Run)" Category="CodeSchema_Calls"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function CallsLambdaTests() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
    
class A
    {
        static void $$Foo(String[] args)
        {
            int[] numbers = { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };
            int oddNumbers = numbers.Count(n => n % 2 == 1);
        }
    }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithMarkedSymbolNode()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New CallsGraphQuery(), GraphContextDirection.Source)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=A Member=(Name=Foo OverloadingParameters=[(@2 Namespace=System Type=(Name=String ArrayRank=1 ParentType=String))]))" Category="CodeSchema_Method" CodeSchemaProperty_IsPrivate="True" CodeSchemaProperty_IsStatic="True" CommonLabel="Foo" Icon="Microsoft.VisualStudio.Method.Private" Label="Foo"/>
                            <Node Id="(Namespace=System.Linq Type=Enumerable Member=(Name=Count GenericParameterCount=1 OverloadingParameters=[(@2 Namespace=System Type=(Name=Func GenericParameterCount=2 GenericArguments=[(@2 Namespace=System Type=Int32),(@2 Namespace=System Type=Boolean)]))]))" Category="CodeSchema_Method" CodeSchemaProperty_IsExtension="True" CodeSchemaProperty_IsPublic="True" CommonLabel="Count" Icon="Microsoft.VisualStudio.Method.Public" Label="Count"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=A Member=(Name=Foo OverloadingParameters=[(@2 Namespace=System Type=(Name=String ArrayRank=1 ParentType=String))]))" Target="(Namespace=System.Linq Type=Enumerable Member=(Name=Count GenericParameterCount=1 OverloadingParameters=[(@2 Namespace=System Type=(Name=Func GenericParameterCount=2 GenericArguments=[(@2 Namespace=System Type=Int32),(@2 Namespace=System Type=Boolean)]))]))" Category="CodeSchema_Calls"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function CallsPropertiesTests() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
    class A
    {
        static public int Get() { return 1; }
        public int $$PropertyA = A.Get();
    }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithMarkedSymbolNode()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New CallsGraphQuery(), GraphContextDirection.Source)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=A Member=Get)" Category="CodeSchema_Method" CodeSchemaProperty_IsPublic="True" CodeSchemaProperty_IsStatic="True" CommonLabel="Get" Icon="Microsoft.VisualStudio.Method.Public" Label="Get"/>
                            <Node Id="(@1 Type=A Member=PropertyA)" Category="CodeSchema_Field" CodeSchemaProperty_IsPublic="True" CommonLabel="PropertyA" Icon="Microsoft.VisualStudio.Field.Public" Label="PropertyA"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=A Member=PropertyA)" Target="(@1 Type=A Member=Get)" Category="CodeSchema_Calls"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function CallsDelegatesTests() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

delegate void MyDelegate1(int x, float y);

class C
{
    public void DelegatedMethod(int x, float y = 3.0f) { System.Console.WriteLine(y); }
    static void $$Main(string[] args)
    {
        C mc = new C();
        MyDelegate1 md1 = null;
        md1 += mc.DelegatedMethod;
        md1(1, 5);
        md1 -= mc.DelegatedMethod;
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithMarkedSymbolNode()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New CallsGraphQuery(), GraphContextDirection.Source)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=C Member=(Name=DelegatedMethod OverloadingParameters=[(@2 Namespace=System Type=Int32),(@2 Namespace=System Type=Single)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsPublic="True" CommonLabel="DelegatedMethod" Icon="Microsoft.VisualStudio.Method.Public" Label="DelegatedMethod"/>
                            <Node Id="(@1 Type=C Member=(Name=Main OverloadingParameters=[(@2 Namespace=System Type=(Name=String ArrayRank=1 ParentType=String))]))" Category="CodeSchema_Method" CodeSchemaProperty_IsPrivate="True" CodeSchemaProperty_IsStatic="True" CommonLabel="Main" Icon="Microsoft.VisualStudio.Method.Private" Label="Main"/>
                            <Node Id="(@1 Type=C Member=.ctor)" Category="CodeSchema_Method" CodeSchemaProperty_IsConstructor="True" CodeSchemaProperty_IsPublic="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Method.Public" Label="C"/>
                            <Node Id="(@1 Type=MyDelegate1 Member=(Name=Invoke OverloadingParameters=[(@2 Namespace=System Type=Int32),(@2 Namespace=System Type=Single)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsPublic="True" CodeSchemaProperty_IsVirtual="True" CommonLabel="Invoke" Icon="Microsoft.VisualStudio.Method.Public" Label="Invoke"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=C Member=(Name=Main OverloadingParameters=[(@2 Namespace=System Type=(Name=String ArrayRank=1 ParentType=String))]))" Target="(@1 Type=C Member=(Name=DelegatedMethod OverloadingParameters=[(@2 Namespace=System Type=Int32),(@2 Namespace=System Type=Single)]))" Category="CodeSchema_Calls"/>
                            <Link Source="(@1 Type=C Member=(Name=Main OverloadingParameters=[(@2 Namespace=System Type=(Name=String ArrayRank=1 ParentType=String))]))" Target="(@1 Type=C Member=.ctor)" Category="CodeSchema_Calls"/>
                            <Link Source="(@1 Type=C Member=(Name=Main OverloadingParameters=[(@2 Namespace=System Type=(Name=String ArrayRank=1 ParentType=String))]))" Target="(@1 Type=MyDelegate1 Member=(Name=Invoke OverloadingParameters=[(@2 Namespace=System Type=Int32),(@2 Namespace=System Type=Single)]))" Category="CodeSchema_Calls"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function CallsDelegateCreationExpressionTests() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

delegate void MyEvent();

class Test
{
    event MyEvent Clicked;
    void Handler() { }

    public void $$Run()
    {
        Test t = new Test();
        t.Clicked += new MyEvent(Handler);
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithMarkedSymbolNode()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New CallsGraphQuery(), GraphContextDirection.Source)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Test Member=.ctor)" Category="CodeSchema_Method" CodeSchemaProperty_IsConstructor="True" CodeSchemaProperty_IsPublic="True" CommonLabel="Test" Icon="Microsoft.VisualStudio.Method.Public" Label="Test"/>
                            <Node Id="(@1 Type=Test Member=Handler)" Category="CodeSchema_Method" CodeSchemaProperty_IsPrivate="True" CommonLabel="Handler" Icon="Microsoft.VisualStudio.Method.Private" Label="Handler"/>
                            <Node Id="(@1 Type=Test Member=Run)" Category="CodeSchema_Method" CodeSchemaProperty_IsPublic="True" CommonLabel="Run" Icon="Microsoft.VisualStudio.Method.Public" Label="Run"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=Test Member=Run)" Target="(@1 Type=Test Member=.ctor)" Category="CodeSchema_Calls"/>
                            <Link Source="(@1 Type=Test Member=Run)" Target="(@1 Type=Test Member=Handler)" Category="CodeSchema_Calls"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function
    End Class
End Namespace
