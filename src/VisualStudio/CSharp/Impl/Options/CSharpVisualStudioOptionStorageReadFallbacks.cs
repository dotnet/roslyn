// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp;

internal static class CSharpVisualStudioOptionStorageReadFallbacks
{
    [ExportVisualStudioStorageReadFallback("csharp_space_between_parentheses"), Shared]
    internal sealed class SpaceBetweenParentheses : IVisualStudioStorageReadFallback
    {
        private static readonly ImmutableArray<(string key, int flag)> s_storages =
        [
            ("TextEditor.CSharp.Specific.SpaceWithinExpressionParentheses", (int)SpacePlacementWithinParentheses.Expressions),
            ("TextEditor.CSharp.Specific.SpaceWithinCastParentheses", (int)SpacePlacementWithinParentheses.TypeCasts),
            ("TextEditor.CSharp.Specific.SpaceWithinOtherParentheses", (int)SpacePlacementWithinParentheses.ControlFlowStatements),
        ];

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SpaceBetweenParentheses()
        {
        }

        public Optional<object?> TryRead(string? language, TryReadValueDelegate readValue)
            => TryReadFlags(s_storages, (int)CSharpFormattingOptions2.SpaceBetweenParentheses.DefaultValue, readValue, out var intValue) ? (SpacePlacementWithinParentheses)intValue : default(Optional<object?>);
    }

    [ExportVisualStudioStorageReadFallback("csharp_new_line_before_open_brace"), Shared]
    internal sealed class NewLinesForBraces : IVisualStudioStorageReadFallback
    {
        private static readonly ImmutableArray<(string key, int flag)> s_storages =
        [
            ("TextEditor.CSharp.Specific.NewLinesForBracesInTypes", (int)NewLineBeforeOpenBracePlacement.Types),
            ("TextEditor.CSharp.Specific.NewLinesForBracesInAnonymousTypes", (int)NewLineBeforeOpenBracePlacement.AnonymousTypes),
            ("TextEditor.CSharp.Specific.NewLinesForBracesInObjectCollectionArrayInitializers", (int)NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers),
            ("TextEditor.CSharp.Specific.NewLinesForBracesInProperties", (int)NewLineBeforeOpenBracePlacement.Properties),
            ("TextEditor.CSharp.Specific.NewLinesForBracesInMethods", (int)NewLineBeforeOpenBracePlacement.Methods),
            ("TextEditor.CSharp.Specific.NewLinesForBracesInAccessors", (int)NewLineBeforeOpenBracePlacement.Accessors),
            ("TextEditor.CSharp.Specific.NewLinesForBracesInAnonymousMethods", (int)NewLineBeforeOpenBracePlacement.AnonymousMethods),
            ("TextEditor.CSharp.Specific.NewLinesForBracesInLambdaExpressionBody", (int)NewLineBeforeOpenBracePlacement.LambdaExpressionBody),
            ("TextEditor.CSharp.Specific.NewLinesForBracesInControlBlocks", (int)NewLineBeforeOpenBracePlacement.ControlBlocks),
        ];

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NewLinesForBraces()
        {
        }

        public Optional<object?> TryRead(string? language, TryReadValueDelegate readValue)
            => TryReadFlags(s_storages, (int)CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue, readValue, out var intValue) ? (NewLineBeforeOpenBracePlacement)intValue : default(Optional<object?>);
    }

    /// <summary>
    /// Returns true if an option for any flag is present in the storage. Each flag in the result will be either read from the storage 
    /// (if present) or from <paramref name="defaultValue"/> otherwise.
    /// Returns false if none of the flags are present in the storage.
    /// </summary>
    private static bool TryReadFlags(ImmutableArray<(string key, int flag)> storages, int defaultValue, TryReadValueDelegate read, out int result)
    {
        var hasAnyFlag = false;
        result = 0;
        foreach (var (key, flag) in storages)
        {
            var defaultFlagValue = defaultValue & flag;
            var value = read(key, typeof(bool), Boxes.Box(defaultFlagValue != 0));
            if (value.HasValue)
            {
                if ((bool)value.Value!)
                {
                    result |= flag;
                }

                hasAnyFlag = true;
            }
            else
            {
                result |= defaultFlagValue;
            }
        }

        return hasAnyFlag;
    }
}
