' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Completion

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Public Class AutomationObject
        Public Property Option_TriggerOnTypingLetters As Boolean
            Get
                Return GetBooleanOption(CompletionOptionsStorage.TriggerOnTypingLetters)
            End Get
            Set(value As Boolean)
                SetBooleanOption(CompletionOptionsStorage.TriggerOnTypingLetters, value)
            End Set
        End Property

        Public Property Option_HighlightMatchingPortionsOfCompletionListItems As Boolean
            Get
                Return GetBooleanOption(CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems)
            End Get
            Set(value As Boolean)
                SetBooleanOption(CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems, value)
            End Set
        End Property

        Public Property Option_EnterKeyBehavior As Integer
            Get
                Return GetOption(CompletionOptionsStorage.EnterKeyBehavior)
            End Get
            Set(value As Integer)
                SetOption(CompletionOptionsStorage.EnterKeyBehavior, DirectCast(value, EnterKeyRule))
            End Set
        End Property

        Public Property Option_SnippetsBehavior As Integer
            Get
                Return GetOption(CompletionOptionsStorage.SnippetsBehavior)
            End Get
            Set(value As Integer)
                SetOption(CompletionOptionsStorage.SnippetsBehavior, DirectCast(value, SnippetsRule))
            End Set
        End Property

        Public Property Option_ShowItemsFromUnimportedNamespaces As Integer
            Get
                Return GetBooleanOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces)
            End Get
            Set(value As Integer)
                SetBooleanOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, value)
            End Set
        End Property

        Public Property Option_TriggerInArgumentLists As Boolean
            Get
                Return GetBooleanOption(CompletionOptionsStorage.TriggerInArgumentLists)
            End Get
            Set(value As Boolean)
                SetBooleanOption(CompletionOptionsStorage.TriggerInArgumentLists, value)
            End Set
        End Property

        Public Property Option_EnableArgumentCompletionSnippets As Integer
            Get
                Return GetBooleanOption(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets)
            End Get
            Set(value As Integer)
                SetBooleanOption(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, value)
            End Set
        End Property
    End Class
End Namespace
