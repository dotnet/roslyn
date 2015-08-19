' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WorkItem(11003, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAnonymousDelegateInvoke1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Function Main(args As String())
        Dim q = Function(e As Integer)
                    Return True
                End Function.$$[|Invoke|](42)
 
        Dim r = Function(e2 As Integer)
                    Return True
                End Function.[|Invoke|](42)
    End Function
End Module
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub
    End Class
End Namespace
