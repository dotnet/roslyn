' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class YieldCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Friend Overrides Function GetCompletionProviderType() As Type
            Return GetType(YieldCompletionProvider)
        End Function

        Private Function Normalize(text As String) As String
            Return text.Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf)
        End Function

        <WpfFact>
        Public Async Function TestInIteratorFunction() As Task
            Await VerifyItemExistsAsync("
Imports System.Collections.Generic
Class C
    Iterator Function M() As IEnumerable(Of Integer)
        $$
    End Function
End Class
", "Yield")
        End Function

        <WpfFact>
        Public Async Function TestInAsyncIteratorFunction() As Task
            Await VerifyItemExistsAsync("
Imports System.Collections.Generic
Class C
    Async Iterator Function M() As IAsyncEnumerable(Of Integer)
        $$
    End Function
End Class
", "Yield")
        End Function

        <WpfFact>
        Public Async Function TestNotInSub() As Task
            Await VerifyItemIsAbsentAsync("
Class C
    Sub M()
        $$
    End Sub
End Class
", "Yield")
        End Function

        <WpfFact>
        Public Async Function TestAddIteratorModifier() As Task
            Await VerifyCustomCommitProviderAsync("
Imports System.Collections.Generic
Class C
    Function M() As IEnumerable(Of Integer)
        $$
    End Function
End Class
", "Yield", Normalize("
Imports System.Collections.Generic
Class C
    Iterator Function M() As IEnumerable(Of Integer)
        Yield
    End Function
End Class
"))
        End Function

        <WpfFact>
        Public Async Function TestAddAsyncAndIteratorModifier() As Task
            Await VerifyCustomCommitProviderAsync("
Imports System.Collections.Generic
Class C
    Function M() As IAsyncEnumerable(Of Integer)
        $$
    End Function
End Class
", "Yield", Normalize("
Imports System.Collections.Generic
Class C
    Async Iterator Function M() As IAsyncEnumerable(Of Integer)
        Yield
    End Function
End Class
"))
        End Function

        <WpfFact>
        Public Async Function TestDoNotAddModifiersIfPresent() As Task
            Await VerifyCustomCommitProviderAsync("
Imports System.Collections.Generic
Class C
    Iterator Function M() As IEnumerable(Of Integer)
        $$
    End Function
End Class
", "Yield", Normalize("
Imports System.Collections.Generic
Class C
    Iterator Function M() As IEnumerable(Of Integer)
        Yield
    End Function
End Class
"))
        End Function

        <WpfFact>
        Public Async Function TestDoNotAddAsyncToIEnumerable() As Task
            Await VerifyCustomCommitProviderAsync("
Imports System.Collections.Generic
Class C
    Function M() As IEnumerable(Of Integer)
        $$
    End Function
End Class
", "Yield", Normalize("
Imports System.Collections.Generic
Class C
    Iterator Function M() As IEnumerable(Of Integer)
        Yield
    End Function
End Class
"))
        End Function
        
        <WpfFact>
        Public Async Function TestInPropertyGetter() As Task
            Await VerifyItemExistsAsync("
Imports System.Collections.Generic
Class C
    ReadOnly Property P As IEnumerable(Of Integer)
        Get
            $$
        End Get
    End Property
End Class
", "Yield")
        End Function

        <WpfFact>
        Public Async Function TestAddIteratorToPropertyGetter() As Task
            Await VerifyCustomCommitProviderAsync("
Imports System.Collections.Generic
Class C
    ReadOnly Property P As IEnumerable(Of Integer)
        Get
            $$
        End Get
    End Property
End Class
", "Yield", Normalize("
Imports System.Collections.Generic
Class C
    ReadOnly Property P As IEnumerable(Of Integer)
        Iterator Get
            Yield
        End Get
    End Property
End Class
"))
        End Function
    End Class
End Namespace