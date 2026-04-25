// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.AspNetCore.Razor.Test.Common.Editor;

public class TestInertContentType : IContentType
{
    public static readonly IContentType Instance = new TestInertContentType();

    public string TypeName => "inert";

    public string DisplayName => TypeName;

    public IEnumerable<IContentType> BaseTypes => Array.Empty<IContentType>();

    public bool IsOfType(string type) => string.Equals(type, TypeName, StringComparison.OrdinalIgnoreCase);
}
