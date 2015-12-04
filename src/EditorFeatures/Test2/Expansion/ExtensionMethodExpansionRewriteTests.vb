' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Expansion
    Public Class ExtensionMethodExpansionRewriteTests
        Inherits AbstractExpansionTest

#Region "Visual Basic ExtensionMethodRewrite Expansion tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestVisualBasic_ExpandSingleExtensionMethod() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestVisualBasic_ExpandSingleExtensionMethodWithArgument() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestVisualBasic_ExpandMultiExtensionMethod() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestVisualBasic_ExpandMultiExtensionMethodWithArgument() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestVisualBasic_ExpandMultiExtensionMethodWithMoreArgument() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestVisualBasic_ExpandSimplifySingleExtensionMethod() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestVisualBasic_ExpandSimplifyChainedExtensionMethodMoreArguments() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestVisualBasic_ExpandSimplifyChainedExtensionMethodMoreArgumentsWithStatic() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WorkItem(654403)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestVB_ExtensionMethodRewriteRoundTripsTrivia() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        <WorkItem(2593, "https://github.com/dotnet/roslyn/issues/2593")>
        <WorkItem(3260, "https://github.com/dotnet/roslyn/issues/3260")>
        Public Async Function TestVisualBasic_ExpandExtensionMethodInMemberAccessExpression() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices

Module M
    <Extension()>
    Public Function Something(cust As C) As IEnumerable(Of String)
        Throw New NotImplementedException()
    End Function
End Module

Class C
    Private Function GetAssemblyIdentity(types As IEnumerable(Of C)) As Object
        For Each t In types
            Dim x = {|Expand:t.Something().First()|}
        Next
        Return Nothing
    End Function
End Class]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices

Module M
    <Extension()>
    Public Function Something(cust As C) As IEnumerable(Of String)
        Throw New NotImplementedException()
    End Function
End Module

Class C
    Private Function GetAssemblyIdentity(types As IEnumerable(Of C)) As Object
        For Each t In types
            Dim x = Global.System.Linq.Enumerable.First((CType((Global.M.Something((CType((t), Global.C)))), Global.System.Collections.Generic.IEnumerable(Of System.String))))
        Next
        Return Nothing
    End Function
End Class]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact(Skip:="3260"), Trait(Traits.Feature, Traits.Features.Expansion)>
        <WorkItem(2593, "https://github.com/dotnet/roslyn/issues/2593")>
        <WorkItem(3260, "https://github.com/dotnet/roslyn/issues/3260")>
        Public Async Function TestVisualBasic_ExpandExtensionMethodInConditionalAccessExpression() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices

Module M
    <Extension()>
    Public Function Something(cust As C) As IEnumerable(Of String)
        Throw New NotImplementedException()
    End Function
End Module

Class C
    Private Function GetAssemblyIdentity(types As IEnumerable(Of C)) As Object
        For Each t In types
            Dim x = {|Expand:t?.Something().First()|}
        Next
        Return Nothing
    End Function
End Class]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices

Module M
    <Extension()>
    Public Function Something(cust As C) As IEnumerable(Of String)
        Throw New NotImplementedException()
    End Function
End Module

Class C
    Private Function GetAssemblyIdentity(types As IEnumerable(Of C)) As Object
        For Each t In types
            Dim x = Global.System.Linq.Enumerable.First((CType(((CType((t), Global.C))?.Something()), Global.System.Collections.Generic.IEnumerable(Of System.String))))
        Next
        Return Nothing
    End Function
End Class]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        <WorkItem(2593, "https://github.com/dotnet/roslyn/issues/2593")>
        <WorkItem(3260, "https://github.com/dotnet/roslyn/issues/3260")>
        Public Async Function TestVisualBasic_ExpandExtensionMethodInMemberAccessExpression_2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System.Runtime.CompilerServices

Module M
    <Extension()>
    Public Function Something(cust As C) As IEnumerable(Of String)
        Throw New NotImplementedException()
    End Function

    <Extension()>
    Public Function Something2(cust As C) As Func(Of C)
        Throw New NotImplementedException()
    End Function
