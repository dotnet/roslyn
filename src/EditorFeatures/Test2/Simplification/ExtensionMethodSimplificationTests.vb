' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    <Trait(Traits.Feature, Traits.Features.Simplification)>
    Public Class ExtensionMethodSimplificationTests
        Inherits AbstractSimplificationTests

#Region "Visual Basic tests"
        <Fact>
        Public Async Function TestVisualBasic_SimplifyExtensionMethodOnce() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = {|SimplifyExtension:Global.ProgramExtensions.goo([p])|}
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function goo(ByVal Prog As Program) As Program
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
        Dim ss = [p].goo()
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function goo(ByVal Prog As Program) As Program
        Return Prog
    End Function
End Module
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestVisualBasic_SimplifyExtensionMethodChained() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = {|SimplifyExtension:Global.ProgramExtensions.[goo]({|SimplifyExtension:Global.ProgramExtensions.[goo]([p])|})|}
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function goo(ByVal Prog As Program) As Program
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
        Dim ss = [p].[goo]().[goo]()
    End Sub
End Class

Module ProgramExtensions
    &lt;Extension()&gt;
    Public Function goo(ByVal Prog As Program) As Program
        Return Prog
    End Function
End Module
</code>

            Await TestAsync(input, expected)

        End Function

#End Region

#Region "CSharp tests"
        <Fact>
        Public Async Function TestCSharp_SimplifyExtensionMethodOnce() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Program s = {|SimplifyExtension:global::ProgramExtensions.goo(@ss)|};
    }
}

public static class ProgramExtensions
{
    public static Program goo(this Program p)
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
        Program s = @ss.goo();
    }
}

public static class ProgramExtensions
{
    public static Program goo(this Program p)
    {
        return p;
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <Fact>
        Public Async Function TestCSharp_SimplifyExtensionMethodChained() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Program s = {|SimplifyExtension:global::ProgramExtensions.goo({|SimplifyExtension:global::ProgramExtensions.goo(ss)|})|};
    }
}

public static class ProgramExtensions
{
    public static Program goo(this Program p)
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
        Program s = ss.goo().goo();
    }
}

public static class ProgramExtensions
{
    public static Program goo(this Program p)
    {
        return p;
    }
}
</code>

            Await TestAsync(input, expected)

        End Function
#End Region

    End Class
End Namespace
