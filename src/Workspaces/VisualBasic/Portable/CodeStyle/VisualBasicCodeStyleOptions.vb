' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

imports System.Collections.Generic
imports System.Collections.Immutable
imports System.Linq
imports Microsoft.CodeAnalysis.CodeStyle
imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeStyle
    Friend Module VisualBasicCodeStyleOptions
        Public ReadOnly PreferredModifierOrderDefault As ImmutableArray(Of SyntaxKind) =
            ImmutableArray.Create(
                SyntaxKind.PartialKeyword, SyntaxKind.DefaultKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword,
                SyntaxKind.PublicKeyword, SyntaxKind.FriendKeyword, SyntaxKind.NotOverridableKeyword, SyntaxKind.OverridableKeyword,
                SyntaxKind.MustOverrideKeyword, SyntaxKind.OverloadsKeyword, SyntaxKind.OverridesKeyword, SyntaxKind.MustInheritKeyword,
                SyntaxKind.NotInheritableKeyword, SyntaxKind.StaticKeyword, SyntaxKind.SharedKeyword, SyntaxKind.ShadowsKeyword,
                SyntaxKind.ReadOnlyKeyword, SyntaxKind.WriteOnlyKeyword, SyntaxKind.DimKeyword, SyntaxKind.ConstKeyword,
                SyntaxKind.WithEventsKeyword, SyntaxKind.WideningKeyword, SyntaxKind.NarrowingKeyword, SyntaxKind.CustomKeyword,
                SyntaxKind.AsyncKeyword, SyntaxKind.IteratorKeyword)

        Public ReadOnly PreferredModifierOrder As [Option](Of CodeStyleOption(Of String)) = New [Option](Of CodeStyleOption(Of String))(
            NameOf(CodeStyleOptions), NameOf(PreferredModifierOrder),
            New CodeStyleOption(Of String)(String.Join(",", PreferredModifierOrderDefault.Select(AddressOf SyntaxFacts.GetText)), NotificationOption.Silent),
            EditorConfigStorageLocation.ForStringCodeStyleOption("visual_basic_preferred_modifier_order"),
            New RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{NameOf(PreferredModifierOrder)}"))

    End Module
End Namespace
