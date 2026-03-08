// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer;

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Razor.Features;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
#endif

[AttributeUsage(AttributeTargets.Class), MetadataAttribute]
internal class ExportRazorLspServiceFactoryAttribute(Type handlerType) : ExportLspServiceFactoryAttribute(handlerType, ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.Any);
