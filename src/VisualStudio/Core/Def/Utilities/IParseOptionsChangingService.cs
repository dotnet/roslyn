// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable 

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Utilities
{
    internal interface IParseOptionsChangingService : ILanguageService
    {
        bool CanApplyChange(ParseOptions oldOptions, ParseOptions newOptions, string? maxSupportedLangVersion);

        void Apply(ParseOptions options, ProjectPropertyStorage storage);
    }
}
