﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportSignatureHelpProviderAttribute : ExportAttribute
    {
        public string Name { get; }
        public string Language { get; }

        public ExportSignatureHelpProviderAttribute(string name, string language)
            : base(typeof(ISignatureHelpProvider))
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Language = language ?? throw new ArgumentNullException(nameof(language));
        }
    }
}
