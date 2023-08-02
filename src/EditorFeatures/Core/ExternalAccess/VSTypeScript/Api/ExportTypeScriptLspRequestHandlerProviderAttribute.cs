﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

[AttributeUsage(AttributeTargets.Class), MetadataAttribute]
internal class ExportTypeScriptLspServiceFactoryAttribute(Type handlerType) : ExportLspServiceFactoryAttribute(handlerType, ProtocolConstants.TypeScriptLanguageContract)
{
}
