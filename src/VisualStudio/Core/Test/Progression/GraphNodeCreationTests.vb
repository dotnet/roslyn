' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Microsoft.VisualStudio.LanguageServices.Progression
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    Public Class GraphNodeCreationTests
        Private Async Function AssertCreatedNodeIsAsync(code As String, expectedId As String, xml As XElement, Optional language As String = "C#") As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                <Workspace>
                    <Project Language=<%= language %> CommonReferences="true" FilePath="Z:\Project.csproj">
                        <Document FilePath="Z:\Project.cs">
                            <%= code %>
                        </Document>
                    </Project>
                </Workspace>)
                Dim symbol = testState.GetMarkedSymbol()
                Dim node = GraphNodeCreation.CreateNodeIdAsync(symbol, testState.GetSolution(), CancellationToken.None).Result
                Assert.Equal(expectedId, node.ToString())

                Dim graph = New Graph()
                Await graph.CreateNodeAsync(symbol, testState.GetSolution(), CancellationToken.None)
                AssertSimplifiedGraphIs(graph, xml)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestSimpleType() As Task
            Await AssertCreatedNodeIsAsync("namespace N { class $$C { } }", "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C)",
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestNamespaceType() As Task
            Await AssertCreatedNodeIsAsync("namespace $$N { class C { } }", "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N)",
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N)" Category="CodeSchema_Namespace" CodeSchemaProperty_IsPublic="True" CodeSchemaProperty_IsStatic="True" CommonLabel="N" Label="N"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestLongNamespaceType() As Task
            Await AssertCreatedNodeIsAsync("namespace N.$$N1.N11 { class C { } }", "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N.N1)",
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N.N1)" Category="CodeSchema_Namespace" CodeSchemaProperty_IsPublic="True" CodeSchemaProperty_IsStatic="True" CommonLabel="N.N1" Label="N.N1"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestSimpleParameterType() As Task
            Await AssertCreatedNodeIsAsync("namespace N { class C { void M(int $$x) { } } }",
                    "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=(Name=M OverloadingParameters=[(Assembly=file:///Z:/FxReferenceAssembliesUri Namespace=System Type=Int32)]) ParameterIdentifier=x)",
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N Type=C Member=(Name=M OverloadingParameters=[(@2 Namespace=System Type=Int32)]) ParameterIdentifier=x)" Category="CodeSchema_Parameter" CodeSchemaProperty_IsByReference="False" CodeSchemaProperty_IsOut="False" CodeSchemaProperty_IsParameterArray="False" CommonLabel="x" Label="x"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestDelegateType() As Task
            Await AssertCreatedNodeIsAsync("namespace N { delegate void D(string $$m); }",
                    "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=D Member=(Name=Invoke OverloadingParameters=[(Assembly=file:///Z:/FxReferenceAssembliesUri Namespace=System Type=String)]) ParameterIdentifier=m)",
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N Type=D Member=(Name=Invoke OverloadingParameters=[(@2 Namespace=System Type=String)]) ParameterIdentifier=m)" Category="CodeSchema_Parameter" CodeSchemaProperty_IsByReference="False" CodeSchemaProperty_IsOut="False" CodeSchemaProperty_IsParameterArray="False" CommonLabel="m" Label="m"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestLambdaParameterType() As Task
            Await AssertCreatedNodeIsAsync("namespace N { class C { void M(Func<int,int> $$x) { } } }",
                    "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=(Name=M OverloadingParameters=[(Assembly=file:///Z:/CSharpAssembly1.dll Type=(Name=Func GenericParameterCount=2 GenericArguments=[(Assembly=file:///Z:/FxReferenceAssembliesUri Namespace=System Type=Int32),(Assembly=file:///Z:/FxReferenceAssembliesUri Namespace=System Type=Int32)]))]) ParameterIdentifier=x)",
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N Type=C Member=(Name=M OverloadingParameters=[(@1 Type=(Name=Func GenericParameterCount=2 GenericArguments=[(@2 Namespace=System Type=Int32),(@2 Namespace=System Type=Int32)]))]) ParameterIdentifier=x)" Category="CodeSchema_Parameter" CodeSchemaProperty_IsByReference="False" CodeSchemaProperty_IsOut="False" CodeSchemaProperty_IsParameterArray="False" CommonLabel="x" Label="x"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestLocalType() As Task
            Await AssertCreatedNodeIsAsync("namespace N { class C { int M() { int $$y = 0; return y; } } }", "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=M LocalVariable=y)",
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N Type=C Member=M LocalVariable=y)" Category="CodeSchema_LocalExpression" CommonLabel="y" Label="y"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestFirstLocalWithSameNameType() As Task
            Await AssertCreatedNodeIsAsync("namespace N { class C { int M() { { int $$y = 0; } { int y = 1;} } } }", "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=M LocalVariable=y)",
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N Type=C Member=M LocalVariable=y)" Category="CodeSchema_LocalExpression" CommonLabel="y" Label="y"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestSecondLocalWithSameNameType() As Task
            Await AssertCreatedNodeIsAsync("namespace N { class C { int M() { { int y = 0; } { int $$y = 1;} } } }",
                    "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=M LocalVariable=y LocalVariableIndex=1)",
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N Type=C Member=M LocalVariable=y LocalVariableIndex=1)" Category="CodeSchema_LocalExpression" CommonLabel="y" Label="y"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestErrorType() As Task
            Await AssertCreatedNodeIsAsync(
                "Class $$C : Inherits D : End Class",
                "(Assembly=file:///Z:/VisualBasicAssembly1.dll Type=C)",
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/VisualBasicAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>,
            LanguageNames.VisualBasic)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestSimpleMethodSymbolTest() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs"><![CDATA[[
                                class C { 
                                    static void $$Foo(string[] args) {}
                                }
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Dim graphNode = testState.GetGraphWithMarkedSymbolNode().Nodes.Single()
                Dim formattedLabelExtension As New GraphFormattedLabelExtension()
                Assert.Equal("Foo(string[]) : void", formattedLabelExtension.Label(graphNode, GraphCommandDefinition.Contains.Id))
                Assert.Equal("Foo", graphNode.Label)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestReferenceParameterSymbolTest() As Task
            Await AssertCreatedNodeIsAsync("namespace N { class C { void $$Foo(ref int i) { i = i + 1; } } }",
                    "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=(Name=Foo OverloadingParameters=[(Assembly=file:///Z:/FxReferenceAssembliesUri Namespace=System Type=Int32 ParamKind=Ref)]))",
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N Type=C Member=(Name=Foo OverloadingParameters=[(@2 Namespace=System Type=Int32 ParamKind=Ref)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsPrivate="True" CommonLabel="Foo" Icon="Microsoft.VisualStudio.Method.Private" Label="Foo"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestReferenceOutParameterSymbolTest() As Task
            Await AssertCreatedNodeIsAsync("namespace N { class C { void $$Foo(out int i) { i = 1; } } }",
                    "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=(Name=Foo OverloadingParameters=[(Assembly=file:///Z:/FxReferenceAssembliesUri Namespace=System Type=Int32 ParamKind=Ref)]))",
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N Type=C Member=(Name=Foo OverloadingParameters=[(@2 Namespace=System Type=Int32 ParamKind=Ref)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsPrivate="True" CommonLabel="Foo" Icon="Microsoft.VisualStudio.Method.Private" Label="Foo"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestSimpleIndexerTest() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs"><![CDATA[[
                                namespace N
                                {
                                    class A {
                                        public string $$this[int i, int j, int k]
                                        {
                                            get { return ""; }
                                            set { }
                                        }
                                    }
                                }
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Dim symbol = testState.GetMarkedSymbol()
                Dim graph = New Graph()
                Await graph.CreateNodeAsync(symbol, testState.GetSolution(), CancellationToken.None)
                AssertSimplifiedGraphIs(graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N Type=A Member=(Name=Item OverloadingParameters=[(@2 Namespace=System Type=Int32),(@2 Namespace=System Type=Int32),(@2 Namespace=System Type=Int32)]))" Category="CodeSchema_Property" CodeSchemaProperty_IsPublic="True" CommonLabel="this" Icon="Microsoft.VisualStudio.Property.Public" Label="this"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestAttributedIndexerTest() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs"><![CDATA[[
                                namespace N
                                {
                                    class A {
                                        [System.Runtime.CompilerServices.IndexerNameAttribute("AAA")]
                                        public string $$this[int i, int j, int k]
                                        {
                                            get { return ""; }
                                            set { }
                                        }
                                    }
                                }
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Dim symbol = testState.GetMarkedSymbol()
                Dim graph = New Graph()
                Await graph.CreateNodeAsync(symbol, testState.GetSolution(), CancellationToken.None)
                AssertSimplifiedGraphIs(graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N Type=A Member=(Name=AAA OverloadingParameters=[(@2 Namespace=System Type=Int32),(@2 Namespace=System Type=Int32),(@2 Namespace=System Type=Int32)]))" Category="CodeSchema_Property" CodeSchemaProperty_IsPublic="True" CommonLabel="this" Icon="Microsoft.VisualStudio.Property.Public" Label="this"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestParameterWithConversionOperatorTest() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs"><![CDATA[[
                                namespace N
                                {
                                    class A {
                                        public static explicit operator $$int(int x) { return x; }
                                    }
                                }
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Dim symbol = testState.GetMarkedSymbol()
                Dim graph = New Graph()
                Await graph.CreateNodeAsync(symbol, testState.GetSolution(), CancellationToken.None)
                AssertSimplifiedGraphIs(graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N Type=A Member=(Name=op_Explicit OverloadingParameters=[(@2 Namespace=System Type=Int32),(@2 Namespace=System Type=Int32 ParamKind=Return)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsOperator="True" CodeSchemaProperty_IsPublic="True" CodeSchemaProperty_IsStatic="True" CommonLabel="op_Explicit" Icon="Microsoft.VisualStudio.Operator.Public" Label="op_Explicit"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestLocalVBVariableType() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb"><![CDATA[[
Module Module1 

    Sub Main()
        Dim $$x As Integer = 3
    End Sub

End Module
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Dim symbol = testState.GetMarkedSymbol()
                Dim graph = New Graph()
                Await graph.CreateNodeAsync(symbol, testState.GetSolution(), CancellationToken.None)
                AssertSimplifiedGraphIs(graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Module1 Member=Main LocalVariable=x)" Category="CodeSchema_LocalExpression" CommonLabel="x" Label="x"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/VisualBasicAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestLocalVBRangeTypeVariable() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb"><![CDATA[[
Module Module1 

    Sub Main()
        Dim wordQuery = From $$word In New List(Of Integer)() Select word
    End Sub

End Module
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Dim symbol = testState.GetMarkedSymbol()
                Dim graph = New Graph()
                Await graph.CreateNodeAsync(symbol, testState.GetSolution(), CancellationToken.None)
                AssertSimplifiedGraphIs(graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Module1 Member=Main LocalVariable=word)" Category="CodeSchema_LocalExpression" CommonLabel="word" Label="word"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/VisualBasicAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestLocalVBVariableWithinBlockType() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb"><![CDATA[[
Module Module1 

    Sub Main()
        if true then
            Dim $$x As Integer = 3
        else
            Dim x as Integer = 4
        endif
    End Sub

End Module
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Dim symbol = testState.GetMarkedSymbol()
                Dim graph = New Graph()
                Await graph.CreateNodeAsync(symbol, testState.GetSolution(), CancellationToken.None)
                AssertSimplifiedGraphIs(graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Module1 Member=Main LocalVariable=x)" Category="CodeSchema_LocalExpression" CommonLabel="x" Label="x"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/VisualBasicAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestLocalVariableIndexTest() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb"><![CDATA[[
Module Module1 

    Sub Main()
        if true then
            Dim x As Integer = 3
        else
            Dim $$x as Integer = 4
        endif
    End Sub

End Module
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Dim symbol = testState.GetMarkedSymbol()
                Dim graph = New Graph()
                Await graph.CreateNodeAsync(symbol, testState.GetSolution(), CancellationToken.None)
                AssertSimplifiedGraphIs(graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Module1 Member=Main LocalVariable=x LocalVariableIndex=1)" Category="CodeSchema_LocalExpression" CommonLabel="x" Label="x"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/VisualBasicAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestGenericArgumentsTest() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs"><![CDATA[[
                                namespace N
                                {
                                    public class MgtpTestOuterClass<T1, T2>
                                    {
                                        public class MgtpTestInnerClass<T3, T4>
                                        {
                                            public void MgtpTestMethod<T5, T6>(
                                                T1 p1,
                                                T2[][,] p2,
                                                T3 p3,
                                                T4[][,] p4,
                                                T5 p5,
                                                MgtpTestOuterClass<T3, $$MgtpTestInnerClass<T1, T6[][,]>> p6)
                                            {
                                            }

                                            public void MgtpTestMethod2<T5, T6>(
                                                T1 p1,
                                                T2[][,] p2,
                                                T3 p3,
                                                T4[][,] p4,
                                                T5 p5,
                                                MgtpTestOuterClass<T3, MgtpTestInnerClass<T3, T4>> p6)
                                            {
                                            }
                                        }
                                    }
                                }
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Dim symbol = testState.GetMarkedSymbol()
                Dim graph = New Graph()
                Await graph.CreateNodeAsync(symbol, testState.GetSolution(), CancellationToken.None)
                AssertSimplifiedGraphIs(graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N Type=(Name=MgtpTestInnerClass GenericParameterCount=2 GenericArguments=[(@1 Namespace=N Type=(Name=MgtpTestOuterClass GenericParameterCount=2) ParameterIdentifier=0),(@1 Namespace=N Type=(ArrayRank=1 ParentType=(ArrayRank=2 ParentType=(ParameterIdentifier=1))))] ParentType=(Name=MgtpTestOuterClass GenericParameterCount=2 GenericArguments=[(@1 Namespace=N Type=(Name=MgtpTestOuterClass GenericParameterCount=2) ParameterIdentifier=0),(@1 Namespace=N Type=(Name=MgtpTestOuterClass GenericParameterCount=2) ParameterIdentifier=1)])))" Category="CodeSchema_Class" CodeSchemaProperty_IsPublic="True" CommonLabel="MgtpTestInnerClass&lt;T3, T4&gt;" Icon="Microsoft.VisualStudio.Class.Public" Label="MgtpTestInnerClass&lt;T1, T6[][,]&gt;"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestGenericArgumentsTest2() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs"><![CDATA[[
                                namespace N
                                {
                                    public class MgtpTestOuterClass<T1, T2>
                                    {
                                        public class MgtpTestInnerClass<T3, T4>
                                        {
                                            public void MgtpTestMethod<T5, T6>(
                                                T1 p1,
                                                T2[][,] p2,
                                                T3 p3,
                                                T4[][,] p4,
                                                T5 p5,
                                                MgtpTestOuterClass<T3, MgtpTestInnerClass<T1, T6[][,]>> p6)
                                            {
                                            }

                                            public void MgtpTestMethod2<T5, T6>(
                                                T1 p1,
                                                T2[][,] p2,
                                                T3 p3,
                                                T4[][,] p4,
                                                T5 p5,
                                                MgtpTestOuterClass<T3, $$MgtpTestInnerClass<T3, T4>> p6)
                                            {
                                            }
                                        }
                                    }
                                }
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Dim symbol = testState.GetMarkedSymbol()
                Dim graph = New Graph()
                Await graph.CreateNodeAsync(symbol, testState.GetSolution(), CancellationToken.None)
                AssertSimplifiedGraphIs(graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Namespace=N Type=(Name=MgtpTestInnerClass GenericParameterCount=2 ParentType=(Name=MgtpTestOuterClass GenericParameterCount=2)))" Category="CodeSchema_Class" CodeSchemaProperty_IsPublic="True" CommonLabel="MgtpTestInnerClass&lt;T3, T4&gt;" Icon="Microsoft.VisualStudio.Class.Public" Label="MgtpTestInnerClass&lt;T3, T4&gt;"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestGenericCSharpMethodSymbolTest() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs"><![CDATA[[
                                class C<T,K> { 
                                    void $$Foo<T,K>() {}
                                }
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Dim graphNode = testState.GetGraphWithMarkedSymbolNode().Nodes.Single()
                Dim formattedLabelExtension As New GraphFormattedLabelExtension()
                Assert.Equal("Foo<T, K>() : void", formattedLabelExtension.Label(graphNode, GraphCommandDefinition.Contains.Id))
                Assert.Equal("Foo", graphNode.Label)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestGenericCSharpTypeSymbolTest() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs"><![CDATA[[
                                class $$C<T> {                                    
                                }
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Dim graphNode = testState.GetGraphWithMarkedSymbolNode().Nodes.Single()
                Dim formattedLabelExtension As New GraphFormattedLabelExtension()
                Assert.Equal("C<T>", formattedLabelExtension.Label(graphNode, GraphCommandDefinition.Contains.Id))
                Assert.Equal("C<T>", graphNode.Label)

                Dim symbol = testState.GetMarkedSymbol()
                Dim graph = New Graph()
                Await graph.CreateNodeAsync(symbol, testState.GetSolution(), CancellationToken.None)
                AssertSimplifiedGraphIs(graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=(Name=C GenericParameterCount=1))" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C&lt;T&gt;" Icon="Microsoft.VisualStudio.Class.Internal" Label="C&lt;T&gt;"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestGenericCSharpMethodTypeSymbolTest() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs"><![CDATA[[
                                class C { 
                                    public delegate T $$SomeDelegate<T>(T param);                                   
                                }
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Dim symbol = testState.GetMarkedSymbol()
                Dim graph = New Graph()
                Await graph.CreateNodeAsync(symbol, testState.GetSolution(), CancellationToken.None)
                AssertSimplifiedGraphIs(graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=(Name=SomeDelegate GenericParameterCount=1 ParentType=C))" Category="CodeSchema_Delegate" CodeSchemaProperty_IsFinal="True" CodeSchemaProperty_IsPublic="True" CommonLabel="SomeDelegate&lt;T&gt;" Icon="Microsoft.VisualStudio.Delegate.Public" Label="SomeDelegate&lt;T&gt;(T) : T"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestGenericVBMethodSymbolTest() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
                                Module Module1
                                    Public Class Foo(Of T)
                                        Public Sub $$Foo(ByVal x As T)
                                        End Sub
                                    End Class
                                End Module
                            </Document>
                        </Project>
                    </Workspace>)

                Dim graphNode = testState.GetGraphWithMarkedSymbolNode().Nodes.Single()
                Dim formattedLabelExtension As New GraphFormattedLabelExtension()
                Assert.Equal("Foo(T)", formattedLabelExtension.Label(graphNode, GraphCommandDefinition.Contains.Id))
                Assert.Equal("Foo", graphNode.Label)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestGenericVBTypeSymbolTest() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
                                Module Module1
                                    Public Class $$Foo(Of T)
                                    End Class
                                End Module
                            </Document>
                        </Project>
                    </Workspace>)

                Dim graphNode = testState.GetGraphWithMarkedSymbolNode().Nodes.Single()
                Dim formattedLabelExtension As New GraphFormattedLabelExtension()
                Assert.Equal("Foo(Of T)", formattedLabelExtension.Label(graphNode, GraphCommandDefinition.Contains.Id))
                Assert.Equal("Foo(Of T)", graphNode.Label)

                Dim symbol = testState.GetMarkedSymbol()
                Dim graph = New Graph()
                Await graph.CreateNodeAsync(symbol, testState.GetSolution(), CancellationToken.None)
                AssertSimplifiedGraphIs(graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=(Name=Foo GenericParameterCount=1 ParentType=Module1))" Category="CodeSchema_Class" CodeSchemaProperty_IsPublic="True" CommonLabel="Foo&lt;T&gt;" Icon="Microsoft.VisualStudio.Class.Public" Label="Foo(Of T)"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/VisualBasicAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestMultiGenericVBTypeSymbolTest() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
                                Module Module1
                                    Public Class $$Foo(Of T, X)
                                    End Class
                                End Module
                            </Document>
                        </Project>
                    </Workspace>)

                Dim graphNode = testState.GetGraphWithMarkedSymbolNode().Nodes.Single()
                Dim formattedLabelExtension As New GraphFormattedLabelExtension()
                Assert.Equal("Foo(Of T, X)", formattedLabelExtension.Label(graphNode, GraphCommandDefinition.Contains.Id))
                Assert.Equal("Foo(Of T, X)", graphNode.Label)

                Dim symbol = testState.GetMarkedSymbol()
                Dim graph = New Graph()
                Await graph.CreateNodeAsync(symbol, testState.GetSolution(), CancellationToken.None)
                AssertSimplifiedGraphIs(graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=(Name=Foo GenericParameterCount=2 ParentType=Module1))" Category="CodeSchema_Class" CodeSchemaProperty_IsPublic="True" CommonLabel="Foo&lt;T, X&gt;" Icon="Microsoft.VisualStudio.Class.Public" Label="Foo(Of T, X)"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/VisualBasicAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestFilteringPropertiesTest() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
Public Class TestEvents
    Public Custom Event CustomEvent As EventHandler(Of Object)
        AddHandler($$value As EventHandler(Of Object))

        End AddHandler

        RemoveHandler(value As EventHandler(Of Object))

        End RemoveHandler

        RaiseEvent(sender As Object, e As Object)

        End RaiseEvent
    End Event
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                Dim symbol = testState.GetMarkedSymbol()
                Dim graph = New Graph()
                Await graph.CreateNodeAsync(symbol, testState.GetSolution(), CancellationToken.None)
                AssertSimplifiedGraphIs(graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=TestEvents Member=(Name=add_CustomEvent OverloadingParameters=[(@2 Namespace=System Type=(Name=EventHandler GenericParameterCount=1 GenericArguments=[(@2 Namespace=System Type=Object)]))]) ParameterIdentifier=value)" Category="CodeSchema_Parameter" CodeSchemaProperty_IsByReference="False" CodeSchemaProperty_IsOut="False" CodeSchemaProperty_IsParameterArray="False" CommonLabel="value" Label="value"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/VisualBasicAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function
    End Class
End Namespace
