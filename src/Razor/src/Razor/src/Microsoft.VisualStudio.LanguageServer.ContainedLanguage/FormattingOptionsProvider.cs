// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

public abstract class FormattingOptionsProvider
{
    public abstract FormattingOptions? GetOptions(Uri uri);
}
