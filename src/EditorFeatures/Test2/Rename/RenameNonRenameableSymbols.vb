' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class RenameNonRenameableSymbols
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub CannotRenameInheritedMetadataButRenameCascade(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                using A;
                                class B : A
                                {
                                    public override int {|conflict:prop|}
                                    { get; set; }
                                }

                                class C : A
                                {
                                    public override int [|prop$$|]
                                    { get; set; }
                                }
                            </Document>
                            <MetadataReferenceFromSource Language="C#" CommonReferences="true">
                                <Document FilePath="ReferencedDocument">
                                    public class A
                                    {
                                        public virtual int prop
                                        { get; set; }
                                    }
                                </Document>
                            </MetadataReferenceFromSource>
                        </Project>
                    </Workspace>, host:=host, renameTo:="proper")

                result.AssertLabeledSpansAre("conflict", "proper", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameEventWithInvalidNames(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                public delegate void MyDelegate();
                                class B
                                {
                                    public event MyDelegate {|Invalid:$$x|};
                                }
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="!x")

                result.AssertReplacementTextInvalid()
                result.AssertLabeledSpansAre("Invalid", "!x", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData>
        Public Sub CannotRenameSpecialNames(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class B
                                {
                                    public static B operator $$|(B x, B y)
                                    {
                                        return x;
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData>
        Public Sub CannotRenameTrivia(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class B
                                {[|$$ |]}
                            </Document>
                        </Project>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfTheory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/883263")>
        <CombinatorialData>
        Public Sub CannotRenameCandidateSymbol(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    void X(int x)
    {
        $$X();
    }
}
                            </Document>
                        </Project>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData>
        Public Sub CannotRenameSyntheticDefinition(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class C
                                {
                                    public int X
                                    {
                                        set { int y = $$value; }
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData>
        Public Sub CannotRenameXmlLiteralProperty(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
                                Module M
                                    Dim x = <x/>.<x>.$$Value
                                End Module
                            ]]></Document>
                        </Project>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData>
        Public Sub CannotRenameSymbolDefinedInMetaData(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class C
                                {
                                    void M()
                                    {
                                        System.Con$$sole.Write(5);
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData>
        Public Sub CannotRenameSymbolInReadOnlyBuffer(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class C
                                {
                                    void M()
                                    {
                                        $$M();
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                Using readOnlyEdit = textBuffer.CreateReadOnlyRegionEdit()
                    readOnlyEdit.CreateReadOnlyRegion(New Span(0, textBuffer.CurrentSnapshot.Length))
                    readOnlyEdit.Apply()
                End Using

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData>
        Public Sub CannotRenameSymbolThatBindsToErrorType(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class C
                                {
                                    void M()
                                    {
                                        int ^^$$x = 0;
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543018")>
        Public Sub CannotRenameSynthesizedParameters(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                Delegate Sub F

                                Module Program
                                    Sub Main(args As String())
                                        Dim f As F
                                        f.EndInvoke($$DelegateAsyncResult:=Nothing)
                                    End Sub
                                End Module
                            </Document>
                        </Project>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfTheory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539554")>
        <CombinatorialData>
        Public Sub CannotRenamePredefinedType(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                Module M
                                    Dim x As $$String
                                End Module
                            </Document>
                        </Project>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfTheory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542937")>
        <CombinatorialData>
        Public Sub CannotRenameContextualKeyword(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                Imports System
                                Imports System.Collections.Generic
                                Imports System.Linq

                                Module Program
                                    Sub Main(args As String())
                                        Dim query = From i In New C
                                                    $$Group Join j In New C
                                                    On i Equals j
                                                    Into g = Group
                                    End Sub
                                End Module
                            </Document>
                        </Project>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfTheory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543714")>
        <CombinatorialData>
        Public Sub CannotRenameOperator(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                public class MyClass
                                {
                                    public static MyClass operator ++(MyClass c)
                                    {
                                        return null;
                                    }

                                    public static void M()
                                    {
                                        $$op_Increment(null);
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfTheory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529751")>
        <CombinatorialData>
        Public Sub CannotRenameExternAlias(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                extern alias goo; // goo is unresolved
 
                                class A
                                {
                                    object x = new $$goo::X();
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543969")>
        <WpfTheory>
        <CombinatorialData>
        Public Sub CannotRenameElementFromPreviousSubmission(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Submission Language="C#" CommonReferences="true">
                            int goo;
                        </Submission>
                        <Submission Language="C#" CommonReferences="true">
                            $$goo = 42;
                        </Submission>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/689002")>
        <WpfTheory>
        <CombinatorialData>
        Public Sub CannotRenameHiddenElement(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class $$R
End Class
                            ]]></Document>
                        </Project>
                    </Workspace>, host)

                Dim navigationService = DirectCast(workspace.Services.GetService(Of IDocumentNavigationService)(), MockDocumentNavigationService)
                navigationService._canNavigateToSpan = False

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767187")>
        <WpfTheory>
        <CombinatorialData>
        Public Sub CannotRenameConstructorInVb(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class R
    ''' <summary>    
    ''' <see cref="R.New()"/>    
    ''' </summary>
    Shared Sub New()
    End Sub

    ''' <summary>    
    ''' <see cref="R.$$New()"/>
    ''' </summary>    
    Public Sub New()
    End Sub
End Class
                            ]]></Document>
                        </Project>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767187")>
        <WpfTheory>
        <CombinatorialData>
        Public Sub CannotRenameConstructorInVb2(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class R
    ''' <summary>    
    ''' <see cref="R.$$New()"/>    
    ''' </summary>
    Shared Sub New()
    End Sub
End Class
                            ]]></Document>
                        </Project>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767187")>
        <WpfTheory>
        <CombinatorialData>
        Public Sub CannotRenameConstructorInVb3(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class Base
End Class

Class R
    Inherits Base
    Public Sub New()
        Mybase.$$New()
    End Sub
End Class
                            ]]></Document>
                        </Project>
                    </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

#Region "Rename In Tuples"

        <WorkItem("https://github.com/dotnet/roslyn/issues/10898")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/10567")>
        <WpfTheory>
        <CombinatorialData>
        <CompilerTrait(CompilerFeature.Tuples)>
        Public Sub RenameTupleFiledInDeclaration(host As RenameTestHost)

            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="__DEMO__">
                            <Document>
using System;

class C
{
    static void Main()
    {
        (int $$Alice, int Bob) t = (1, 2);
        t.Alice = 3;
    }
}

namespace System
{
    // struct with two values
    public struct ValueTuple&lt;T1, T2&gt;
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + ", " + Item2?.ToString() + '}';
        }
    }
}
                            </Document>
                        </Project>
                    </Workspace>, host)

                ' NOTE: this is currently intentionally blocked
                '       see https://github.com/dotnet/roslyn/issues/10898
                AssertTokenNotRenamable(workspace)
            End Using

        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/10898")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/10567")>
        <WpfTheory>
        <CombinatorialData>
        <CompilerTrait(CompilerFeature.Tuples)>
        Public Sub RenameTupleFiledInLiteral(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                   <Workspace>
                       <Project Language="C#" CommonReferences="true" PreprocessorSymbols="__DEMO__">
                           <Document>
using System;

class C
{
    static void Main()
    {
        var t = ($$Alice: 1, Bob: 2);
        t.Alice = 3;
    }
}



namespace System
{
    // struct with two values
    public struct ValueTuple&lt;T1, T2&gt;
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + ", " + Item2?.ToString() + '}';
        }
    }
}
                            </Document>
                       </Project>
                   </Workspace>, host)

                ' NOTE: this is currently intentionally blocked
                '       see https://github.com/dotnet/roslyn/issues/10898
                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/10898")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/10567")>
        <WpfTheory>
        <CombinatorialData>
        <CompilerTrait(CompilerFeature.Tuples)>
        Public Sub RenameTupleFiledInFieldAccess(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
       <Workspace>
           <Project Language="C#" CommonReferences="true" PreprocessorSymbols="__DEMO__">
               <Document>
using System;

class C
{
    static void Main()
    {
        var t = (Alice: 1, Bob: 2);
        t.$$Alice = 3;
    }
}

namespace System
{
    // struct with two values
    public struct ValueTuple&lt;T1, T2&gt;
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + ", " + Item2?.ToString() + '}';
        }
    }
}
                            </Document>
           </Project>
       </Workspace>, host)

                ' NOTE: this is currently intentionally blocked
                '       see https://github.com/dotnet/roslyn/issues/10898
                AssertTokenNotRenamable(workspace)
            End Using

        End Sub

        <WorkItem(10567, "https://github.com/dotnet/roslyn/issues/14600")>
        <WpfTheory>
        <CombinatorialData>
        <CompilerTrait(CompilerFeature.Tuples)>
        Public Sub RenameTupleFiledInLiteralRegress14600(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                   <Workspace>
                       <Project Language="C#" CommonReferences="true" PreprocessorSymbols="__DEMO__">
                           <Document>
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = (Program: 1, Bob: 2);

        var Alice = x.$$Program;                
    }

}



namespace System
{
    // struct with two values
    public struct ValueTuple&lt;T1, T2&gt;
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + ", " + Item2?.ToString() + '}';
        }
    }
}
                            </Document>
                       </Project>
                   </Workspace>, host)

                ' NOTE: this is currently intentionally blocked
                '       see https://github.com/dotnet/roslyn/issues/10898
                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

#End Region

        <WpfTheory, CombinatorialData>
        Public Sub CannotRenameSymbolDefinedInSourceGeneratedDocument(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                   <Workspace>
                       <Project Language="C#" CommonReferences="true">
                           <Document>
class Program
{
    private $$C _c;
}
                           </Document>
                           <DocumentFromSourceGenerator>
class C
{
}
                           </DocumentFromSourceGenerator>
                       </Project>
                   </Workspace>, host)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub
    End Class
End Namespace
