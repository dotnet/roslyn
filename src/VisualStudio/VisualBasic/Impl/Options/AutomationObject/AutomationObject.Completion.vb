' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Completion

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Public Class AutomationObject
        Public Property Option_TriggerOnTypingLetters As Boolean
            Get
                Return GetBooleanOption(CompletionOptions.TriggerOnTypingLetters2)
            End Get
            Set(value As Boolean)
                SetBooleanOption(CompletionOptions.TriggerOnTypingLetters2, value)
            End Set
        End Property

        Public Property Option_HighlightMatchingPortionsOfCompletionListItems As Boolean
            Get
                Return GetBooleanOption(CompletionOptions.HighlightMatchingPortionsOfCompletionListItems)
            End Get
            Set(value As Boolean)
                SetBooleanOption(CompletionOptions.HighlightMatchingPortionsOfCompletionListItems, value)
            End Set
        End Property

        Public Property Option_EnterKeyBehavior As Integer
            Get
                Return GetOption(CompletionOptions.EnterKeyBehavior)
            End Get
            Set(value As Integer)
                SetOption(CompletionOptions.EnterKeyBehavior, DirectCast(value, EnterKeyRule))
            End Set
        End Property

        Public Property Option_SnippetsBehavior As Integer
            Get
                Return GetOption(CompletionOptions.SnippetsBehavior)
            End Get
            Set(value As Integer)
                SetOption(CompletionOptions.SnippetsBehavior, DirectCast(value, SnippetsRule))
            End Set
        End Property

        Public Property Option_ShowItemsFromUnimportedNamespaces As Integer
            Get
                Return GetBooleanOption(CompletionOptions.ShowItemsFromUnimportedNamespaces)
            End Get
            Set(value As Integer)
                SetBooleanOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, value)
            End Set
        End Property

        Public Property Option_TriggerInArgumentLists As Boolean
            Get
                Return GetBooleanOption(CompletionOptions.TriggerInArgumentLists)
            End Get
            Set(value As Boolean)
                SetBooleanOption(CompletionOptions.TriggerInArgumentLists, value)
            End Set
        End Property

        Public Property Option_EnableArgumentCompletionSnippets As Integer
            Get
                Return GetBooleanOption(CompletionOptions.EnableArgumentCompletionSnippets)
            End Get
            Set(value As Integer)
                SetBooleanOption(CompletionOptions.EnableArgumentCompletionSnippets, value)
            End Set
        End Property
    End Class
End Namespace
