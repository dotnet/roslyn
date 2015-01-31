' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Expansion
    Public Class ExtensionMethodExpansionRewriteTests
        Inherits AbstractExpansionTest

#Region "Visual Basic ExtensionMethodRewrite Expansion tests"
        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub VisualBasic_ExpandSingleExtensionMethod()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = {|Expand:p.foo()|}
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program) As Program
        Return Prog
    End Function
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = Global.ProgramExtensions.foo((CType((p), Global.Program)))
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program) As Program
        Return Prog
    End Function
End Module
</code>

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub VisualBasic_ExpandSingleExtensionMethodWithArgument()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = {|Expand:p.foo("")|}
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program, str as string) As Program
        Return Prog
    End Function
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = Global.ProgramExtensions.foo((CType((p), Global.Program)), (CType((""), System.String)))
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program, str as string) As Program
        Return Prog
    End Function
End Module
</code>

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub VisualBasic_ExpandMultiExtensionMethod()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = {|Expand:p.foo().foo()|}
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program) As Program
        Return Prog
    End Function
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = Global.ProgramExtensions.foo((CType((Global.ProgramExtensions.foo((CType((p), Global.Program)))), Global.Program)))
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program) As Program
        Return Prog
    End Function
End Module
</code>

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub VisualBasic_ExpandMultiExtensionMethodWithArgument()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = {|Expand:p.foo("").foo("")|}
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program, str as string) As Program
        Return Prog
    End Function
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = Global.ProgramExtensions.foo((CType((Global.ProgramExtensions.foo((CType((p), Global.Program)), (CType((""), System.String)))), Global.Program)), (CType((""), System.String)))
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program, str as string) As Program
        Return Prog
    End Function
End Module
</code>

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub VisualBasic_ExpandMultiExtensionMethodWithMoreArgument()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = {|Expand:p.foo("","","").foo("","","")|}
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program, str as string, str1 as string, str2 as string) As Program
        Return Prog
    End Function
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = Global.ProgramExtensions.foo((CType((Global.ProgramExtensions.foo((CType((p), Global.Program)), (CType((""), System.String)), (CType((""), System.String)), (CType((""), System.String)))), Global.Program)), (CType((""), System.String)), (CType((""), System.String)), (CType((""), System.String)))
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program, str as string, str1 as string, str2 as string) As Program
        Return Prog
    End Function
End Module
</code>

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub VisualBasic_ExpandSimplifySingleExtensionMethod()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = {|ExpandAndSimplify:p.foo()|}
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program) As Program
        Return Prog
    End Function
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = p.foo()
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program) As Program
        Return Prog
    End Function
End Module
</code>

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub VisualBasic_ExpandSimplifyChainedExtensionMethodMoreArguments()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = {|ExpandAndSimplify:p.foo("","","").foo("","","")|}
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program, str as string, str1 as string, str2 as string) As Program
        Return Prog
    End Function
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = p.foo("", "", "").foo("", "", "")
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program, str as string, str1 as string, str2 as string) As Program
        Return Prog
    End Function
End Module
</code>

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub VisualBasic_ExpandSimplifyChainedExtensionMethodMoreArgumentsWithStatic()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim ss = {|ExpandAndSimplify:staticer.statP.foo("", "", "").foo("", "", "")|}
    End Sub
End Class

Public Class staticer
    Public Shared statP As Program
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program, str As String, str1 As String, str2 As String) As Program
        Return Prog
    End Function
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim ss = staticer.statP.foo("", "", "").foo("", "", "")
    End Sub
End Class

Public Class staticer
    Public Shared statP As Program
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function foo(ByVal Prog As Program, str As String, str1 As String, str2 As String) As Program
        Return Prog
    End Function
End Module
</code>

            Test(input, expected)
        End Sub

        <WorkItem(654403)>
        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub VB_ExtensionMethodRewriteRoundTripsTrivia()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim f As Program = New Program()
        {|ExpandAndSimplify:Dim openDocumentSearchingTask = _
            f _
            . _
            DoStuff _
            ( _
            23 _
            , _
            56 _
            , _
            36 _
            ) ' 1|}
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function DoStuff(ByVal Prog As Program, i1 As Integer, i2 As Integer, i3 As Integer) As Program
        Return Prog
    End Function
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim f As Program = New Program()
        Dim openDocumentSearchingTask = _
            f _
            . _
            DoStuff _
            ( _
            23 _
            , _
            56 _
            , _
            36 _
            ) ' 1
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function DoStuff(ByVal Prog As Program, i1 As Integer, i2 As Integer, i3 As Integer) As Program
        Return Prog
    End Function
End Module
</code>

            Test(input, expected)
        End Sub

#End Region

#Region "CSharp ExtensionMethodRewrite Expansion tests"
        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub CSharp_ExpandSingleExtensionMethod()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Program s = {|Expand:ss.foo()|};
    }
}

public static class ProgramExtensions
{
    public static Program foo(this Program p)
    {
        return p;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Program s = global::ProgramExtensions.foo(ss);
    }
}

public static class ProgramExtensions
{
    public static Program foo(this Program p)
    {
        return p;
    }
}
</code>

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub CSharp_ExpandSingleExtensionMethodWithArgument()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Second sec = null;
        Program s = {|Expand:ss.foo(sec)|};
    }
}

