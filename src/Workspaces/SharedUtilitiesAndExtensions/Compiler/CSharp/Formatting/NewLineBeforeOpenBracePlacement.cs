// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

[Flags]
internal enum NewLineBeforeOpenBracePlacement
{
    None = 0,
    Types = 1,
    Methods = 1 << 1,
    Properties = 1 << 2,
    AnonymousMethods = 1 << 3,
    ControlBlocks = 1 << 4,
    AnonymousTypes = 1 << 5,
    ObjectCollectionArrayInitializers = 1 << 6,
    LambdaExpressionBody = 1 << 7,
    Accessors = 1 << 8,
    All = (1 << 9) - 1
}

internal static partial class Extensions
{
    public static NewLineBeforeOpenBracePlacement ToNewLineBeforeOpenBracePlacement(this NewLinePlacement value)
        => (value.HasFlag(NewLinePlacement.BeforeOpenBraceInTypes) ? NewLineBeforeOpenBracePlacement.Types : 0) |
           (value.HasFlag(NewLinePlacement.BeforeOpenBraceInMethods) ? NewLineBeforeOpenBracePlacement.Methods : 0) |
           (value.HasFlag(NewLinePlacement.BeforeOpenBraceInProperties) ? NewLineBeforeOpenBracePlacement.Properties : 0) |
           (value.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousMethods) ? NewLineBeforeOpenBracePlacement.AnonymousMethods : 0) |
           (value.HasFlag(NewLinePlacement.BeforeOpenBraceInControlBlocks) ? NewLineBeforeOpenBracePlacement.ControlBlocks : 0) |
           (value.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousTypes) ? NewLineBeforeOpenBracePlacement.AnonymousTypes : 0) |
           (value.HasFlag(NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers) ? NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers : 0) |
           (value.HasFlag(NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody) ? NewLineBeforeOpenBracePlacement.LambdaExpressionBody : 0) |
           (value.HasFlag(NewLinePlacement.BeforeOpenBraceInAccessors) ? NewLineBeforeOpenBracePlacement.Accessors : 0);

    public static NewLinePlacement ToNewLinePlacement(this NewLineBeforeOpenBracePlacement value)
        => (value.HasFlag(NewLineBeforeOpenBracePlacement.Types) ? NewLinePlacement.BeforeOpenBraceInTypes : 0) |
           (value.HasFlag(NewLineBeforeOpenBracePlacement.Methods) ? NewLinePlacement.BeforeOpenBraceInMethods : 0) |
           (value.HasFlag(NewLineBeforeOpenBracePlacement.Properties) ? NewLinePlacement.BeforeOpenBraceInProperties : 0) |
           (value.HasFlag(NewLineBeforeOpenBracePlacement.AnonymousMethods) ? NewLinePlacement.BeforeOpenBraceInAnonymousMethods : 0) |
           (value.HasFlag(NewLineBeforeOpenBracePlacement.ControlBlocks) ? NewLinePlacement.BeforeOpenBraceInControlBlocks : 0) |
           (value.HasFlag(NewLineBeforeOpenBracePlacement.AnonymousTypes) ? NewLinePlacement.BeforeOpenBraceInAnonymousTypes : 0) |
           (value.HasFlag(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers) ? NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers : 0) |
           (value.HasFlag(NewLineBeforeOpenBracePlacement.LambdaExpressionBody) ? NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody : 0) |
           (value.HasFlag(NewLineBeforeOpenBracePlacement.Accessors) ? NewLinePlacement.BeforeOpenBraceInAccessors : 0);

    public static NewLineBeforeOpenBracePlacement WithFlagValue(this NewLineBeforeOpenBracePlacement flags, NewLineBeforeOpenBracePlacement flag, bool value)
        => (flags & ~flag) | (value ? flag : 0);
}
