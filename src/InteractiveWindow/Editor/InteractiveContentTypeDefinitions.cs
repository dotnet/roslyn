// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

#pragma warning disable CS0649 // field is not assigned to

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal static class InteractiveContentTypeDefinitions
    {
        [Export, Name(PredefinedInteractiveContentTypes.InteractiveContentTypeName), BaseDefinition("text"), BaseDefinition("projection")]
        internal static readonly ContentTypeDefinition InteractiveContentTypeDefinition;

        [Export, Name(PredefinedInteractiveContentTypes.InteractiveOutputContentTypeName), BaseDefinition("text")]
        internal static readonly ContentTypeDefinition InteractiveOutputContentTypeDefinition;
    }
}