End Module

Class C
    Private Function GetAssemblyIdentity(types As IEnumerable(Of C)) As Object
        For Each t In types
            Dim x = {|Expand:t.Something2()().Something().First()|}
        Next
        Return Nothing
    End Function
End Class]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System.Runtime.CompilerServices

Module M
    <Extension()>
    Public Function Something(cust As C) As IEnumerable(Of String)
        Throw New NotImplementedException()
    End Function

    <Extension()>
    Public Function Something2(cust As C) As Func(Of C)
        Throw New NotImplementedException()
    End Function
End Module

Class C
    Private Function GetAssemblyIdentity(types As IEnumerable(Of C)) As Object
        For Each t In types
            Dim x = Global.System.Linq.Enumerable.First((CType((Global.M.Something((CType((Global.M.Something2((CType((t), Global.C)))()), Global.C)))), Global.System.Collections.Generic.IEnumerable(Of System.String))))
        Next
        Return Nothing
    End Function
End Class]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact(Skip:="3260"), Trait(Traits.Feature, Traits.Features.Expansion)>
        <WorkItem(2593, "https://github.com/dotnet/roslyn/issues/2593")>
        <WorkItem(3260, "https://github.com/dotnet/roslyn/issues/3260")>
        Public Async Function TestVisualBasic_ExpandExtensionMethodInConditionalAccessExpression_2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System.Runtime.CompilerServices

Module M
    <Extension()>
    Public Function Something(cust As C) As IEnumerable(Of String)
        Throw New NotImplementedException()
    End Function

    <Extension()>
    Public Function Something2(cust As C) As Func(Of C)
        Throw New NotImplementedException()
    End Function
End Module

Class C
    Private Function GetAssemblyIdentity(types As IEnumerable(Of C)) As Object
        For Each t In types
            Dim x = {|Expand:t?.Something2()?()?.Something().First()|}
        Next
        Return Nothing
    End Function
End Class]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System.Runtime.CompilerServices

Module M
    <Extension()>
    Public Function Something(cust As C) As IEnumerable(Of String)
        Throw New NotImplementedException()
    End Function

    <Extension()>
    Public Function Something2(cust As C) As Func(Of C)
        Throw New NotImplementedException()
    End Function
End Module

Class C
    Private Function GetAssemblyIdentity(types As IEnumerable(Of C)) As Object
        For Each t In types
            Dim x = Global.System.Linq.Enumerable.First((CType(((CType(((CType((t), Global.C))?.Something2()?()), Global.C)?.Something())), Global.System.Collections.Generic.IEnumerable(Of System.String))))
        Next
        Return Nothing
    End Function
End Class]]>
</code>

            Await TestAsync(input, expected)
        End Function
#End Region

#Region "CSharp ExtensionMethodRewrite Expansion tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestCSharp_ExpandSingleExtensionMethod() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestCSharp_ExpandSingleExtensionMethodWithArgument() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestCSharp_ExpandMultiExtensionMethod() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestCSharp_ExpandMultiExtensionMethodWithArgument() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestCSharp_ExpandMultiExtensionMethodWithMoreArgument() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestCSharp_ExpandSimplifySingleExtensionMethod() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestCSharp_ExpandSimplifyChainedExtensionMethodWithMoreArgument() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestCSharp_ExpandSimplifyWithStaticFieldExtensionMethod() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WorkItem(654403)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestCSharp_ExtensionMethodRewriteRoundTripsTrivia() As Task
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

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        <WorkItem(2593, "https://github.com/dotnet/roslyn/issues/2593")>
        <WorkItem(3260, "https://github.com/dotnet/roslyn/issues/3260")>
        Public Async Function TestCSharp_ExpandExtensionMethodInMemberAccessExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;

static class M
{
    public static IEnumerable<string> Something(this C cust)
    {
        throw new NotImplementedException();
    }
}

