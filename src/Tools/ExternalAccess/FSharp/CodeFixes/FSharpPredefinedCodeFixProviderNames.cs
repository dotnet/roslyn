// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.CodeFixes
{
    internal static class FSharpPredefinedCodeFixProviderNames
    {
        // Normally this would be a property, but this is used inside an attribute.
        public const string SimplifyNames = PredefinedCodeFixProviderNames.SimplifyNames;
    }
}
