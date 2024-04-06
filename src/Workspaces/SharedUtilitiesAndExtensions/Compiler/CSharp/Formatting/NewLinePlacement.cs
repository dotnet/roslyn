// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

[Flags]
internal enum NewLinePlacement
{
    BeforeMembersInObjectInitializers = 1,
    BeforeMembersInAnonymousTypes = 1 << 1,
    BeforeElse = 1 << 2,
    BeforeCatch = 1 << 3,
    BeforeFinally = 1 << 4,
    BeforeOpenBraceInTypes = 1 << 5,
    BeforeOpenBraceInAnonymousTypes = 1 << 6,
    BeforeOpenBraceInObjectCollectionArrayInitializers = 1 << 7,
    BeforeOpenBraceInProperties = 1 << 8,
    BeforeOpenBraceInMethods = 1 << 9,
    BeforeOpenBraceInAccessors = 1 << 10,
    BeforeOpenBraceInAnonymousMethods = 1 << 11,
    BeforeOpenBraceInLambdaExpressionBody = 1 << 12,
    BeforeOpenBraceInControlBlocks = 1 << 13,
    BetweenQueryExpressionClauses = 1 << 14
}
