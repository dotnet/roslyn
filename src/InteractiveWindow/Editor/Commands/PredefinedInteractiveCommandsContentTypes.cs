// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

#pragma warning disable CS0649 // field is not assigned to

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    public static class PredefinedInteractiveCommandsContentTypes
    {
        public const string InteractiveCommandContentTypeName = "Interactive Command";

        [Export, Name(InteractiveCommandContentTypeName), BaseDefinition("code")]
        internal static readonly ContentTypeDefinition InteractiveCommandContentTypeDefinition;
    }
}
