// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.GoOrFind;

internal interface IGoOrFindNavigationService
{
    string DisplayName { get; }

    bool IsAvailable([NotNullWhen(true)] Document? document);
    bool ExecuteCommand(Document document, int position, bool allowInvalidPosition);
}
