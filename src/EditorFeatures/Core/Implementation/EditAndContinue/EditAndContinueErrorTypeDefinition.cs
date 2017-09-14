// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    internal static class EditAndContinueErrorTypeDefinition
    {
        public const string Name = "Edit and Continue";

        [Export(typeof(ErrorTypeDefinition))]
        [Name(Name)]
        internal static ErrorTypeDefinition Definition;
    }
}
