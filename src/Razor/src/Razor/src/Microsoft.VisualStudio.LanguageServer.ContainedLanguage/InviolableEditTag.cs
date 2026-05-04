// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

// Used to indicate that no other entity should respond to the edit event associated with this tag.
internal class InviolableEditTag : IInviolableEditTag
{
    private InviolableEditTag() { }

    public static readonly IInviolableEditTag Instance = new InviolableEditTag();
}
