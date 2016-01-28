' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLabel1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Foo()
            {
            $${|Definition:Foo|}:
                int Foo;
                goto [|Foo|];
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLabel2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Foo()
            {
            {|Definition:Foo|}:
                int Foo;
                goto [|$$Foo|];
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(529060)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNumericLabel1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module M
    Sub Main()
label1: GoTo $$[|200|]
{|Definition:200|}:    GoTo label1
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(529060)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNumericLabel2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module M
    Sub Main()
label1: GoTo [|200|]
{|Definition:$$200|}:    GoTo label1
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
    End Class
End Namespace
