// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Formatting
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportNewDocumentFormattingProviderAttribute : ExportAttribute
    {
        public ExportNewDocumentFormattingProviderAttribute(string languageName)
            : base(typeof(INewDocumentFormattingProvider))
        {
            Language = languageName;
        }

        public string Language { get; }
    }
}
