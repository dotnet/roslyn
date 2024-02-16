// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

/// <summary>
/// Defines an easy to use subclass for <see cref="ExportStatelessLspServiceAttribute"/> with the Roslyn languages contract name.
/// </summary>
[AttributeUsage(AttributeTargets.Class), MetadataAttribute]
internal sealed class ExportXamlStatelessLspServiceAttribute : ExportStatelessLspServiceAttribute
{
    public ExportXamlStatelessLspServiceAttribute(Type handlerType) : base(handlerType, ProtocolConstants.RoslynLspLanguagesContract)
    {
    }
}
