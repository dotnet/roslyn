// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// CodeFixProvider factory. if an analyzer reference implements this, we call this to get CodeFixProviders
    /// </summary>
    internal interface ICodeFixProviderFactory
    {
        ImmutableArray<CodeFixProvider> GetFixers();
    }
}
