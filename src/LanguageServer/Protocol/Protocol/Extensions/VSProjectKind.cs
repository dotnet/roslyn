// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// <see cref="VSProjectKind" /> represents the various kinds of contexts.
/// </summary>
internal enum VSProjectKind
{
    /// <summary>
    /// C++ project.
    /// </summary>
    CPlusPlus = 1,

    /// <summary>
    /// C# project.
    /// </summary>
    CSharp = 2,

    /// <summary>
    /// Visual Basic project.
    /// </summary>
    VisualBasic = 3,
}
