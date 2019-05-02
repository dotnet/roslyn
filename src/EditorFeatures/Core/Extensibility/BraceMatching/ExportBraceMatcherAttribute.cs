// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Editor
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal class ExportBraceMatcherAttribute : ExportAttribute
    {
        public string Language { get; }

        public ExportBraceMatcherAttribute(string language)
            : base(typeof(IBraceMatcher))
        {
            this.Language = language ?? throw new ArgumentNullException(nameof(language));
        }
    }
}
