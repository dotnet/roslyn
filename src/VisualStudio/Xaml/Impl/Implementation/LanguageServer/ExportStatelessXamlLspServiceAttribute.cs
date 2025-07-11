// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer;

[AttributeUsage(AttributeTargets.Class), MetadataAttribute]
internal sealed class ExportStatelessXamlLspServiceAttribute : ExportStatelessLspServiceAttribute
{
    public ExportStatelessXamlLspServiceAttribute(Type handlerType) : base(handlerType, StringConstants.XamlLspLanguagesContract, WellKnownLspServerKinds.XamlLspServer)
    {
    }
}
