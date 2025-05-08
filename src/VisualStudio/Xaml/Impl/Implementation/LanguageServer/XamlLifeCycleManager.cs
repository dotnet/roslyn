// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.ServerLifetime;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer;

[ExportLspServiceFactory(typeof(LspServiceLifeCycleManager), StringConstants.XamlLspLanguagesContract), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class XamlLifeCycleManager(LspWorkspaceRegistrationService lspWorkspaceRegistrationService) : LspServiceLifeCycleManager.LspLifeCycleManagerFactory(lspWorkspaceRegistrationService);
