' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WorkItem(541167)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestExtensionMethodToDelegateConversion()
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
            Test(input)
        End Sub

        <WorkItem(541697)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestReducedExtensionMethod1()
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
            Test(input)
        End Sub

        <WorkItem(541697)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestReducedExtensionMethod2()
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
            Test(input)
        End Sub

#Region "Normal Visual Basic Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub VisualBasicFindReferencesOnExtensionMethod()
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

            Test(input)
        End Sub

#End Region
    End Class
End Namespace
