// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.PreferFrameworkType
{
    internal static class PreferFrameworkTypeConstants
    {
        public const string PreferFrameworkType = nameof(PreferFrameworkType);
        public static readonly ImmutableDictionary<string, string> Properties =
            ImmutableDictionary<string, string>.Empty.Add(PreferFrameworkType, "");
    }
}
