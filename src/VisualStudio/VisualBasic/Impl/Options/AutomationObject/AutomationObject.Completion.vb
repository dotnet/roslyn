' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Completion

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Public Class AutomationObject
        Public Property Option_TriggerOnTypingLetters As Boolean
            Get
                Return GetBooleanOption(CompletionOptionsMetadata.TriggerOnTypingLetters)
            End Get
            Set(value As Boolean)
                SetBooleanOption(CompletionOptionsMetadata.TriggerOnTypingLetters, value)
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
                Return GetOption(CompletionOptionsMetadata.EnterKeyBehavior)
            End Get
            Set(value As Integer)
                SetOption(CompletionOptionsMetadata.EnterKeyBehavior, DirectCast(value, EnterKeyRule))
            End Set
        End Property

        Public Property Option_SnippetsBehavior As Integer
            Get
                Return GetOption(CompletionOptionsMetadata.SnippetsBehavior)
            End Get
            Set(value As Integer)
                SetOption(CompletionOptionsMetadata.SnippetsBehavior, DirectCast(value, SnippetsRule))
            End Set
        End Property

        Public Property Option_ShowItemsFromUnimportedNamespaces As Integer
            Get
                Return GetBooleanOption(CompletionOptionsMetadata.ShowItemsFromUnimportedNamespaces)
            End Get
            Set(value As Integer)
                SetBooleanOption(CompletionOptionsMetadata.ShowItemsFromUnimportedNamespaces, value)
            End Set
        End Property

        Public Property Option_TriggerInArgumentLists As Boolean
            Get
                Return GetBooleanOption(CompletionOptionsMetadata.TriggerInArgumentLists)
            End Get
            Set(value As Boolean)
                SetBooleanOption(CompletionOptionsMetadata.TriggerInArgumentLists, value)
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
