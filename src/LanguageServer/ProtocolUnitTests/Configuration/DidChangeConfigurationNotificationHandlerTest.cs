// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Configuration;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.LanguageServer.Protocol;
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
        private static readonly string s_clientSideSectionPattern = @"^((csharp|visual_basic)\|)?([\w_|\.]+)([\w_]+)$";

        public DidChangeConfigurationNotificationHandlerTest(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, CombinatorialData]
        public async Task VerifyNoRequestToClientWithoutCapability(bool mutatingLspWorkspace)
        {
            var markup = @"
public class B { }";

            var clientCapabilities = new ClientCapabilities()
            {
                Workspace = new WorkspaceClientCapabilities()
                {
                    DidChangeConfiguration = new DynamicRegistrationSetting() { DynamicRegistration = true },
                    Configuration = false
                }
            };

            var clientCallbackTarget = new ClientCallbackTarget();
            var initializationOptions = new InitializationOptions()
            {
                CallInitialized = true,
                ClientCapabilities = clientCapabilities,
                ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
                ClientTarget = clientCallbackTarget,
            };

            await CreateTestLspServerAsync(
                markup, mutatingLspWorkspace, initializationOptions);
            Assert.False(clientCallbackTarget.ReceivedWorkspaceConfigurationRequest);
        }

        [Theory, CombinatorialData]
        public async Task VerifyWorkflow(bool mutatingLspWorkspace)
        {
            var markup = @"
public class A { }";

            var clientCapabilities = new ClientCapabilities()
            {
                Workspace = new WorkspaceClientCapabilities()
                {
                    DidChangeConfiguration = new DynamicRegistrationSetting() { DynamicRegistration = true },
                    Configuration = true
                }
            };

            var clientCallbackTarget = new ClientCallbackTarget();
            var initializationOptions = new InitializationOptions()
            {
                CallInitialized = true,
                ClientCapabilities = clientCapabilities,
                ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
                ClientTarget = clientCallbackTarget,
            };

            // Let client has non-default values for all options.
            clientCallbackTarget.SetClientSideOptionValues(setToDefaultValue: false);

            // 1. When initialized, server should register workspace/didChangeConfiguration if client support DynamicRegistration
            var server = await CreateTestLspServerAsync(
                markup, mutatingLspWorkspace, initializationOptions);

            Assert.True(clientCallbackTarget.WorkspaceDidChangeConfigurationRegistered);

            // 2. Server should fetched all the values from client, options should have non-default values now.
            VerifyValuesInServer(server.TestWorkspace, clientCallbackTarget.MockClientSideValues);

            // 3. When client side value changes, client should send server didChangeConfiguration. Then server would re-fetch all the options.
            // Let client has a default values for all options.
            clientCallbackTarget.SetClientSideOptionValues(setToDefaultValue: true);

            await server.ExecuteRequestAsync<DidChangeConfigurationParams, object>(Methods.WorkspaceDidChangeConfigurationName, new DidChangeConfigurationParams(), CancellationToken.None).ConfigureAwait(false);
            VerifyValuesInServer(server.TestWorkspace, clientCallbackTarget.MockClientSideValues);
        }

        [Fact]
        public void VerifyLspClientOptionNames()
        {
            var actualNames = DidChangeConfigurationNotificationHandler.SupportedOptions.Select(
                DidChangeConfigurationNotificationHandler.GenerateFullNameForOption).OrderBy(name => name).ToArray();
            // These options are persist in the LSP client. Please make sure also modify the LSP client code if these strings are changed.
            var expectedNames = new[]
            {
                "symbol_search.dotnet_search_reference_assemblies",
                "implement_type.dotnet_member_insertion_location",
                "implement_type.dotnet_property_generation_behavior",
                "completion.dotnet_show_name_completion_suggestions",
                "completion.dotnet_provide_regex_completions",
                "completion.dotnet_show_completion_items_from_unimported_namespaces",
                "quick_info.dotnet_show_remarks_in_quick_info",
                "navigation.dotnet_navigate_to_decompiled_sources",
                "highlighting.dotnet_highlight_related_json_components",
                "highlighting.dotnet_highlight_related_regex_components",
                "inlay_hints.dotnet_enable_inlay_hints_for_parameters",
                "inlay_hints.dotnet_enable_inlay_hints_for_literal_parameters",
                "inlay_hints.dotnet_enable_inlay_hints_for_indexer_parameters",
                "inlay_hints.dotnet_enable_inlay_hints_for_object_creation_parameters",
                "inlay_hints.dotnet_enable_inlay_hints_for_other_parameters",
                "inlay_hints.dotnet_suppress_inlay_hints_for_parameters_that_differ_only_by_suffix",
                "inlay_hints.dotnet_suppress_inlay_hints_for_parameters_that_match_method_intent",
                "inlay_hints.dotnet_suppress_inlay_hints_for_parameters_that_match_argument_name",
                "inlay_hints.csharp_enable_inlay_hints_for_types",
                "inlay_hints.csharp_enable_inlay_hints_for_implicit_variable_types",
                "inlay_hints.csharp_enable_inlay_hints_for_lambda_parameter_types",
                "inlay_hints.csharp_enable_inlay_hints_for_implicit_object_creation",
                "inlay_hints.csharp_enable_inlay_hints_for_collection_expressions",
                "code_style.formatting.indentation_and_spacing.tab_width",
                "code_style.formatting.indentation_and_spacing.indent_size",
                "code_style.formatting.indentation_and_spacing.indent_style",
                "code_style.formatting.new_line.end_of_line",
                "code_style.formatting.new_line.insert_final_newline",
                "background_analysis.dotnet_analyzer_diagnostics_scope",
                "background_analysis.dotnet_compiler_diagnostics_scope",
                "code_lens.dotnet_enable_references_code_lens",
                "code_lens.dotnet_enable_tests_code_lens",
                "projects.dotnet_binary_log_path",
                "projects.dotnet_enable_automatic_restore"
            }.OrderBy(name => name);

            Assert.Equal(expectedNames, actualNames);
        }

        private static void VerifyValuesInServer(EditorTestWorkspace workspace, List<string> expectedValues)
        {
            var globalOptionService = workspace.GetService<IGlobalOptionService>();
            var supportedOptions = DidChangeConfigurationNotificationHandler.SupportedOptions;
            Assert.Equal(supportedOptions.Sum(option => option is IPerLanguageValuedOption ? 2 : 1), expectedValues.Count);
            var optionsAndLanguageToVerify = supportedOptions.SelectManyAsArray(option => option is IPerLanguageValuedOption
                ? DidChangeConfigurationNotificationHandler.SupportedLanguages.SelectAsArray(lang => (option, lang))
                : [(option, string.Empty)]);

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

        private class ClientCallbackTarget
        {
            public bool WorkspaceDidChangeConfigurationRegistered { get; private set; } = false;
            public List<ConfigurationItem> ReceivedConfigurationItems { get; } = [];
            public List<string> MockClientSideValues { get; } = [];
            public bool ReceivedWorkspaceConfigurationRequest { get; private set; } = false;

            [JsonRpcMethod(Methods.ClientRegisterCapabilityName, UseSingleObjectParameterDeserialization = true)]
            public void ClientRegisterCapability(RegistrationParams @registrationParams, CancellationToken _)
            {
                if (WorkspaceDidChangeConfigurationRegistered)
                {
                    AssertEx.Fail($"{Methods.WorkspaceDidChangeConfigurationName} is registered twice.");
                    return;
                }

                WorkspaceDidChangeConfigurationRegistered = registrationParams.Registrations.Any(item => item.Method == Methods.WorkspaceDidChangeConfigurationName);
                return;
            }

            [JsonRpcMethod(Methods.WorkspaceConfigurationName, UseSingleObjectParameterDeserialization = true)]
            public List<string> WorkspaceConfigurationName(ConfigurationParams configurationParams, CancellationToken _)
            {
                ReceivedWorkspaceConfigurationRequest = true;
                var expectConfigurationItemsNumber = DidChangeConfigurationNotificationHandler.SupportedOptions.Sum(option => option is IPerLanguageValuedOption ? 2 : 1);
                Assert.Equal(expectConfigurationItemsNumber, configurationParams!.Items.Length);
                Assert.Equal(expectConfigurationItemsNumber, MockClientSideValues.Count);

                foreach (var item in configurationParams.Items)
                {
                    AssertSectionPattern(item.Section);
                }

                return MockClientSideValues;
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
                if (TryGetValueNonDefaultValueBasedOnName(option, out var nonDefaultValue))
                {
                    return nonDefaultValue;
                }

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
                else if (type == typeof(string))
                {
                    return Guid.NewGuid().ToString();
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
                    throw new Exception($"Please return a non-default value based on the config name, config name: {option.Name}.");
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

                static bool TryGetValueNonDefaultValueBasedOnName(IOption option, out object? nonDefaultValue)
                {
                    if (option.Name is "end_of_line")
                    {
                        nonDefaultValue = "\n";
                        return true;
                    }

                    nonDefaultValue = null;
                    return false;
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
