// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
