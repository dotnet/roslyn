// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    /// <summary>
    /// Use this attribute to export a <see cref="SignatureHelpProvider"/> that will
    /// be accessed by a <see cref="SignatureHelpServiceWithProviders"/> service.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportSignatureHelpProviderAttribute : ExportAttribute
    {
        public string Name { get; }
        public string Language { get; }
        public string[] Roles { get; set; }

        public ExportSignatureHelpProviderAttribute(string name, string language)
            : base(typeof(SignatureHelpProvider))
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Language = language ?? throw new ArgumentNullException(nameof(language));
        }
    }
}
