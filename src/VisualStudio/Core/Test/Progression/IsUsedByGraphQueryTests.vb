' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    Public Class IsUsedByGraphQueryTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function IsUsedByTests() As Threading.Tasks.Task
            Using testState = New ProgressionTestState(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                public class C {
                                  public int $$X;
                                  public int Y = X * X;
                                  public void M() {
                                     int x = 10;
                                     int y = x + X;
                                  }
                                }
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithMarkedSymbolNode()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New IsUsedByGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=C Member=X)" Category="CodeSchema_Field" CodeSchemaProperty_IsPublic="True" CommonLabel="X" Icon="Microsoft.VisualStudio.Field.Public" Label="X"/>
                            <Node Id="(@2 StartLineNumber=2 StartCharacterOffset=45 EndLineNumber=2 EndCharacterOffset=46)" Category="CodeSchema_SourceLocation" Icon="Microsoft.VisualStudio.Reference.Public" Label="Project.cs (3, 46): public int X;"/>
                            <Node Id="(@2 StartLineNumber=3 StartCharacterOffset=49 EndLineNumber=3 EndCharacterOffset=50)" Category="CodeSchema_SourceLocation" Icon="Microsoft.VisualStudio.Reference.Public" Label="Project.cs (4, 50): public int Y = X * X;"/>
                            <Node Id="(@2 StartLineNumber=3 StartCharacterOffset=53 EndLineNumber=3 EndCharacterOffset=54)" Category="CodeSchema_SourceLocation" Icon="Microsoft.VisualStudio.Reference.Public" Label="Project.cs (4, 54): public int Y = X * X;"/>
                            <Node Id="(@2 StartLineNumber=6 StartCharacterOffset=49 EndLineNumber=6 EndCharacterOffset=50)" Category="CodeSchema_SourceLocation" Icon="Microsoft.VisualStudio.Reference.Public" Label="Project.cs (7, 50): int y = x + X;"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=C Member=X)" Target="(@2 StartLineNumber=2 StartCharacterOffset=45 EndLineNumber=2 EndCharacterOffset=46)" Category="CodeSchema_SourceReferences"/>
                            <Link Source="(@1 Type=C Member=X)" Target="(@2 StartLineNumber=3 StartCharacterOffset=49 EndLineNumber=3 EndCharacterOffset=50)" Category="CodeSchema_SourceReferences"/>
                            <Link Source="(@1 Type=C Member=X)" Target="(@2 StartLineNumber=3 StartCharacterOffset=53 EndLineNumber=3 EndCharacterOffset=54)" Category="CodeSchema_SourceReferences"/>
                            <Link Source="(@1 Type=C Member=X)" Target="(@2 StartLineNumber=6 StartCharacterOffset=49 EndLineNumber=6 EndCharacterOffset=50)" Category="CodeSchema_SourceReferences"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/Project.cs"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function
    End Class
End Namespace
