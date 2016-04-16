// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportOptionSerializerAttribute : ExportAttribute
    {
        public ExportOptionSerializerAttribute(params string[] features) : base(typeof(IOptionSerializer))
        {
            this.Features = features;
        }

        public IEnumerable<string> Features { get; set; }
    }
}
