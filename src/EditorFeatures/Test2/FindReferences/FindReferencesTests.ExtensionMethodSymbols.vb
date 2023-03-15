' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541167")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestExtensionMethodToDelegateConversion(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541697")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestReducedExtensionMethod1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541697")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestReducedExtensionMethod2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

#Region "Normal Visual Basic Tests"

        <WpfTheory, CombinatorialData>
        Public Async Function TestVisualBasicFindReferencesOnExtensionMethod(kind As TestKind, host As TestHost) As Task
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

            Await TestAPIAndFeature(input, kind, host)
        End Function

#End Region
    End Class
End Namespace
