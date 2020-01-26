// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Language = language ?? throw new ArgumentNullException(nameof(language));
        }
    }
}