class C
{
    private object GetAssemblyIdentity(IEnumerable<C> types)
    {
        foreach (var t in types)
        {
            var x = {|Expand:t.Something().First()|};
        }
        return null;
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;

static class M
{
    public static IEnumerable<string> Something(this C cust)
    {
        throw new NotImplementedException();
    }
}

class C
{
    private object GetAssemblyIdentity(IEnumerable<C> types)
    {
        foreach (var t in types)
        {
            var x = global::System.Linq.Enumerable.First<global::System.String>(global::M.Something(t));
        }
        return null;
    }
}]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact(Skip:="3260"), Trait(Traits.Feature, Traits.Features.Expansion)>
        <WorkItem(2593, "https://github.com/dotnet/roslyn/issues/2593")>
        <WorkItem(3260, "https://github.com/dotnet/roslyn/issues/3260")>
        Public Async Function TestCSharp_ExpandExtensionMethodInConditionalAccessExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;

static class M
{
    public static IEnumerable<string> Something(this C cust)
    {
        throw new NotImplementedException();
    }
}

class C
{
    private object GetAssemblyIdentity(IEnumerable<C> types)
    {
        foreach (var t in types)
        {
            var x = {|Expand:t?.Something().First()|};
        }
        return null;
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;

static class M
{
    public static IEnumerable<string> Something(this C cust)
    {
        throw new NotImplementedException();
    }
}

class C
{
    private object GetAssemblyIdentity(IEnumerable<C> types)
    {
        foreach (var t in types)
        {
            var x = global::System.Linq.Enumerable.First<global::System.String>(t?.Something());
        }
        return null;
    }
}]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Expansion)>
        <WorkItem(2593, "https://github.com/dotnet/roslyn/issues/2593")>
        <WorkItem(3260, "https://github.com/dotnet/roslyn/issues/3260")>
        Public Async Function TestCSharp_ExpandExtensionMethodInMemberAccessExpression_2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;

static class M
{
    public static IEnumerable<string> Something(this C cust)
    {
        throw new NotImplementedException();
    }

    public static Func<C> Something2(this C cust)
    {
        throw new NotImplementedException();
    }
}

class C
{
    private object GetAssemblyIdentity(IEnumerable<C> types)
    {
        foreach (var t in types)
        {
            var x = {|Expand:(t.Something2())().Something().First()|};
        }
        return null;
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;

static class M
{
    public static IEnumerable<string> Something(this C cust)
    {
        throw new NotImplementedException();
    }

    public static Func<C> Something2(this C cust)
    {
        throw new NotImplementedException();
    }
}

class C
{
    private object GetAssemblyIdentity(IEnumerable<C> types)
    {
        foreach (var t in types)
        {
            var x = global::System.Linq.Enumerable.First<global::System.String>(global::M.Something((global::M.Something2(t))()));
        }
        return null;
    }
}]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact(Skip:="3260"), Trait(Traits.Feature, Traits.Features.Expansion)>
        <WorkItem(2593, "https://github.com/dotnet/roslyn/issues/2593")>
        <WorkItem(3260, "https://github.com/dotnet/roslyn/issues/3260")>
        Public Async Function TestCSharp_ExpandExtensionMethodInConditionalAccessExpression_2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;

static class M
{
    public static IEnumerable<string> Something(this C cust)
    {
        throw new NotImplementedException();
    }

    public static Func<C> Something2(this C cust)
    {
        throw new NotImplementedException();
    }
}

class C
{
    private object GetAssemblyIdentity(IEnumerable<C> types)
    {
        foreach (var t in types)
        {
            var x = {|Expand:(t?.Something2())()?.Something().First()|};
        }
        return null;
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;

static class M
{
    public static IEnumerable<string> Something(this C cust)
    {
        throw new NotImplementedException();
    }

    public static Func<C> Something2(this C cust)
    {
        throw new NotImplementedException();
    }
}

class C
{
    private object GetAssemblyIdentity(IEnumerable<C> types)
    {
        foreach (var t in types)
        {
            var x = global::System.Linq.Enumerable.First<global::System.String>((t?.Something2())()?.Something());
        }
        return null;
    }
}]]>
</code>

            Await TestAsync(input, expected)
        End Function
#End Region

    End Class
End Namespace