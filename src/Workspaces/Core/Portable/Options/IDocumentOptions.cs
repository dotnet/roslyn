// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Returned from a <see cref="IDocumentOptionsProvider"/>
    /// </summary>
    interface IDocumentOptions
    {
        bool TryGetDocumentOption(OptionKey option, out object? value);
    }
}
