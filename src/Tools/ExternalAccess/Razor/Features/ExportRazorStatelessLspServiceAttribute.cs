// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;

[AttributeUsage(AttributeTargets.Class), MetadataAttribute]
internal sealed class ExportRazorStatelessLspServiceAttribute(Type handlerType) : ExportStatelessLspServiceAttribute(handlerType, ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.Any);
