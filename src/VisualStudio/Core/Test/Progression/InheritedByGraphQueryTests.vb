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
    Public Class InheritedByGraphQueryTests
        <WpfFact>
        Public Async Function TestInheritedByClassesCSharp() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
using System;

interface IBlah {
}

abstract class $$Base
{
    public abstract int CompareTo(object obj);
}

class Goo : Base, IComparable, IBlah
{
    public override int CompareTo(object obj)
    {
        throw new NotImplementedException();
    }
}

class Goo2 : Base, IBlah
{
    public override int CompareTo(object obj)
    {
        throw new NotImplementedException();
    }
}

class ReallyDerived : Goo // should not be shown as inherited by Base
{
}
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = Await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New InheritedByGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Base)" Category="CodeSchema_Class" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsInternal="True" CommonLabel="Base" Icon="Microsoft.VisualStudio.Class.Internal" Label="Base"/>
                            <Node Id="(@1 Type=Goo)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="Goo" Icon="Microsoft.VisualStudio.Class.Internal" Label="Goo"/>
                            <Node Id="(@1 Type=Goo2)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="Goo2" Icon="Microsoft.VisualStudio.Class.Internal" Label="Goo2"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=Goo)" Target="(@1 Type=Base)" Category="InheritsFrom"/>
                            <Link Source="(@1 Type=Goo2)" Target="(@1 Type=Base)" Category="InheritsFrom"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestInheritedByInterfacesCSharp() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
using System;

public interface $$I { }

public class C : I { } // should appear as being derived from (implementing) I

public class C2 : C { } // should not appear as being derived from (implementing) I

interface I2 : I, IComparable
{
    void M();
}

interface I3 : I2 // should not be shown as inherited by I
{
}
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = Await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New InheritedByGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsPublic="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Public" Label="C"/>
                            <Node Id="(@1 Type=I)" Category="CodeSchema_Interface" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsPublic="True" CommonLabel="I" Icon="Microsoft.VisualStudio.Interface.Public" Label="I"/>
                            <Node Id="(@1 Type=I2)" Category="CodeSchema_Interface" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsInternal="True" CommonLabel="I2" Icon="Microsoft.VisualStudio.Interface.Internal" Label="I2"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=C)" Target="(@1 Type=I)" Category="InheritsFrom"/>
                            <Link Source="(@1 Type=I2)" Target="(@1 Type=I)" Category="InheritsFrom"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestInheritedByClassesVisualBasic() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
Imports System

Interface IBlah
End Interface

MustInherit Class $$Base
    Public MustOverride Function CompareTo(obj As Object) As Integer
End Class

Class Goo
    Inherits Base
    Implements IComparable, IBlah
    Public Overrides Function CompareTo(obj As Object) As Integer Implements IComparable.CompareTo
        Throw New NotImplementedException()
    End Function
End Class

Class Goo2
    Inherits Base
    Implements IBlah
    Public Overrides Function CompareTo(obj As Object) As Integer
        Throw New NotImplementedException()
    End Function
End Class

Class ReallyDerived ' should not be shown as inherited by Base
    Inherits Goo
End Class
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = Await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New InheritedByGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Base)" Category="CodeSchema_Class" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsInternal="True" CommonLabel="Base" Icon="Microsoft.VisualStudio.Class.Internal" Label="Base"/>
                            <Node Id="(@1 Type=Goo)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="Goo" Icon="Microsoft.VisualStudio.Class.Internal" Label="Goo"/>
                            <Node Id="(@1 Type=Goo2)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="Goo2" Icon="Microsoft.VisualStudio.Class.Internal" Label="Goo2"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=Goo)" Target="(@1 Type=Base)" Category="InheritsFrom"/>
                            <Link Source="(@1 Type=Goo2)" Target="(@1 Type=Base)" Category="InheritsFrom"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/VisualBasicAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestInheritedByInterfacesVisualBasic() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
Imports System

Public Interface $$I
End Interface

Public Class C ' should appear as being derived from (implementing) I
    Implements I
End Class

Public Class C2 ' should not appear as being derived from (implementing) I
    Inherits C
End Class

Interface I2
    Inherits I, IComparable
    Sub M()
End Interface

Interface I3 ' should not be shown as inherited by I
    Inherits I2
End Interface
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = Await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New InheritedByGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsPublic="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Public" Label="C"/>
                            <Node Id="(@1 Type=I)" Category="CodeSchema_Interface" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsPublic="True" CommonLabel="I" Icon="Microsoft.VisualStudio.Interface.Public" Label="I"/>
                            <Node Id="(@1 Type=I2)" Category="CodeSchema_Interface" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsInternal="True" CommonLabel="I2" Icon="Microsoft.VisualStudio.Interface.Internal" Label="I2"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=C)" Target="(@1 Type=I)" Category="InheritsFrom"/>
                            <Link Source="(@1 Type=I2)" Target="(@1 Type=I)" Category="InheritsFrom"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/VisualBasicAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function
    End Class

End Namespace
