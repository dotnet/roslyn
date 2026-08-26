// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.Razor.CohostingShared;

#pragma warning disable RS0030 // Do not use banned APIs
[AttributeUsage(AttributeTargets.Class), MetadataAttribute]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class ExportRazorStatelessLspServiceAttribute(Type handlerType) : ExportStatelessLspServiceAttribute(handlerType, ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.Any);
