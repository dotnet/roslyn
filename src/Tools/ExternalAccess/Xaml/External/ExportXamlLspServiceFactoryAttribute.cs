// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

/// <summary>
/// Defines an easy to use subclass for <see cref="ExportLspServiceFactoryAttribute"/> with the Roslyn languages contract name.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false), MetadataAttribute]
internal sealed class ExportXamlLspServiceFactoryAttribute : ExportLspServiceFactoryAttribute
{
    [Obsolete("Use the constructor that takes only a type")]
    public ExportXamlLspServiceFactoryAttribute(Type type, WellKnownLspServerKinds serverKind)
        : base(type, ProtocolConstants.RoslynLspLanguagesContract, serverKind)
    {
    }

    public ExportXamlLspServiceFactoryAttribute(Type type)
        : base(type, ProtocolConstants.RoslynLspLanguagesContract)
    {
    }
}