public static class ProgramExtensions
{
    public static Program foo(this Program p, Second s)
    {
        return p;
    }
}

public class Second
{
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Second sec = null;
        Program s = global::ProgramExtensions.foo(ss, (global::Second)(sec));
    }
}

public static class ProgramExtensions
{
    public static Program foo(this Program p, Second s)
    {
        return p;
    }
}

public class Second
{
}
</code>

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub CSharp_ExpandMultiExtensionMethod()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Program sss = {|Expand:ss.foo().foo()|};
    }
}

public static class ProgramExtensions
{
    public static Program foo(this Program p)
    {
        return p;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Program sss = global::ProgramExtensions.foo(global::ProgramExtensions.foo(ss));
    }
}

public static class ProgramExtensions
{
    public static Program foo(this Program p)
    {
        return p;
    }
}
</code>

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub CSharp_ExpandMultiExtensionMethodWithArgument()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Second sec = null;
        Program s = {|Expand:ss.foo(sec).foo(sec)|};
    }
}

public static class ProgramExtensions
{
    public static Program foo(this Program p, Second s)
    {
        return p;
    }
}

public class Second
{
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Second sec = null;
        Program s = global::ProgramExtensions.foo(global::ProgramExtensions.foo(ss, (global::Second)(sec)), (global::Second)(sec));
    }
}

public static class ProgramExtensions
{
    public static Program foo(this Program p, Second s)
    {
        return p;
    }
}

public class Second
{
}
</code>

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub CSharp_ExpandMultiExtensionMethodWithMoreArgument()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Second sec = null;
        Program s = {|Expand:ss.foo(sec, sec, sec).foo(sec, sec, sec)|};
    }
}

public static class ProgramExtensions
{
    public static Program foo(this Program p, Second s, Second ss, Second sss)
    {
        return p;
    }
}

public class Second
{
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Second sec = null;
        Program s = global::ProgramExtensions.foo(global::ProgramExtensions.foo(ss, (global::Second)(sec), (global::Second)(sec), (global::Second)(sec)), (global::Second)(sec), (global::Second)(sec), (global::Second)(sec));
    }
}

public static class ProgramExtensions
{
    public static Program foo(this Program p, Second s, Second ss, Second sss)
    {
        return p;
    }
}

public class Second
{
}
</code>

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub CSharp_ExpandSimplifySingleExtensionMethod()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Program s = {|ExpandAndSimplify:ss.foo()|};
    }
}

public static class ProgramExtensions
{
    public static Program foo(this Program p)
    {
        return p;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Program s = ss.foo();
    }
}

public static class ProgramExtensions
{
    public static Program foo(this Program p)
    {
        return p;
    }
}
</code>

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub CSharp_ExpandSimplifyChainedExtensionMethodWithMoreArgument()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Second sec = null;
        Program s = {|ExpandAndSimplify:ss.foo(sec, sec, sec).foo(sec, sec, sec)|};
    }
}

public static class ProgramExtensions
{
    public static Program foo(this Program p, Second s, Second ss, Second sss)
    {
        return p;
    }
}

public class Second
{
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Second sec = null;
        Program s = ss.foo(sec, sec, sec).foo(sec, sec, sec);
    }
}

public static class ProgramExtensions
{
    public static Program foo(this Program p, Second s, Second ss, Second sss)
    {
        return p;
    }
}

public class Second
{
}
</code>

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub CSharp_ExpandSimplifyWithStaticFieldExtensionMethod()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class Program
{
    static void Main(string[] args)
    {
        Program s = {|ExpandAndSimplify:starter.staticP.foo()|};
    }
}

public class starter
{
    public static Program staticP;
}

public static class ProgramExtensions
{
    public static Program foo(this Program p)
    {
        return p;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
public class Program
{
    static void Main(string[] args)
    {
        Program s = starter.staticP.foo();
    }
}

public class starter
{
    public static Program staticP;
}

public static class ProgramExtensions
{
    public static Program foo(this Program p)
    {
        return p;
    }
}
</code>

            Test(input, expected)
        End Sub

        <WorkItem(654403)>
        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Sub CSharp_ExtensionMethodRewriteRoundTripsTrivia()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class Program
{
    public static void Main(string[] args)
    {
        Program f = new Program();
        {|ExpandAndSimplify:var openDocumentSearchingTask =/*1*/f/*2*/./*3*/DoStuff/*4*/(/*5*/23/*6*/,/*7*/56/*8*/,/*9*/36/*9*/)/*10*/;/*11*/|}
    }
}

public static class FooExtension
{
    public static object DoStuff(this Program p, int i, int j, int k)
    {
        return null;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
public class Program
{
    public static void Main(string[] args)
    {
        Program f = new Program();
        var openDocumentSearchingTask =/*1*/f/*2*/./*3*/DoStuff/*4*/(/*5*/23/*6*/,/*7*/56/*8*/,/*9*/36/*9*/)/*10*/;/*11*/
    }
}

public static class FooExtension
{
    public static object DoStuff(this Program p, int i, int j, int k)
    {
        return null;
    }
}
</code>

            Test(input, expected)
        End Sub

#End Region

    End Class
End Namespace
