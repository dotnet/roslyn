' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests

        <WorkItem(541928, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541928")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpRangeVariableInInto1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System.Linq;
class C
{
    static void Main(string[] args)
    {
        var temp = from x in "abc"
                   let z = x.ToString()
                   select z into $${|Definition:w|}
                   select [|w|];
    }
}</Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(541928, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541928")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpRangeVariableInInto2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System.Linq;
class C
{
    static void Main(string[] args)
    {
        var temp = from x in "abc"
                   let z = x.ToString()
                   select z into {|Definition:w|}
                   select [|$$w|];
    }
}</Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542161, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542161")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpRangeVariableInSubmission1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Submission Language="C#" CommonReferences="true">
using System.Linq;
var q = from $${|Definition:x|} in new int[] { 1, 2, 3, 4 } select [|x|];
</Submission>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542161, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542161")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpRangeVariableInSubmission2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Submission Language="C#" CommonReferences="true">
using System.Linq;
var q = from {|Definition:x|} in new int[] { 1, 2, 3, 4 } select [|$$x|];
</Submission>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542161, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542161")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpRangeVariableInFieldInitializer1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System.Linq;
class C
{
    IEnumerable&lt;int&gt; q = from $${|Definition:x|} in new int[] { 1, 2, 3, 4 } select [|x|];
}</Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542161, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542161")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpRangeVariableInFieldInitializer2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System.Linq;
class C
{
    IEnumerable&lt;int&gt; q = from {|Definition:x|} in new int[] { 1, 2, 3, 4 } select [|$$x|];
}</Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542509, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542509")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVisualBasicTrivialSelect1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim m = From {|Definition:$$z|} In "abc" Select [|z|]
    End Sub
End Module
</Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542509, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542509")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVisualBasicTrivialSelect2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim m = From {|Definition:z|} In "abc" Select [|$$z|]
    End Sub
End Module
</Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545163, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545163")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVisualBasicLetClause1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module Program
    Sub Main()
        Dim x = From y In "" Let {|Definition:$$z|} = 1 Select [|z|]
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545163, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545163")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVisualBasicLetClause2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module Program
    Sub Main()
        Dim x = From y In "" Let {|Definition:z|} = 1 Select [|$$z|]
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(628189, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/628189")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVisualBasicMultipleAggregateFunctions(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Module Program  
    Sub Main()    
        Dim q = Aggregate x In {0, 1, 2} Into Count(), {|Definition:Goo()|}
        Dim y = q.[|$$Goo|] ' Find All references for Goo    
        Dim y2 = q.[|Goo|]
    End Sub

    &lt;Extension&gt;
    Function Goo(Of T)(seq As IEnumerable(Of T)) As Integer  
        Return 0
    End Function 
End Module

Namespace System.Runtime.CompilerServices
    &lt;AttributeUsage(AttributeTargets.Method Or AttributeTargets.Property Or AttributeTargets.Class Or AttributeTargets.Assembly)&gt;
    Public Class ExtensionAttribute
        Inherits Attribute
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

    End Class
End Namespace
