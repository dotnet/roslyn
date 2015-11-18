' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression

    Public Class ImplementsGraphQueryTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestClassImplementsInterface1() As Task
            Using testState = New ProgressionTestState(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class $$C : System.IDisposable { }
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithMarkedSymbolNode()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ImplementsGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                            <Node Id="(@2 Namespace=System Type=IDisposable)" Category="CodeSchema_Interface" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsPublic="True" CommonLabel="IDisposable" Icon="Microsoft.VisualStudio.Interface.Public" Label="IDisposable"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=C)" Target="(@2 Namespace=System Type=IDisposable)" Category="Implements"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestMethodImplementsInterfaceMethod1() As Threading.Tasks.Task
            Using testState = New ProgressionTestState(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
using System;

class Foo : IComparable
{
    public int $$CompareTo(object obj)
    {
        throw new NotImplementedException();
    }
}
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithMarkedSymbolNode()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ImplementsGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Foo Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsPublic="True" CommonLabel="CompareTo" Icon="Microsoft.VisualStudio.Method.Public" Label="CompareTo"/>
                            <Node Id="(@2 Namespace=System Type=IComparable Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsPublic="True" CommonLabel="CompareTo" Icon="Microsoft.VisualStudio.Method.Public" Label="CompareTo"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=Foo Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Target="(@2 Namespace=System Type=IComparable Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Category="Implements"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function
    End Class
End Namespace
