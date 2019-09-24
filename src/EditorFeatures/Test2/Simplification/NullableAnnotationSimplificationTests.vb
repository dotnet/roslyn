' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    Public Class NullableAnnotationSimplificationTests
        Inherits AbstractSimplificationTests

#Region "CSharp tests"

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharpLeaveAnnotationIfValidAndEnabled() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#nullable enable

class C
{
    void M()
    {
        {|SimplifyParent:string?|} x;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
#nullable enable

class C
{
    void M()
    {
        string? x;
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharpRemoveAnnotationIfDisabled() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#nullable disable

class C
{
    void M()
    {
        {|SimplifyParent:string?|} x;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
#nullable disable

class C
{
    void M()
    {
        string x;
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharpRemoveAnnotationLeavesTrivia() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#nullable disable

class C
{
    void M()
    {
        {|SimplifyParent:/*before*/string/*inner*/?/*after*/|} x;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
#nullable disable

class C
{
    void M()
    {
        /*before*/string/*inner*//*after*/ x;
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharpLeaveAnnotationIfStructType() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#nullable disable

class C
{
    void M()
    {
        {|SimplifyParent:int?|} x;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
#nullable disable

class C
{
    void M()
    {
        int? x;
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharpLeaveAnnotationIfUnconstrainedGeneric() As Task
            ' Putting a ? on a unconstrained generic is illegal, but for now we're going to be cautious and not remove any ?s on unconstrained generics.
            ' If we need extra smarts to clean these up it's not hard, but there's no reason to do it until we have a reason.
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#nullable disable

class C
{
    void M&lt;T&gt;()
    {
        {|SimplifyParent:T?|} x;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
#nullable disable

class C
{
    void M&lt;T&gt;()
    {
        T? x;
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

#End Region

    End Class
End Namespace
