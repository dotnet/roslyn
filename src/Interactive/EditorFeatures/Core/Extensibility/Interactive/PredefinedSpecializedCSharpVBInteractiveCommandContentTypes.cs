// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Editor.Interactive
{
    public static class PredefinedSpecializedCSharpVBInteractiveCommandsContentTypes
    {
        public const string SpecializedCSharpVBInteractiveCommandContentTypeName = "Specialized CSharp and VB Interactive Command";

        [Export, Name(SpecializedCSharpVBInteractiveCommandContentTypeName), BaseDefinition("code")]
        internal static readonly ContentTypeDefinition SpecializedCSharpVBInteractiveCommandContentTypeDefinition;
    }
}

