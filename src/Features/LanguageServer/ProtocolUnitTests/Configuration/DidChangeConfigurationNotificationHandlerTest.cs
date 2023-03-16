// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        // A regex help to check the message we send to client.
        // It should look like "feature_group.feature_name"
        private static readonly string s_clientSideSectionPattern = @"^((csharp|visual_basic)\.)?([\w_]+)\.([\w_]+)$";

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
            clientCallbackTarget.SetClientSideOptionValues(setToDefaultValue: false);

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
            clientCallbackTarget.SetClientSideOptionValues(setToDefaultValue: true);

            await server.ExecuteRequestAsync<DidChangeConfigurationParams, object>(Methods.WorkspaceDidChangeConfigurationName, new DidChangeConfigurationParams(), CancellationToken.None).ConfigureAwait(false);
            await WaitAsync(server.TestWorkspace, Methods.WorkspaceDidChangeConfigurationName).ConfigureAwait(false);
            VerifyValuesInServer(server.TestWorkspace, clientCallbackTarget.MockClientSideValues);
        }

        private static void VerifyValuesInServer(TestWorkspace workspace, List<string> expectedValues)
        {
            var globalOptionService = workspace.GetService<IGlobalOptionService>();
            var supportedOptions = DidChangeConfigurationNotificationHandler.SupportedOptions;
            Assert.Equal(supportedOptions.Sum(option => option is IPerLanguageValuedOption ? 2 : 1), expectedValues.Count);
            var optionsAndLanguageToVerify = supportedOptions.SelectManyAsArray(option => option is IPerLanguageValuedOption
                ? DidChangeConfigurationNotificationHandler.SupportedLanguages.SelectAsArray(lang => (option, lang))
                : SpecializedCollections.SingletonEnumerable((option, string.Empty)));

            for (var i = 0; i < expectedValues.Count; i++)
            {
                var (option, languageName) = optionsAndLanguageToVerify[i];
                var valueFromClient = expectedValues[i];

                Assert.True(option.Definition.Serializer.TryParse(valueFromClient, out var result));
                if (option is IPerLanguageValuedOption)
                {
                    var valueInServer = globalOptionService.GetOption<object>(new OptionKey2(option, languageName));
                    Assert.Equal(result, valueInServer);
                }
                else
                {
                    var valueInServer = globalOptionService.GetOption<object>(new OptionKey2(option, null));
                    Assert.Equal(result, valueInServer);
                }
            }
        }

        private static async Task WaitAsync(TestWorkspace testWorkspace, string name)
        {
            var listenerProvider = testWorkspace.GetService<AsynchronousOperationListenerProvider>();
            await listenerProvider.GetWaiter(name).ExpeditedWaitAsync();
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

                var expectConfigurationItemsNumber = DidChangeConfigurationNotificationHandler.SupportedOptions.Sum(option => option is IPerLanguageValuedOption ? 2 : 1);
                Assert.Equal(expectConfigurationItemsNumber, configurationParams!.Items.Length);
                Assert.Equal(expectConfigurationItemsNumber, MockClientSideValues.Count);

                foreach (var item in configurationParams.Items)
                {
                    AssertSectionPattern(item.Section);
                }

                return JArray.FromObject(MockClientSideValues);
            }

            public void SetClientSideOptionValues(bool setToDefaultValue)
            {
                MockClientSideValues.Clear();
                foreach (var option in DidChangeConfigurationNotificationHandler.SupportedOptions)
                {
                    var valueToSet = setToDefaultValue ? GenerateDefaultValue(option) : GenerateNonDefaultValue(option);
                    if (option is IPerLanguageValuedOption)
                    {
                        foreach (var _ in DidChangeConfigurationNotificationHandler.SupportedLanguages)
                        {
                            MockClientSideValues.Add(valueToSet);
                        }
                    }
                    else
                    {
                        MockClientSideValues.Add(valueToSet);
                    }
                }
            }

            private static string ConvertToString(object? value)
                => value switch
                {
                    null => "null",
                    _ => value.ToString()
                };

            private static string GenerateNonDefaultValue(IOption2 option)
            {
                var nonDefaultValue = GetNonDefaultValue(option);
                return ConvertToString(nonDefaultValue);
            }

            private static string GenerateDefaultValue(IOption2 option)
                => ConvertToString(option.DefaultValue);

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

            private static void AssertSectionPattern(string? section)
            {
                Assert.NotNull(section);
                var regex = new Regex(s_clientSideSectionPattern);
                var match = regex.Match(section!);
                Assert.True(match.Success);
            }
        }
    }
}
