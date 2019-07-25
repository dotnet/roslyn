// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.Format.Options
{
    internal sealed partial class CachingEditorConfigDocumentOptionsProvider
    {
        private class DocumentOptions : IDocumentOptions
        {
            private readonly OptionSet _options;

            public DocumentOptions(OptionSet options)
            {
                _options = options;
            }

            public bool TryGetDocumentOption(OptionKey option, out object value)
            {
                value = _options.GetOption(option);
                return value is object;
            }
        }
    }
}
