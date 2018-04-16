' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression

    <[UseExportProvider]>
    Public Class OverriddenByGraphQueryTests
        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestOverriddenByMethod1() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
using System;

abstract class Base
{
    public abstract int $$CompareTo(object obj);
}

class Goo : Base, IComparable
{
    public override int CompareTo(object obj)
    {
        throw new NotImplementedException();
    }
}
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New OverriddenByGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Base Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsPublic="True" CommonLabel="CompareTo" Icon="Microsoft.VisualStudio.Method.Public" Label="CompareTo"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestOverriddenByMethod2() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
using System;

abstract class Base
{
    public abstract int CompareTo(object obj);
}

class Goo : Base, IComparable
{
    public override int $$CompareTo(object obj)
    {
        throw new NotImplementedException();
    }
}
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New OverriddenByGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Base Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsPublic="True" CommonLabel="CompareTo" Icon="Microsoft.VisualStudio.Method.Public" Label="CompareTo"/>
                            <Node Id="(@1 Type=Goo Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsPublic="True" CommonLabel="CompareTo" Icon="Microsoft.VisualStudio.Method.Public" IsOverloaded="True" Label="CompareTo"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=Goo Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Target="(@1 Type=Base Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))"/>
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
