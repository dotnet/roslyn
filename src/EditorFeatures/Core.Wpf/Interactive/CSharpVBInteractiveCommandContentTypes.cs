// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Editor.Interactive
{
    /// <summary>
    /// Represents the content type for specialized interactive commands for C# and VB
    /// interactive window which override the implementation of these commands in underlying
    /// interactive window.
    /// </summary>
    internal static class CSharpVBInteractiveCommandsContentTypes
    {
        public const string CSharpVBInteractiveCommandContentTypeName = "Specialized CSharp and VB Interactive Command";

        [Export, Name(CSharpVBInteractiveCommandContentTypeName), BaseDefinition("code")]
        internal static readonly ContentTypeDefinition CSharpVBInteractiveCommandContentTypeDefinition;
    }
}

