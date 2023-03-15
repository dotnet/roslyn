// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal static class EditAndContinueErrorTypeDefinition
{
    public const string Name = "Edit and Continue";

    [Export(typeof(ErrorTypeDefinition))]
    [Name(Name)]
    internal static ErrorTypeDefinition? Definition;
}
