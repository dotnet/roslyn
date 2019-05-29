// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Implementation.EndConstructGeneration;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EndConstructGeneration
{
    [ExportLanguageService(typeof(IEndConstructGenerationService), LanguageNames.CSharp), Shared]
    [ExcludeFromCodeCoverage]
    internal class CSharpEndConstructGenerationService : IEndConstructGenerationService
    {
        [ImportingConstructor]
        public CSharpEndConstructGenerationService()
        {
        }

        public bool TryDo(
            ITextView textView,
            ITextBuffer subjectBuffer,
            char typedChar,
            CancellationToken cancellationToken)
        {
            return false;
        }
    }
}
