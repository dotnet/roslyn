// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionSize
{
    internal static class SolutionSizeOptions
    {
        internal static readonly Option<bool> ComputeSolutionSize = new Option<bool>(
            nameof(SolutionSizeOptions), nameof(ComputeSolutionSize), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(@"Roslyn\Internal\ComputeSolutionSize"));
    }
}
