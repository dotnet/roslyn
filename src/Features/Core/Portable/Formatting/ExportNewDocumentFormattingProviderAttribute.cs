// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Formatting
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportNewDocumentFormattingProviderAttribute(string languageName) : ExportAttribute(typeof(INewDocumentFormattingProvider))
    {
        public string Language { get; } = languageName;
    }
}
