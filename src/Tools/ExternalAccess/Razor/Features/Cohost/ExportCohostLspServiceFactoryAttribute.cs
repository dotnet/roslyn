// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

[AttributeUsage(AttributeTargets.Class), MetadataAttribute]
internal class ExportCohostLspServiceFactoryAttribute(Type handlerType) : ExportLspServiceFactoryAttribute(handlerType, ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.AlwaysActiveVSLspServer);
