// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;

/// <summary>
/// TODO - This custom liveshare model should live elsewhere.
/// </summary>
internal sealed class Project
{
    /// <summary>
    /// Name of the project.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The project language.
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// Paths of the files in the project.
    /// </summary>
    public Uri[] SourceFiles { get; set; }
}
