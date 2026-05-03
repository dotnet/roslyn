' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
        Public Async Function TestVisualBasic_BuiltinBinaryOperator1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module Program
    Sub Goo()
        Dim a = 5 $$[|+|] 5
        Dim b = 1 [|+|] 1
        Dim x = 1 - 1
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVisualBasic_BuiltinUnaryOperator1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module Program
    Sub Goo()
        Dim a = $$[|-|] 5
        Dim b = [|-|] 1
        Dim x = +1
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharp_BuiltinBinaryOperator1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo()
    {
        var a = 5 $$[|+|] 5;
        var b = 1 [|+|] 1;
        var c = 1 - 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharp_BuiltinUnaryOperator1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo()
    {
        var a = $$[|-|] 5;
        var b = [|-|] 1;
        var c = + 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharp_BuiltinCheckedBinaryOperator1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo()
    {
        checked
        {
            var a = 5 $$[|+|] 5;
            var b = 1 [|+|] 1;
            var c = 1 - 1;
        }

        unchecked
        {
            var a = 5 + 5;
            var b = 1 + 1;
            var c = 1 - 1;
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharp_BuiltinUncheckedBinaryOperator1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo()
    {
        unchecked
        {
            var a = 5 $$[|+|] 5;
            var b = 1 [|+|] 1;
            var c = 1 - 1;
        }

        checked
        {
            var a = 5 + 5;
            var b = 1 + 1;
            var c = 1 - 1;
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharp_BuiltinCheckedUnaryOperator1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo()
    {
        checked
        {
            var a = $$[|-|] 5;
            var b = [|-|] 1;
            var c = + 1;
        }

        unchecked
        {
            var a = - 5;
            var b = - 1;
            var c = + 1;
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharp_BuiltinCheckedUnaryOperator2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo()
    {
        unchecked
        {
            var a = $$[|-|] 5;
            var b = [|-|] 1;
            var c = + 1;
        }

        checked
        {
            var a = - 5;
            var b = - 1;
            var c = + 1;
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCrossLanguage_BuiltinBinaryOperator1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module Program
    Sub Goo()
        Dim a = 5 $$[|+|] 5
        Dim b = 1 [|+|] 1
        Dim x = 1 - 1
    End Sub
End Module
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo()
    {
        var a = 5 [|+|] 5;
        var b = 1 [|+|] 1;
        var c = 1 - 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCrossLanguage_BuiltinBinaryOperator2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module Program
    Sub Goo()
        Dim a = 5 [|+|] 5
        Dim b = 1 [|+|] 1
        Dim x = 1 - 1
    End Sub
End Module
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo()
    {
        var a = 5 $$[|+|] 5;
        var b = 1 [|+|] 1;
        var c = 1 - 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
