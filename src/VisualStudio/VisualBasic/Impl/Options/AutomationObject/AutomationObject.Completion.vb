' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Completion

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Public Class AutomationObject
        Public Property Option_TriggerOnTypingLetters As Boolean
            Get
                Return GetBooleanOption(CompletionOptions.Metadata.TriggerOnTypingLetters)
            End Get
            Set(value As Boolean)
                SetBooleanOption(CompletionOptions.Metadata.TriggerOnTypingLetters, value)
            End Set
        End Property

        Public Property Option_HighlightMatchingPortionsOfCompletionListItems As Boolean
            Get
                Return GetBooleanOption(CompletionViewOptions.HighlightMatchingPortionsOfCompletionListItems)
            End Get
            Set(value As Boolean)
                SetBooleanOption(CompletionViewOptions.HighlightMatchingPortionsOfCompletionListItems, value)
            End Set
        End Property

        Public Property Option_EnterKeyBehavior As Integer
            Get
                Return GetOption(CompletionOptions.Metadata.EnterKeyBehavior)
            End Get
            Set(value As Integer)
                SetOption(CompletionOptions.Metadata.EnterKeyBehavior, DirectCast(value, EnterKeyRule))
            End Set
        End Property

        Public Property Option_SnippetsBehavior As Integer
            Get
                Return GetOption(CompletionOptions.Metadata.SnippetsBehavior)
            End Get
            Set(value As Integer)
                SetOption(CompletionOptions.Metadata.SnippetsBehavior, DirectCast(value, SnippetsRule))
            End Set
        End Property

        Public Property Option_ShowItemsFromUnimportedNamespaces As Integer
            Get
                Return GetBooleanOption(CompletionOptions.Metadata.ShowItemsFromUnimportedNamespaces)
            End Get
            Set(value As Integer)
                SetBooleanOption(CompletionOptions.Metadata.ShowItemsFromUnimportedNamespaces, value)
            End Set
        End Property

        Public Property Option_TriggerInArgumentLists As Boolean
            Get
                Return GetBooleanOption(CompletionOptions.Metadata.TriggerInArgumentLists)
            End Get
            Set(value As Boolean)
                SetBooleanOption(CompletionOptions.Metadata.TriggerInArgumentLists, value)
            End Set
        End Property

        Public Property Option_EnableArgumentCompletionSnippets As Integer
            Get
                Return GetBooleanOption(CompletionViewOptions.EnableArgumentCompletionSnippets)
            End Get
            Set(value As Integer)
                SetBooleanOption(CompletionViewOptions.EnableArgumentCompletionSnippets, value)
            End Set
        End Property
    End Class
End Namespace
