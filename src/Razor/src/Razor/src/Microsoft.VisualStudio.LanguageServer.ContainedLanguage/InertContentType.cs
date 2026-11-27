// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

// It's possible to get this via ITextBufferFactoryService.InertContentType,
// but plumbing it through is ugly and this can be used in unit tests as well
internal class InertContentType : IContentType
{
    public static readonly IContentType Instance = new InertContentType();

    public string TypeName => "inert";

    public string DisplayName => TypeName;

    public IEnumerable<IContentType> BaseTypes => Enumerable.Empty<IContentType>();

    public bool IsOfType(string type) => string.Equals(type, TypeName, StringComparison.OrdinalIgnoreCase);
}
