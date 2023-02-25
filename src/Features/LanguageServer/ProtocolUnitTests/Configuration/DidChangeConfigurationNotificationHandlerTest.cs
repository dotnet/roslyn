// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Configuration;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Configuration
{
    public class DidChangeConfigurationNotificationHandlerTest : AbstractLanguageServerProtocolTests
    {
        public DidChangeConfigurationNotificationHandlerTest(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task VerifyWorkflow()
        {
            var markup = @"
public class A { }";

            var clientCapabilities = new ClientCapabilities()
            {
                Workspace = new WorkspaceClientCapabilities()
                {
                    DidChangeConfiguration = new DynamicRegistrationSetting() { DynamicRegistration = true }
                }
            };

            var initializationOptions = new InitializationOptions()
            {
                CallInitialized = true,
                ClientCapabilities = clientCapabilities,
                ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer
            };

            var clientCallbackTarget = new ClientCallbackTarget();
            // Let client has non-default values for all options.
            foreach (var option in DidChangeConfigurationNotificationHandler.SupportedOptions)
            {
                clientCallbackTarget.MockClientSideValues.Add(GenerateNonDefaultValueString(option));
            }

            // 1. When initialized, server should register workspace/didChangeConfiguration if client support DynamicRegistration
            var server = await CreateTestLspServerAsync(
                markup,
                initializationOptions: initializationOptions,
                clientTarget: clientCallbackTarget);

            await WaitAsync(server.TestWorkspace, Methods.InitializedName).ConfigureAwait(false);
            Assert.True(clientCallbackTarget.WorkspaceDidChangeConfigurationRegistered);

            // 2. Server should fetched all the values from client, options should have non-default values now.
            VerifyValuesInServer(server.TestWorkspace, clientCallbackTarget.MockClientSideValues);

            // 3. When client side value changes, client should send server didChangeConfiguration. Then server would re-fetch all the options.
            // Let client has a default values for all options.
            clientCallbackTarget.MockClientSideValues.Clear();
            foreach (var option in DidChangeConfigurationNotificationHandler.SupportedOptions)
            {
                clientCallbackTarget.MockClientSideValues.Add(ConvertToString(option.DefaultValue));
            }

            await server.ExecuteRequestAsync<DidChangeConfigurationParams, object>(Methods.WorkspaceDidChangeConfigurationName, new DidChangeConfigurationParams(), CancellationToken.None).ConfigureAwait(false);
            await WaitAsync(server.TestWorkspace, Methods.WorkspaceDidChangeConfigurationName).ConfigureAwait(false);
            VerifyValuesInServer(server.TestWorkspace, clientCallbackTarget.MockClientSideValues);
        }

        private static void VerifyValuesInServer(TestWorkspace workspace, List<string> expectedValues)
        {
            var globalOptionService = workspace.GetService<IGlobalOptionService>();
            var supportedOptions = DidChangeConfigurationNotificationHandler.SupportedOptions;
            Assert.Equal(supportedOptions.Length, expectedValues.Count);
            var optionKeys = supportedOptions.SelectAsArray(
                option =>
                {
                    if (option is ISingleValuedOption singleValuedOption)
                    {
                        return new OptionKey2(singleValuedOption);
                    }
                    else if (option is IPerLanguageValuedOption perLanguageValuedOption)
                    {
                        return new OptionKey2(perLanguageValuedOption, LanguageNames.CSharp);
                    }

                    throw ExceptionUtilities.UnexpectedValue(nameof(option.Type));
                });

            var optionValuesInServer = globalOptionService.GetOptions(optionKeys);
            for (var i = 0; i < expectedValues.Count; i++)
            {
                var option = supportedOptions[i];
                var optionValue = optionValuesInServer[i];
                var stringValue = expectedValues[i];
                Assert.True(option.Definition.Serializer.TryParse(stringValue, out var result));
                Assert.Equal(result, optionValue);
            }
        }

        private async Task WaitAsync(TestWorkspace testWorkspace, string name)
        {
            var listenerProvider = testWorkspace.GetService<AsynchronousOperationListenerProvider>();
            await listenerProvider.GetWaiter(name).ExpeditedWaitAsync();
        }

        private static string ConvertToString(object? value)
            => value switch
            {
                null => "null",
                _ => value.ToString()
            };

        private static string GenerateNonDefaultValueString(IOption2 option)
        {
            var nonDefaultValue = GetNonDefaultValue(option);
            return ConvertToString(nonDefaultValue);
        }

        private static object? GetNonDefaultValue(IOption2 option)
        {
            var type = option.Type;
            if (type == typeof(bool))
            {
                var defaultValue = (bool)option.DefaultValue!;
                return !defaultValue;
            }
            else if (type == typeof(int))
            {
                var defaultValue = (int)option.DefaultValue!;
                return defaultValue + 1;
            }
            else if (type.IsEnum)
            {
                return GetDifferentEnumValue(type, option.DefaultValue!);
            }
            else if (type == typeof(bool?))
            {
                var defaultValue = (bool?)option.DefaultValue;
                return defaultValue switch
                {
                    null => true,
                    _ => !defaultValue.Value
                };
            }
            else if (Nullable.GetUnderlyingType(type)?.IsEnum == true)
            {
                // nullable enum
                var enumType = Nullable.GetUnderlyingType(type)!;
                return option.DefaultValue switch
                {
                    null => Enum.GetValues(type).GetValue(0),
                    _ => GetDifferentEnumValue(enumType, option.DefaultValue)
                };
            }
            else
            {
                throw new Exception("Please return a non-default value based on the config name.");
            }

            object GetDifferentEnumValue(Type enumType, object enumValue)
            {
                foreach (var value in Enum.GetValues(type))
                {
                    if (value != enumValue)
                    {
                        return value;
                    }
                }

                throw new ArgumentException($"{enumType.Name} has only one value.");
            }
        }

        private class ClientCallbackTarget
        {
            public bool WorkspaceDidChangeConfigurationRegistered { get; private set; } = false;
            public List<ConfigurationItem> ReceivedConfigurationItems { get; } = new();
            public List<string> MockClientSideValues { get; } = new();

            [JsonRpcMethod(Methods.ClientRegisterCapabilityName)]
            public void ClientRegisterCapability(JToken @params, CancellationToken _)
            {
                var registrationParams = JsonConvert.DeserializeObject<RegistrationParams>(@params.ToString());
                if (registrationParams == null)
                {
                    AssertEx.Fail($"Can't parse {@params} to {nameof(RegistrationParams)}");
                    return;
                }

                if (registrationParams.Registrations.Any(item => item.Method == Methods.WorkspaceDidChangeConfigurationName))
                {
                    if (WorkspaceDidChangeConfigurationRegistered)
                    {
                        AssertEx.Fail($"{Methods.WorkspaceDidChangeConfigurationName} is registered twice.");
                    }

                    WorkspaceDidChangeConfigurationRegistered = true;
                }

                return;
            }

            [JsonRpcMethod(Methods.WorkspaceConfigurationName)]
            public JArray WorkspaceConfigurationName(JToken configurationParamsToken, CancellationToken _)
            {
                var configurationParams = JsonConvert.DeserializeObject<ConfigurationParams>(configurationParamsToken.ToString());
                if (configurationParams == null)
                {
                    AssertEx.Fail($"Can't parse {configurationParams} to {nameof(ConfigurationParams)}.");
                }

                Assert.Equal(DidChangeConfigurationNotificationHandler.SupportedOptions.Length, configurationParams!.Items.Length);
                Assert.Equal(DidChangeConfigurationNotificationHandler.SupportedOptions.Length, MockClientSideValues.Count);
                return JArray.FromObject(MockClientSideValues);
            }
        }
    }
}
