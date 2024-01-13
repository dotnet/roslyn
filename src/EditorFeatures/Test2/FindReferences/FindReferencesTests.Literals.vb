' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
        Public Async Function TestInt32Literals1(host As TestHost) As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>

class C
{
    void M()
    {
        var i = [|$$0|];
        var i = [|0|];
        var i = [|00|];
        var i = [|0x0|];
        var i = [|0b0|];
        var i = 1;
        var i = 0.0;
    }
}
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class C
    dim i = [|0|]
    dim i = [|0|]
    dim i = [|&amp;H0|]
    dim i = 1
    dim i = 0.0
end class
        </Document>
    </Project>
</Workspace>

            Await TestStreamingFeature(test, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCharLiterals1(host As TestHost) As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var i = [|$$'c'|];
        var i = [|'c'|];
        var i = [|'\u0063'|];
        var i = 99; // 'c' in decimal
        var i = "c";
        var i = 'd';
    }
}
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class C
    dim i = [|"c"c|]
    dim i = [|"c"c|]
    dim i = 99
    dim i = "d"c
end class
        </Document>
    </Project>
</Workspace>

            Await TestStreamingFeature(test, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestDoubleLiterals1(host As TestHost) As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>

class C
{
    void M()
    {
        var i = [|$$0.0|];
        var i = [|0D|];
        var i = 0;
        var i = 0F;
        var i = '\u0000';
        var i = 00;
        var i = 0x0;
        var i = 0b0;
        var i = 1;
        var i = [|0.00|];
        var i = [|0e0|];
    }
}
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class C
    dim i = [|0.0|]
    dim i = 0
    dim i = [|0.00|]
end class
        </Document>
    </Project>
</Workspace>

            Await TestStreamingFeature(test, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestFloatLiterals1(host As TestHost) As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>

class C
{
    void M()
    {
        var i = [|$$0F|];
        var i = 0D;
        var i = 0;
        var i = '\u0000';
        var i = 00;
        var i = 0x0;
        var i = 0b0;
        var i = 1;
        var i = [|0.0f|];
        var i = [|0.00f|];
        var i = [|0e0f|];
    }
}
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class C
    dim i = [|0.0F|]
    dim i = 0
    dim i = [|0.00F|]
end class
        </Document>
    </Project>
</Workspace>

            Await TestStreamingFeature(test, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestStringLiterals1(host As TestHost) As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>

class C
{
    void M()
    {
        var i = [|$$"goo"|];
        var i = [|"goo"|];
        var i = [|@"goo"|];
        var i = "fo";
        var i = "gooo";
        var i = 'f';
        var i = 00;
        var i = 0x0;
        var i = 0b0;
        var i = 1;
        var i = 0.0f;
        var i = 0.00f;
        var i = 0e0f;
    }
}
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class C
    dim i = [|"goo"|]
    dim i = [|"goo"|]
    dim i = "fo"
    dim i = "gooo"
end class
        </Document>
    </Project>
</Workspace>

            Await TestStreamingFeature(test, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestStringLiterals2(host As TestHost) As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>

class C
{
    void M()
    {
        var i = [|$$"goo\nbar"|];
        var i = @"goo
bar";
        var i = "goo\r\nbar";
    }
}
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class C
    dim i = "goo
bar"
    dim i = "goobar"
end class
        </Document>
    </Project>
</Workspace>

            Await TestStreamingFeature(test, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestStringLiterals3(host As TestHost) As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>

class C
{
    void M()
    {
        var i = [|$$"goo\r\nbar"|];
        var i = [|@"goo
bar"|];
        var i = "goo\nbar";
    }
}
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class C
    dim i = [|"goo
bar"|]
    dim i = "goobar"
end class
        </Document>
    </Project>
</Workspace>

            Await TestStreamingFeature(test, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestDecimalLiterals1(host As TestHost) As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>

class C
{
    void M()
    {
        var i = $$1M; // Decimals not currently supported
        var i = 1M;
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestStreamingFeature(test, host)
        End Function

        <WpfFact, CombinatorialData>
        Public Async Function TestInt32LiteralsUsedInSourceGeneratedDocument() As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>

class C
{
    void M()
    {
        var i = [|$$0|];
    }
}
        </Document>
        <DocumentFromSourceGenerator>

class D
{
    void M()
    {
        var i = [|0|];
    }
}
        </DocumentFromSourceGenerator>
    </Project>
</Workspace>

            Await TestStreamingFeature(test, TestHost.InProcess) ' TODO: support out of proc in tests: https://github.com/dotnet/roslyn/issues/50494
        End Function
    End Class
End Namespace
