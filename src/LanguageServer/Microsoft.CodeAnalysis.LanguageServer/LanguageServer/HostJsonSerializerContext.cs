// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.Completion;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Copilot;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer.Handler.Logging;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.LanguageServer;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(OpenProjectHandler.NotificationParams), TypeInfoPropertyName = "OpenProjectParams")]
[JsonSerializable(typeof(OpenSolutionHandler.NotificationParams), TypeInfoPropertyName = "OpenSolutionParams")]
[JsonSerializable(typeof(ServiceBrokerConnectHandler.NotificationParams), TypeInfoPropertyName = "ServiceBrokerConnectParams")]
[JsonSerializable(typeof(ProjectLoadTelemetryEvent))]
[JsonSerializable(typeof(ShowToastNotification.ShowToastNotificationParams))]
[JsonSerializable(typeof(ContextResolveParam))]
[JsonSerializable(typeof(IContextItem[]))]
[JsonSerializable(typeof(WorkspaceDebugConfigurationParams))]
[JsonSerializable(typeof(ProjectDebugConfiguration[]))]
[JsonSerializable(typeof(UpdateLogLevelParams))]
[JsonSerializable(typeof(RestoreParams))]
[JsonSerializable(typeof(RestoreResult))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(RunTestsPartialResult[]))]
[JsonSerializable(typeof(SumType<string, int, bool, string[]>))]
internal sealed partial class HostJsonSerializerContext : JsonSerializerContext;
