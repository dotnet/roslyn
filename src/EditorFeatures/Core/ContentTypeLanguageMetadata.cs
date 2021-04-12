﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class ContentTypeLanguageMetadata : LanguageMetadata
    {
        public string DefaultContentType { get; }

        public ContentTypeLanguageMetadata(IDictionary<string, object> data)
            : base(data)
        {
            this.DefaultContentType = (string)data.GetValueOrDefault("DefaultContentType");
        }

        public ContentTypeLanguageMetadata(string defaultContentType, string language)
            : base(language)
        {
            this.DefaultContentType = defaultContentType;
        }
    }
}
