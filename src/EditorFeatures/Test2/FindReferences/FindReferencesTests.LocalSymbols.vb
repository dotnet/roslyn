' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WorkItem(10714, "https://github.com/dotnet/roslyn/issues/10714")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WorkItem(530636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530636")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WorkItem(530636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530636")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(608210, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608210")>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(2667, "https://github.com/dotnet/roslyn/issues/2667")>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)>
        <WorkItem(14881, "https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem(15476, "https://github.com/dotnet/roslyn/issues/15476")>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)>
        <WorkItem(14881, "https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem(15476, "https://github.com/dotnet/roslyn/issues/15476")>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)>
        <WorkItem(14881, "https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem(15476, "https://github.com/dotnet/roslyn/issues/15476")>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)>
        <WorkItem(14881, "https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem(15476, "https://github.com/dotnet/roslyn/issues/15476")>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)>
        <WorkItem(14881, "https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem(15476, "https://github.com/dotnet/roslyn/issues/15476")>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)>
        <WorkItem(14881, "https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem(15476, "https://github.com/dotnet/roslyn/issues/15476")>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)>
        <WorkItem(14881, "https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem(15476, "https://github.com/dotnet/roslyn/issues/15476")>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)>
        <WorkItem(14881, "https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem(15476, "https://github.com/dotnet/roslyn/issues/15476")>
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

    End Class
End Namespace
