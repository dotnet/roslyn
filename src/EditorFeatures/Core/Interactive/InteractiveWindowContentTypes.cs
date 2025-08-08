// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Interactive;

/// <summary>
/// Represents the content type for specialized interactive commands for C# and VB
/// interactive window which override the implementation of these commands in underlying
/// interactive window.
/// </summary>
internal static class InteractiveWindowContentTypes
{
    public const string CommandContentTypeName = "Specialized CSharp and VB Interactive Command";

    [Export, Name(CommandContentTypeName), BaseDefinition("code")]
    internal static readonly ContentTypeDefinition CommandContentTypeDefinition;
}

