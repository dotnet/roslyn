' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542553")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestAnonymousType1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Infer On
Imports System
Module Program
    Sub Main(args As String())
        Dim namedCust = New With {.[|$${|Definition:Name|}|] = "Blue Yonder Airlines",
                                   .City = "Snoqualmie"}
        Dim product = New With {Key.Name = "paperclips", .Price = 1.29}
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542553")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestAnonymousType2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Infer On
Imports System
Module Program
    Sub Main(args As String())
        Dim namedCust = New With {.Name = "Blue Yonder Airlines",
                                   .City = "Snoqualmie"}
        Dim product = New With {Key.[|$${|Definition:Name|}|] = "paperclips", .Price = 1.29}
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542553")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestAnonymousType3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Infer On
Imports System
Module Program
    Sub Main(args As String())
        Dim namedCust1 = New With {.[|$${|Definition:Name|}|] = "Blue Yonder Airlines",
                                   .City = "Snoqualmie"}
        Dim namedCust2 = New With {.[|Name|] = "Blue Yonder Airlines",
                                   .City = "Snoqualmie"}
        Dim product = New With {Key.Name = "paperclips", .Price = 1.29}
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542553")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestAnonymousType4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Infer On
Imports System
Module Program
    Sub Main(args As String())
        Dim namedCust1 = New With {.[|Name|] = "Blue Yonder Airlines",
                                   .City = "Snoqualmie"}
        Dim namedCust2 = New With {.{|Definition:[|$$Name|]|} = "Blue Yonder Airlines",
                                   .City = "Snoqualmie"}
        Dim product = New With {Key.Name = "paperclips", .Price = 1.29}
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542705")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestAnonymousType5(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program1
    Shared str As String = "abc"
    Shared Sub Main(args As String())
        Dim employee08 = New With {.[|$${|Definition:Category|}|] = Category(str), Key.Name = 2 + 1}
        Dim employee01 = New With {Key.Category = 2 + 1, Key.Name = "Bob"}
    End Sub
    Shared Function Category(str As String)
        Category = str
    End Function
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/3284")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCaseInsensitiveAnonymousType1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        Dim x = New With {.[|$${|Definition:A|}|] = 1}
        Dim y = New With {.[|A|] = 2}
        Dim z = New With {.[|a|] = 3}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/3284")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCaseInsensitiveAnonymousType2(kind As TestKind, host As TestHost) As System.Threading.Tasks.Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        Dim x = New With {.[|A|] = 1}
        Dim y = New With {.[|A|] = 2}
        Dim z = New With {.[|$${|Definition:a|}|] = 3}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
