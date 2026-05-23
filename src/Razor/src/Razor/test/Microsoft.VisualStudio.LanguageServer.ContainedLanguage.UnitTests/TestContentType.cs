// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.Test;

internal class TestContentType : IContentType
{
    public TestContentType(string typeName)
    {
        TypeName = typeName;
    }

    public string TypeName { get; }

    public string DisplayName => TypeName;

    public IEnumerable<IContentType> BaseTypes => Enumerable.Empty<IContentType>();

    public bool IsOfType(string type) => string.Equals(type, TypeName, StringComparison.OrdinalIgnoreCase);
}
