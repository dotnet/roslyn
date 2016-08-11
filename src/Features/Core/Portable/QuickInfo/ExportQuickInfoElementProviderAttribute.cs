// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportQuickInfoElementProviderAttribute : ExportAttribute
    {
        public string Name { get; }
        public string Language { get; }

        public ExportQuickInfoElementProviderAttribute(string name, string language)
            : base(typeof(QuickInfoElementProvider))
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            this.Name = name;
            this.Language = language;
        }
    }
}