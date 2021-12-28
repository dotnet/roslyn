' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WorkItem(11003, "DevDiv_Projects/Roslyn")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousDelegateInvoke1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Function Main(args As String())
        Dim q = {|Definition:Function(e As Integer)
                    Return True
                End Function|}.$$[|Invoke|](42)
 
        Dim r = Function(e2 As Integer)
                    Return True
                End Function.[|Invoke|](42)
    End Function
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
