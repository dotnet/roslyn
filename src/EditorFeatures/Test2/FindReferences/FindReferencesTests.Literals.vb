﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInt32Literals1() As Task
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

            Await TestStreamingFeature(test)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCharLiterals1() As Task
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

            Await TestStreamingFeature(test)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDoubleLiterals1() As Task
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

            Await TestStreamingFeature(test)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFloatLiterals1() As Task
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

            Await TestStreamingFeature(test)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestStringLiterals1() As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>

class C
{
    void M()
    {
        var i = [|$$"foo"|];
        var i = [|"foo"|];
        var i = [|@"foo"|];
        var i = "fo";
        var i = "fooo";
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
    dim i = [|"foo"|]
    dim i = [|"foo"|]
    dim i = "fo"
    dim i = "fooo"
end class
        </Document>
    </Project>
</Workspace>

            Await TestStreamingFeature(test)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestStringLiterals2() As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>

class C
{
    void M()
    {
        var i = [|$$"foo\nbar"|];
        var i = @"foo
bar";
        var i = "foo\r\nbar";
    }
}
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class C
    dim i = "foo
bar"
    dim i = "foobar"
end class
        </Document>
    </Project>
</Workspace>

            Await TestStreamingFeature(test)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestStringLiterals3() As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>

class C
{
    void M()
    {
        var i = [|$$"foo\r\nbar"|];
        var i = [|@"foo
bar"|];
        var i = "foo\nbar";
    }
}
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class C
    dim i = [|"foo
bar"|]
    dim i = "foobar"
end class
        </Document>
    </Project>
</Workspace>

            Await TestStreamingFeature(test)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDecimalLiterals1() As Task
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

            Await TestStreamingFeature(test)
        End Function
    End Class
End Namespace
