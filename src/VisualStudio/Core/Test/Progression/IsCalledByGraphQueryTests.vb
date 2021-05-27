' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    <UseExportProvider, Trait(Traits.Feature, Traits.Features.Progression)>
    Public Class IsCalledByGraphQueryTests
        <WpfFact>
        Public Async Function IsCalledBySimpleTests() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
    class A
    {
        public $$A() { }
        public virtual void Run() { }
    }

    class B : A
    {
        public B() { }
        override public void Run() { var x = new A(); x.Run(); }
    }

    class C
    {
        public C() { }
        public void Goo()
        {
            var x = new B();
            x.Run();
        }
    }
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = Await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New IsCalledByGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=A Member=.ctor)" Category="CodeSchema_Method" CodeSchemaProperty_IsConstructor="True" CodeSchemaProperty_IsPublic="True" CommonLabel="A" Icon="Microsoft.VisualStudio.Method.Public" Label="A"/>
                            <Node Id="(@1 Type=B Member=Run)" Category="CodeSchema_Method" CodeSchemaProperty_IsPublic="True" CommonLabel="Run" Icon="Microsoft.VisualStudio.Method.Public" IsOverloaded="True" Label="Run"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=B Member=Run)" Target="(@1 Type=A Member=.ctor)" Category="CodeSchema_Calls"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function
    End Class
End Namespace
