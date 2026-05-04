// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeCleanup;

/// <summary>
/// Indicates which, if any, Organize Usings features are enabled for code cleanup.
/// </summary>
internal sealed class OrganizeUsingsSet
{
    public bool IsRemoveUnusedImportEnabled { get; }
    public bool IsSortImportsEnabled { get; }

    public OrganizeUsingsSet(bool isRemoveUnusedImportEnabled, bool isSortImportsEnabled)
    {
        IsRemoveUnusedImportEnabled = isRemoveUnusedImportEnabled;
        IsSortImportsEnabled = isSortImportsEnabled;
    }
}
