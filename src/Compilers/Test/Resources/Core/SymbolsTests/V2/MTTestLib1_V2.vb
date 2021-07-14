' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'vbc /t:library /out:MTTestLib1.Dll MTTestLib1_V2.vb 
'vbc /t:module /out:MTTestModule1.netmodule MTTestLib1_V2.vb

<Assembly: System.Reflection.AssemblyVersion("2.0.0.0")> 
<Assembly: System.Reflection.AssemblyFileVersion("2.0.0.0")> 

Public Class Class1

End Class

Public Class Class2

End Class

Public Delegate Sub Delegate1()

Public Interface Interface1
    Sub Method1() ' same as V1
    ' Sub Method2() ' removed since V1
    Sub Method3(x As Integer) ' different param type in V2
    Sub Method4(x As Class1) ' new version of param type in V2
    Property Property1 As String ' same as V1
    ' Property Property2 As String ' removed since V1
    Property Property3 As Integer ' different type in V2
    Property Property4 As Class1 ' new version of type in V2
    Default Property Indexer(x As String) As String ' same as V1
    ' Default Property Indexer(x As String, y As String) As String ' removed since V1
    Default Property Indexer(x As Integer, y As Integer, z As Integer) As String ' different param type in V2
    Default Property Indexer(x As Class1, y As Class1, z As Class1, w As Class1) As Class1 ' new version of type in V2
    Event Event1 As System.Action ' same in V2
    ' Event Event2 As System.Action ' gone in V2
    Event Event3 As System.Action(Of Integer) ' different type in V2
    Event Event4 As Delegate1 ' new version of type in V2
End Interface

Public Interface Interface2(Of T)
    Sub Method1(t As T) ' same in V2
    Property Property1 As T ' same in V2
    Event Event1 As System.Action(Of T) ' same in V2
End Interface
