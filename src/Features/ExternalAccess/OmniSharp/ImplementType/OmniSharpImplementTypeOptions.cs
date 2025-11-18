// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ImplementType;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.ImplementType;

internal readonly record struct OmniSharpImplementTypeOptions(
    OmniSharpImplementTypeInsertionBehavior InsertionBehavior,
    OmniSharpImplementTypePropertyGenerationBehavior PropertyGenerationBehavior);

internal enum OmniSharpImplementTypeInsertionBehavior
{
    WithOtherMembersOfTheSameKind = ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind,
    AtTheEnd = ImplementTypeInsertionBehavior.AtTheEnd,
}

internal enum OmniSharpImplementTypePropertyGenerationBehavior
{
    PreferThrowingProperties = ImplementTypePropertyGenerationBehavior.PreferThrowingProperties,
    PreferAutoProperties = ImplementTypePropertyGenerationBehavior.PreferAutoProperties,
}
