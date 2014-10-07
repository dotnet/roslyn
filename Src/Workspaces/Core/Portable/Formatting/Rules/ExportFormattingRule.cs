// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public string Name { get; private set; }
        public string Language { get; private set; }

        public ExportFormattingRule(string name, string language)
            : base(typeof(IFormattingRule))
        {
            if (language == null)
            {
                throw new ArgumentNullException("language");
            }

            this.Name = name;
            this.Language = language;
        }
    }
}