// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;

internal record UnifiedSettingsEnumOption : UnifiedSettingsOption<string>
{
    public required string[] @Enum { get; init; }

    public required string[] EnumLabel { get; init; }
}
