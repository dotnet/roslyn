' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

imports System.Collections.Generic
imports System.Collections.Immutable
imports System.Linq
imports Microsoft.CodeAnalysis.CodeStyle
imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeStyle
    Friend Module VisualBasicCodeStyleOptions

        Private ReadOnly s_preferredModifierOrderDefault As SyntaxKind() =
            {
                SyntaxKind.PublicKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.FriendKeyword,
                SyntaxKind.SharedKeyword, SyntaxKind.ShadowsKeyword,
                SyntaxKind.MustInheritKeyword, SyntaxKind.OverloadsKeyword, SyntaxKind.NotInheritableKeyword, SyntaxKind.OverridesKeyword,
                SyntaxKind.NotOverridableKeyword, SyntaxKind.OverridableKeyword, SyntaxKind.MustOverrideKeyword,
                SyntaxKind.ReadOnlyKeyword, SyntaxKind.WriteOnlyKeyword,
                SyntaxKind.WideningKeyword, SyntaxKind.NarrowingKeyword,
                SyntaxKind.DimKeyword, SyntaxKind.ConstKeyword, SyntaxKind.StaticKeyword, SyntaxKind.DefaultKeyword, SyntaxKind.WithEventsKeyword, SyntaxKind.CustomKeyword,
                SyntaxKind.PartialKeyword,
                SyntaxKind.AsyncKeyword,
                SyntaxKind.IteratorKeyword
            }

        Public ReadOnly PreferredModifierOrder As [Option](Of CodeStyleOption(Of String)) = New [Option](Of CodeStyleOption(Of String))(
            NameOf(CodeStyleOptions), NameOf(PreferredModifierOrder),
            New CodeStyleOption(Of String)(String.Join(",", s_preferredModifierOrderDefault.Select(AddressOf SyntaxFacts.GetText)), NotificationOption.None),
            EditorConfigStorageLocation.ForStringCodeStyleOption("visual_basic_preferred_modifier_order"),
            New RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{NameOf(PreferredModifierOrder)}"))
    End Module
End Namespace