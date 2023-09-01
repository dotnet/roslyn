' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
        Public Async Function TestLocal(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Goo()
            {
                int {|Definition:$$i|} = 0;
                Console.WriteLine([|i|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/10714")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestLocalInAutoPropInitializer(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class Program
{
    public Action&lt;object&gt; Test { get; set; } = test =>
    {
        var $${|Definition:goo|} = 1;
        [|goo|] = 3;
    };
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestLocalInFieldInitializerLambda1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using System;
        class C
        {
            Action a = () =>
            {
                int $${|Definition:i|} = 0;
                Console.WriteLine([|i|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestLocalInFieldInitializerLambda2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using System;
        class C
        {
            Action a = () =>
            {
                int {|Definition:i|} = 0;
                Console.WriteLine([|$$i|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestLocalCaseSensitivity(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Goo()
            {
                int {|Definition:$$i|} = 0;
                Console.WriteLine(I);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestLocalCaseInsensitivity(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        class C
            sub Goo()
                dim {|Definition:$$i|} = 0
                Console.WriteLine([|i|])
                Console.WriteLine([|I|])
                Console.WriteLine(x)
            end sub
        end class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530636")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestLocalInLambdaInField1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module M
    Dim a = Sub() If True Then Dim {|Definition:$$x|} = 1, y = [|x|] + 1 Else Return
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530636")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestLocalInLambdaInField2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module M
    Dim a = Sub() If True Then Dim {|Definition:x|} = 1, y = [|$$x|] + 1 Else Return
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608210")>
        Public Async Function TestLocalInPropertyInitializer(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C
            Property A As Action = Sub()
                                       Dim {|Definition:$$x|} = 1
                                       Dim y = [|x|]
                                       Dim z = [|x|]
                                   End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/2667")>
        Public Async Function TestLocalWithWithStatement(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C
            Public Property P As String
            Public Property L As New List(Of String)
            Sub M()
                Dim {|Definition:$$x|} As New C
                With [|x|]
                    .P = "abcd"
                End With

                With [|x|].L
                    .Add("efgh")
                End With
            End Function
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

#Region "FAR on collection initializers"

        <WpfTheory, CombinatorialData>
        Public Async Function TestLocal_CSharpNamedIdentifiersUsedInNestedColInit(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class C
        {          
            void M()
            {                    
                var list = new List<int> { 13, 21, 34 };
                int {|Definition:$$i|}  = 5;
                int j = 6;
                var col = new List<List<int>> {list, new List<int> { [|i|], j } }; 
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestLocal_VBNamedIdentifiersUsedInNestedColInit(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
        Class C                
           sub M()
               Dim list = new List(of Integer) From {12, 21}
               Dim {|Definition:$$i|} = 1
               Dim j =  6
               Dim col = New List(Of List(Of Integer)) From {{list}, {New List(Of Integer) From {[|i|], j}}}
           End Sub
        End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestLocal_CSharpNamedIdentifiersUsedInAVeryLongColInitExp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class C
        {          
            void M()
            {                    
                int {|Definition:$$bird|} = 4;
                var bedbug = new List<bool> { [|bird|] == 92, ([|bird|] | 1) == 0, (((([|bird|] * [|bird|] / /*RN2*/[|bird|]) % ([|bird|] / 5)) | ([|bird|] - ([|bird|] / 5))) & [|bird|]) == 1 }; 
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestLocal_VBNamedIdentifiersUsedInAVeryLongColInitEx(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
        Class C                
           sub M()
               Dim  {|Definition:$$bird|} = 4
               Dim col = New List(Of Boolean)() From { [|bird|] = 92, ([|bird|] Or 1) = 0, (((([|bird|] * [|bird|] \ [|bird|]) Mod([|bird|] \ 5)) Or([|bird|] -([|bird|] \ 5))) And [|bird|]) = 1 }
           End Sub
        End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
#End Region

        <WpfTheory, CombinatorialData>
        <CompilerTrait(CompilerFeature.Tuples)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/15476")>
        Public Async Function TupleElementVsLocal_CS_01(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

static class Program
{
    static void Main()
    {
        (int elem1, int elem2) tuple;
        int {|Definition:$$elem2|};

        tuple = (5, 6);
        tuple.elem2 = 23;
        [|elem2|] = 10;

        Console.WriteLine(tuple.elem2);
        Console.WriteLine([|elem2|]);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <CompilerTrait(CompilerFeature.Tuples)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/15476")>
        Public Async Function TupleElementVsLocal_CS_02(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

static class Program
{
    static void Main()
    {
        (int elem1, int elem2) tuple;
        int {|Definition:elem2|};

        tuple = (5, 6);
        tuple.elem2 = 23;
        [|elem2|] = 10;

        Console.WriteLine(tuple.elem2);
        Console.WriteLine([|$$elem2|]);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <CompilerTrait(CompilerFeature.Tuples)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/15476")>
        Public Async Function TupleElementVsLocal_CS_03(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

static class Program
{
    static void Main()
    {
        (int elem1, int {|Definition:$$elem2|}) tuple;
        int elem2;

        tuple = (5, 6);
        tuple.[|elem2|] = 23;
        elem2 = 10;

        Console.WriteLine(tuple.[|elem2|]);
        Console.WriteLine(elem2);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <CompilerTrait(CompilerFeature.Tuples)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/15476")>
        Public Async Function TupleElementVsLocal_CS_04(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

static class Program
{
    static void Main()
    {
        (int elem1, int {|Definition:elem2|}) tuple;
        int elem2;

        tuple = (5, 6);
        tuple.[|elem2|] = 23;
        elem2 = 10;

        Console.WriteLine(tuple.[|$$elem2|]);
        Console.WriteLine(elem2);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <CompilerTrait(CompilerFeature.Tuples)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/15476")>
        Public Async Function TupleElementVsLocal_VB_01(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Module C

    Sub Main()
        Dim tuple As (elem1 As Integer, elem2 As Integer)
        Dim {|Definition:$$elem2|} As Integer

        tuple = (5, 6)
        tuple.elem2 = 23
        [|elem2|] = 10

        Console.WriteLine(tuple.elem2)
        Console.WriteLine([|elem2|])
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <CompilerTrait(CompilerFeature.Tuples)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/15476")>
        Public Async Function TupleElementVsLocal_VB_02(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Module C

    Sub Main()
        Dim tuple As (elem1 As Integer, elem2 As Integer)
        Dim {|Definition:elem2|} As Integer

        tuple = (5, 6)
        tuple.elem2 = 23
        [|elem2|] = 10

        Console.WriteLine(tuple.elem2)
        Console.WriteLine([|$$elem2|])
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <CompilerTrait(CompilerFeature.Tuples)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/15476")>
        Public Async Function TupleElementVsLocal_VB_03(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Module C

    Sub Main()
        Dim tuple As (elem1 As Integer, {|Definition:$$elem2|} As Integer)
        Dim elem2 As Integer

        tuple = (5, 6)
        tuple.[|elem2|] = 23
        elem2 = 10

        Console.WriteLine(tuple.[|elem2|])
        Console.WriteLine(elem2)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <CompilerTrait(CompilerFeature.Tuples)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/15476")>
        Public Async Function TupleElementVsLocal_VB_04(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Module C

    Sub Main()
        Dim tuple As (elem1 As Integer, {|Definition:elem2|} As Integer)
        Dim elem2 As Integer

        tuple = (5, 6)
        tuple.[|elem2|] = 23
        elem2 = 10

        Console.WriteLine(tuple.[|$$elem2|])
        Console.WriteLine(elem2)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestLocal_ValueUsageInfo(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Goo()
            {
                int {|Definition:$$i|} = 0;
                Console.WriteLine({|ValueUsageInfo.Read:[|i|]|});
                {|ValueUsageInfo.Write:[|i|]|} = 0;
                {|ValueUsageInfo.ReadWrite:[|i|]|}++;
                Goo2(in {|ValueUsageInfo.ReadableReference:[|i|]|}, ref {|ValueUsageInfo.ReadableWritableReference:[|i|]|});
                Goo3(out {|ValueUsageInfo.WritableReference:[|i|]|});
                Console.WriteLine(nameof({|ValueUsageInfo.Name:[|i|]|}));
            }

            void Goo2(in int j, ref int k)
            {
            }

            void Goo3(out int i)
            {
                i = 0;
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/50589")>
        Public Async Function TestLocal_NoMatchWithImplicitObjectNamedParameter_1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    C(int goo) { }

    C M()
    {
        var {|Definition:$$goo|} = 1;
        return new(goo: 2);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/50589")>
        Public Async Function TestLocal_NoMatchWithImplicitObjectNamedParameter_2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    C(int {|Definition:$$goo|}) { }

    C M()
    {
        var goo = 1;
        return new([|goo|]: 2);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_queries/edit/1711987")>
        Public Async Function TestLocal_ErrorDuplicateMethodInDifferentFiles(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
partial class C
{
    int M()
    {
        var {|Definition:$$goo|} = 1;
        return [|goo|];
    }
}
        </Document>
        <Document>
partial class C
{
    int M()
    {
        return goo;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
