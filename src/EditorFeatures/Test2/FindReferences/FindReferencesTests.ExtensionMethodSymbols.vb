' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WorkItem(541167)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestExtensionMethodToDelegateConversion() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
using System.Linq;
 
class Program
{
    static void Main()
    {
        Func<int> x = "".[|$$Count|], y = "".[|Count|];
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(541697)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestReducedExtensionMethod1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
class Program
{
    static void Main(string[] args)
    {
        string s = "Hello";
        s.[|$$ExtensionMethod|]();
    }
}
 
 
public static class MyExtension
{
    public static int {|Definition:ExtensionMethod|}(this String s)
    {
        return s.Length;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(541697)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestReducedExtensionMethod2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
class Program
{
    static void Main(string[] args)
    {
        string s = "Hello";
        s.[|ExtensionMethod|]();
    }
}
 
 
public static class MyExtension
{
    public static int {|Definition:$$ExtensionMethod|}(this String s)
    {
        return s.Length;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

#Region "Normal Visual Basic Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVisualBasicFindReferencesOnExtensionMethod() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Method Or AttributeTargets.Property Or AttributeTargets.Class Or AttributeTargets.Assembly)>
    Public Class ExtensionAttribute
        Inherits Attribute
    End Class
End Namespace

Class Program
    Private Shared Sub Main(args As String())
        Dim s As String = "Hello"
        s.[|ExtensionMethod|]()
    End Sub
End Class

Module MyExtension
    <System.Runtime.CompilerServices.Extension()> _
    Public Function {|Definition:$$ExtensionMethod|}(s As [String]) As Integer
        Return s.Length
    End Function
End Module]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

#End Region
    End Class
End Namespace
