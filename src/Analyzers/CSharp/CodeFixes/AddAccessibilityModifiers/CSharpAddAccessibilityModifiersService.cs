// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers;

[ExportLanguageService(typeof(IAddAccessibilityModifiersService), LanguageNames.CSharp), Shared]
internal class CSharpAddAccessibilityModifiersService : CSharpAddAccessibilityModifiers, IAddAccessibilityModifiersService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpAddAccessibilityModifiersService()
    {
    }
}
