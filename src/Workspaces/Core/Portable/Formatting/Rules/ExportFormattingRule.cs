// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// Specifies the exact type of the formatting rule exported
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportFormattingRule : ExportAttribute
    {
        public string Name { get; }
        public string Language { get; }

        public ExportFormattingRule(string name, string language)
            : base(typeof(IFormattingRule))
        {
            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            this.Name = name;
            this.Language = language;
        }
    }
}
