' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    Public Class ExtensionMethodSimplificationTests
        Inherits AbstractSimplificationTests

#Region "Visual Basic tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyExtensionMethodOnce()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = {|SimplifyExtension:Global.ProgramExtensions.foo([p])|}
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
        Dim ss = [p].foo()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyExtensionMethodChained()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Runtime.CompilerServices

Public Class Program
    Public Sub Main(args As String())
        Dim p As Program = Nothing
        Dim ss = {|SimplifyExtension:Global.ProgramExtensions.[foo]({|SimplifyExtension:Global.ProgramExtensions.[foo]([p])|})|}
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
        Dim ss = [p].[foo]().[foo]()
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

#End Region


#Region "CSharp tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyExtensionMethodOnce()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Program s = {|SimplifyExtension:global::ProgramExtensions.foo(@ss)|};
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
        Program s = @ss.foo();
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyExtensionMethodChained()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class Program
{
    static void Main(string[] args)
    {
        Program ss = null;
        Program s = {|SimplifyExtension:global::ProgramExtensions.foo({|SimplifyExtension:global::ProgramExtensions.foo(ss)|})|};
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
        Program s = ss.foo().foo();
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
#End Region

    End Class
End Namespace
