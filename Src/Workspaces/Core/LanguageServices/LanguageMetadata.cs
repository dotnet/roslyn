// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal class LanguageMetadata : ILanguageMetadata
    {
        public string Language { get; private set; }

        public LanguageMetadata(IDictionary<string, object> data)
        {
            this.Language = (string)data.GetValueOrDefault("Language");
        }

        public LanguageMetadata(string language)
        {
            this.Language = language;
        }
    }
}