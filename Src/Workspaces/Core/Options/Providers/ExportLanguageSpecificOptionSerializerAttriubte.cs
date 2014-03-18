// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Options.Providers
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportLanguageSpecificOptionSerializerAttribute : ExportAttribute
    {
        public ExportLanguageSpecificOptionSerializerAttribute(string language, params string[] features) : base(typeof(IOptionSerializer))
        {
            this.Language = language;
            this.Features = features;
        }

        public string Language { get; set; }
        public IEnumerable<string> Features { get; set; }
    }
}
