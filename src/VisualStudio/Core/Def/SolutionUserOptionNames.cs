// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.LanguageServices
{
    internal static class SolutionUserOptionNames
    {
        public static readonly ImmutableHashSet<string> AllOptionNames = ImmutableHashSet.Create(
            DoNotAddEditorConfigAsSlnItem);

        // NOTE: All option names must be less than 31 characters long and cannot contain special characters.
        public const string DoNotAddEditorConfigAsSlnItem = nameof(DoNotAddEditorConfigAsSlnItem);
    }
}
