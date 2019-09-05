// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching;

namespace Microsoft.CodeAnalysis.Editor.CSharp.BraceMatching
{
    [ExportBraceMatcher(LanguageNames.CSharp)]
    internal class CSharpEmbeddedLanguageBraceMatcher : AbstractEmbeddedLanguageBraceMatcher
    {
        [ImportingConstructor]
        public CSharpEmbeddedLanguageBraceMatcher()
        {
        }
    }
}
