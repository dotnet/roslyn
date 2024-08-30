// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Test.Utilities.Snippets;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

public abstract class AbstractCSharpSnippetProviderTests : AbstractSnippetProviderTests
{
    protected sealed override string LanguageName => LanguageNames.CSharp;
}
