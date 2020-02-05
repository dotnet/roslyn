' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities.ChangeSignature

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    Partial Public Class ChangeSignatureTests
        Inherits AbstractChangeSignatureTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function AddParameterAddsAllImports() As Task

            Dim markup = <Text><![CDATA[
Class C
    Sub $$M()
    End Sub
End Class
]]></Text>.NormalizedValue()

            Dim permutation = {New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Dictionary<ConsoleColor, Task<AsyncOperation>>", "test", "TODO"), "System.Collections.Generic.Dictionary(Of System.ConsoleColor, System.Threading.Tasks.Task(Of System.ComponentModel.AsyncOperation))")}

            Dim updatedCode = <Text><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Threading.Tasks

Class C
    Sub M(test As Dictionary(Of ConsoleColor, Task(Of AsyncOperation)))
    End Sub
End Class
]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function
    End Class
End Namespace
