// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public sealed class TestContentTypeDefinition
    {
        public const string ContentTypeName = "InteractiveWindowTest";

        [Export]
        [Name(ContentTypeName)]
        [BaseDefinition("code")]
        public static readonly ContentTypeDefinition Definition;
    }
}
