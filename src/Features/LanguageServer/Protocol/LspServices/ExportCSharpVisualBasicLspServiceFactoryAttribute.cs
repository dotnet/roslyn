﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Defines an easy to use subclass for <see cref="ExportLspServiceFactoryAttribute"/> with the roslyn languages contract name.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false), MetadataAttribute]
internal class ExportCSharpVisualBasicLspServiceFactoryAttribute : ExportLspServiceFactoryAttribute
{
    public ExportCSharpVisualBasicLspServiceFactoryAttribute(Type type, WellKnownLspServerKinds serverKind = WellKnownLspServerKinds.Any) : base(type, ProtocolConstants.RoslynLspLanguagesContract, serverKind)
    {
    }
}
