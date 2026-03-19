// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Completion;

public enum SnippetsRule
{
    /// <summary>
    /// Snippet triggering follows the default rules of the language.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Snippets are never included in the completion list
    /// </summary>
    NeverInclude = 1,

    /// <summary>
    /// Snippets are always included in the completion list.
    /// </summary>
    AlwaysInclude = 2,

    /// <summary>
    /// Snippets are included if the user types: id?&lt;tab&gt;
    /// </summary>
    IncludeAfterTypingIdentifierQuestionTab = 3,
}
