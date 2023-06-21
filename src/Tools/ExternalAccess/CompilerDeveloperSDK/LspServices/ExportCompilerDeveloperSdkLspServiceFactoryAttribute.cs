// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.CompilerDeveloperSdk;

internal class ExportCompilerDeveloperSdkLspServiceFactoryAttribute(Type type, string contractName) :
    ExportLspServiceFactoryAttribute(type, contractName, WellKnownLspServerKinds.Any);
