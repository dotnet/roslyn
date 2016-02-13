// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

