' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    <[UseExportProvider]>
    Public Class RenameNonRenameableSymbols
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameInheritedMetadataButRenameCascade()
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
                    </Workspace>, renameTo:="proper")


                result.AssertLabeledSpansAre("conflict", "proper", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameEventWithInvalidNames()
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
                    </Workspace>, renameTo:="!x")

                result.AssertReplacementTextInvalid()
                result.AssertLabeledSpansAre("Invalid", "!x", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameSpecialNames()
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
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameTrivia()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class B
                                {[|$$ |]}
                            </Document>
                        </Project>
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfFact>
        <WorkItem(883263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/883263")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameCandidateSymbol()
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
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameSyntheticDefinition()
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
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameXmlLiteralProperty()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
                                Module M
                                    Dim x = <x/>.<x>.$$Value
                                End Module
                            ]]></Document>
                        </Project>
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameSymbolDefinedInMetaData()
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
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameSymbolInReadOnlyBuffer()
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
                    </Workspace>)

                Dim textBuffer = workspace.Documents.Single().TextBuffer

                Using readOnlyEdit = textBuffer.CreateReadOnlyRegionEdit()
                    readOnlyEdit.CreateReadOnlyRegion(New Span(0, textBuffer.CurrentSnapshot.Length))
                    readOnlyEdit.Apply()
                End Using

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameSymbolThatBindsToErrorType()
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
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(543018, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543018")>
        Public Sub CannotRenameSynthesizedParameters()
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
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfFact>
        <WorkItem(539554, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539554")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenamePredefinedType()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                Module M
                                    Dim x As $$String
                                End Module
                            </Document>
                        </Project>
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfFact>
        <WorkItem(542937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542937")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameContextualKeyword()
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
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfFact>
        <WorkItem(543714, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543714")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameOperator()
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
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WpfFact>
        <WorkItem(529751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529751")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameExternAlias()
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
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WorkItem(543969, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543969")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameElementFromPreviousSubmission()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Submission Language="C#" CommonReferences="true">
                            int goo;
                        </Submission>
                        <Submission Language="C#" CommonReferences="true">
                            $$goo = 42;
                        </Submission>
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WorkItem(689002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/689002")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameHiddenElement()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class $$R
End Class
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Dim navigationService = DirectCast(workspace.Services.GetService(Of IDocumentNavigationService)(), MockDocumentNavigationService)
                navigationService._canNavigateToSpan = False

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WorkItem(767187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767187")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameConstructorInVb()
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
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WorkItem(767187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767187")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameConstructorInVb2()
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
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WorkItem(767187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767187")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CannotRenameConstructorInVb3()
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
                    </Workspace>)

                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

#Region "Rename In Tuples"

        <WorkItem(10898, "https://github.com/dotnet/roslyn/issues/10898")>
        <WorkItem(10567, "https://github.com/dotnet/roslyn/issues/10567")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)>
        Public Sub RenameTupleFiledInDeclaration()

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
                    </Workspace>)

                ' NOTE: this is currently intentionally blocked
                '       see https://github.com/dotnet/roslyn/issues/10898
                AssertTokenNotRenamable(workspace)
            End Using

        End Sub

        <WorkItem(10898, "https://github.com/dotnet/roslyn/issues/10898")>
        <WorkItem(10567, "https://github.com/dotnet/roslyn/issues/10567")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)>
        Public Sub RenameTupleFiledInLiteral()
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
                   </Workspace>)

                ' NOTE: this is currently intentionally blocked
                '       see https://github.com/dotnet/roslyn/issues/10898
                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

        <WorkItem(10898, "https://github.com/dotnet/roslyn/issues/10898")>
        <WorkItem(10567, "https://github.com/dotnet/roslyn/issues/10567")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)>
        Public Sub RenameTupleFiledInFieldAccess()
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
       </Workspace>)

                ' NOTE: this is currently intentionally blocked
                '       see https://github.com/dotnet/roslyn/issues/10898
                AssertTokenNotRenamable(workspace)
            End Using

        End Sub

        <WorkItem(10567, "https://github.com/dotnet/roslyn/issues/14600")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)>
        Public Sub RenameTupleFiledInLiteralRegress14600()
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
                   </Workspace>)

                ' NOTE: this is currently intentionally blocked
                '       see https://github.com/dotnet/roslyn/issues/10898
                AssertTokenNotRenamable(workspace)
            End Using
        End Sub

#End Region

    End Class
End Namespace
