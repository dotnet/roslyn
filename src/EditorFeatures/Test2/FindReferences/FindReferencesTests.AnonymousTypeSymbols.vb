' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WorkItem(542553)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAnonymousType1()
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
            Test(input)
        End Sub

        <WorkItem(542553)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAnonymousType2()
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
            Test(input)
        End Sub

        <WorkItem(542553)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAnonymousType3()
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
            Test(input)
        End Sub

        <WorkItem(542553)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAnonymousType4()
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
            Test(input)
        End Sub

        <WorkItem(542705)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAnonymousType5()
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
            Test(input)
        End Sub

    End Class
End Namespace
