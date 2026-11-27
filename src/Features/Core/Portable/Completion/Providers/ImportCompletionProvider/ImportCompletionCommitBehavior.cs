// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal enum ImportCompletionCommitBehavior
{
    AlwaysAddImport,
    NeverAddImport,
    OnlyAddImportIfExplicitlyCompleted,     // via TAB or explicitly selected in completion list
}
