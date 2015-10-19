﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Editor
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportCompletionProviderAttribute : ExportAttribute
    {
        public string Name { get; }
        public string Language { get; }

        public ExportCompletionProviderAttribute(string name, string language)
            : base(typeof(CompletionListProvider))
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
