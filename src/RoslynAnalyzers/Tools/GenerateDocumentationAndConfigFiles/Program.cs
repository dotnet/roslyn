// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Analyzer.Utilities.PooledObjects.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ReleaseTracking;
using Microsoft.CodeAnalysis.Text;
using static GenerateDocumentationAndConfigFiles.CommonPropertyNames;

namespace GenerateDocumentationAndConfigFiles
{
    public static class Program
    {
        private static readonly HttpClient httpClient = new();

        private record CommandLineArgs(
            bool ValidateOnly,
            string AnalyzerRulesetsDir,
            string AnalyzerEditorconfigsDir,
            string AnalyzerGlobalconfigsDir,
            string BinDirectory,
            string Configuration,
            string Tfm,
            List<string> AssemblyList,
            string PropsFileDir,
            string PropsFileName,
            string TargetsFileDir,
            string TargetsFileName,
            string PropsFileToDisableNetAnalyzersInNuGetPackageName,
            string AnalyzerDocumentationFileDir,
            string AnalyzerDocumentationFileName,
            string AnalyzerSarifFileDir,
            string AnalyzerSarifFileName,
            string AnalyzerVersion,
            string AnalyzerPackageName,
            bool ContainsPortedFxCopRules,
            bool GenerateAnalyzerRulesMissingDocumentationFile,
            bool ReleaseTrackingOptOut,
            bool ValidateOffline);

        public static Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Generate documentation and configuration files for analyzers");

            var validateOnlyOption = new Option<bool>("--validateOnly")
            {
                Description = "Validate files instead of generating them",
                Required = true
            };
            var analyzerRulesetsDirOption = new Option<string>("--analyzerRulesetsDir")
            {
                Description = "Directory for analyzer rulesets",
                Required = true
            };
            var analyzerEditorConfigsDirOption = new Option<string>("--analyzerEditorconfigsDir")
            {
                Description = "Directory for analyzer editorconfigs",
                Required = true
            };
            var analyzerGlobalConfigsDirOption = new Option<string>("--analyzerGlobalconfigsDir")
            {
                Description = "Directory for analyzer global configs",
                Required = true
            };
            var binDirectoryOption = new Option<string>("--binDirectory")
            {
                Description = "Binary directory path",
                Required = true
            };
            var configurationOption = new Option<string>("--configuration")
            {
                Description = "Build configuration",
                Required = true
            };
            var tfmOption = new Option<string>("--tfm")
            {
                Description = "Target framework moniker",
                Required = true
            };
            var assembliesOption = new Option<string>("--assemblies")
            {
                Description = "Semicolon-separated list of assemblies",
                Required = true
            };
            var propsFileDirOption = new Option<string>("--propsFileDir")
            {
                Description = "Props file directory",
                Required = true
            };
            var propsFileNameOption = new Option<string>("--propsFileName")
            {
                Description = "Props file name",
                Required = true
            };
            var targetsFileDirOption = new Option<string>("--targetsFileDir")
            {
                Description = "Targets file directory",
                Required = true
            };
            var targetsFileNameOption = new Option<string>("--targetsFileName")
            {
                Description = "Targets file name",
                Required = true
            };
            var propsFileToDisableNetAnalyzersInNuGetPackageNameOption = new Option<string>("--propsFileToDisableNetAnalyzers")
            {
                Description = "Props file name to disable .NET analyzers",
                Required = true
            };
            var analyzerDocumentationFileDirOption = new Option<string>("--analyzerDocumentationFileDir")
            {
                Description = "Documentation file directory",
                Required = true
            };
            var analyzerDocumentationFileNameOption = new Option<string>("--analyzerDocumentationFileName")
            {
                Description = "Documentation file name",
                Required = true
            };
            var analyzerSarifFileDirOption = new Option<string>("--analyzerSarifFileDir")
            {
                Description = "SARIF file directory",
                Required = true
            };
            var analyzerSarifFileNameOption = new Option<string>("--analyzerSarifFileName")
            {
                Description = "SARIF file name",
                Required = true
            };
            var analyzerVersionOption = new Option<string>("--analyzerVersion")
            {
                Description = "Analyzer version",
                Required = true
            };
            var analyzerPackageNameOption = new Option<string>("--analyzerPackageName")
            {
                Description = "Analyzer package name",
                Required = true
            };
            var containsPortedFxCopRulesOption = new Option<bool>("--containsPortedFxcopRules")
            {
                Description = "Indicates if contains ported FxCop rules",
                Required = true
            };
            var generateAnalyzerRulesMissingDocumentationFileOption = new Option<bool>("--generateAnalyzerRulesMissingDocumentationFile")
            {
                Description = "Generate a file listing rules with missing documentation",
                Required = true
            };
            var releaseTrackingOptOutOption = new Option<bool>("--releaseTrackingOptOut")
            {
                Description = "Opt out of release tracking",
                Required = true
            };
            var validateOfflineOption = new Option<bool>("--validateOffline")
            {
                Description = "Validate files without checking external links",
                Required = true
            };

            // Add options to command
            rootCommand.Add(validateOnlyOption);
            rootCommand.Add(analyzerRulesetsDirOption);
            rootCommand.Add(analyzerEditorConfigsDirOption);
            rootCommand.Add(analyzerGlobalConfigsDirOption);
            rootCommand.Add(binDirectoryOption);
            rootCommand.Add(configurationOption);
            rootCommand.Add(tfmOption);
            rootCommand.Add(assembliesOption);
            rootCommand.Add(propsFileDirOption);
            rootCommand.Add(propsFileNameOption);
            rootCommand.Add(targetsFileDirOption);
            rootCommand.Add(targetsFileNameOption);
            rootCommand.Add(propsFileToDisableNetAnalyzersInNuGetPackageNameOption);
            rootCommand.Add(analyzerDocumentationFileDirOption);
            rootCommand.Add(analyzerDocumentationFileNameOption);
            rootCommand.Add(analyzerSarifFileDirOption);
            rootCommand.Add(analyzerSarifFileNameOption);
            rootCommand.Add(analyzerVersionOption);
            rootCommand.Add(analyzerPackageNameOption);
            rootCommand.Add(containsPortedFxCopRulesOption);
            rootCommand.Add(generateAnalyzerRulesMissingDocumentationFileOption);
            rootCommand.Add(releaseTrackingOptOutOption);
            rootCommand.Add(validateOfflineOption);

            rootCommand.SetAction((parseResult, cancellationToken) =>
            {
                var validateOnly = parseResult.GetValue(validateOnlyOption);
                var analyzerRulesetsDir = parseResult.GetValue(analyzerRulesetsDirOption) ?? string.Empty;
                var analyzerEditorconfigsDir = parseResult.GetValue(analyzerEditorConfigsDirOption) ?? string.Empty;
                var analyzerGlobalconfigsDir = parseResult.GetValue(analyzerGlobalConfigsDirOption) ?? string.Empty;
                var binDirectory = parseResult.GetValue(binDirectoryOption) ?? string.Empty;
                var configuration = parseResult.GetValue(configurationOption) ?? string.Empty;
                var tfm = parseResult.GetValue(tfmOption) ?? string.Empty;
                var assembliesString = parseResult.GetValue(assembliesOption) ?? string.Empty;
                var assemblyList = assembliesString.Split([';'], StringSplitOptions.RemoveEmptyEntries).ToList();
                var propsFileDir = parseResult.GetValue(propsFileDirOption) ?? string.Empty;
                var propsFileName = parseResult.GetValue(propsFileNameOption) ?? string.Empty;
                var targetsFileDir = parseResult.GetValue(targetsFileDirOption) ?? string.Empty;
                var targetsFileName = parseResult.GetValue(targetsFileNameOption) ?? string.Empty;
                var propsFileToDisableNetAnalyzersInNuGetPackageName = parseResult.GetValue(propsFileToDisableNetAnalyzersInNuGetPackageNameOption) ?? string.Empty;
                var analyzerDocumentationFileDir = parseResult.GetValue(analyzerDocumentationFileDirOption) ?? string.Empty;
                var analyzerDocumentationFileName = parseResult.GetValue(analyzerDocumentationFileNameOption) ?? string.Empty;
                var analyzerSarifFileDir = parseResult.GetValue(analyzerSarifFileDirOption) ?? string.Empty;
                var analyzerSarifFileName = parseResult.GetValue(analyzerSarifFileNameOption) ?? string.Empty;
                var analyzerVersion = parseResult.GetValue(analyzerVersionOption) ?? string.Empty;
                var analyzerPackageName = parseResult.GetValue(analyzerPackageNameOption) ?? string.Empty;
                var containsPortedFxCopRules = parseResult.GetValue(containsPortedFxCopRulesOption);
                var generateAnalyzerRulesMissingDocumentationFile = parseResult.GetValue(generateAnalyzerRulesMissingDocumentationFileOption);
                var releaseTrackingOptOut = parseResult.GetValue(releaseTrackingOptOutOption);
                var validateOffline = parseResult.GetValue(validateOfflineOption);

                var commandLineArgs = new CommandLineArgs(
                    validateOnly,
                    analyzerRulesetsDir,
                    analyzerEditorconfigsDir,
                    analyzerGlobalconfigsDir,
                    binDirectory,
                    configuration,
                    tfm,
                    assemblyList,
                    propsFileDir,
                    propsFileName,
                    targetsFileDir,
                    targetsFileName,
                    propsFileToDisableNetAnalyzersInNuGetPackageName,
                    analyzerDocumentationFileDir,
                    analyzerDocumentationFileName,
                    analyzerSarifFileDir,
                    analyzerSarifFileName,
                    analyzerVersion,
                    analyzerPackageName,
                    containsPortedFxCopRules,
                    generateAnalyzerRulesMissingDocumentationFile,
                    releaseTrackingOptOut,
                    validateOffline);

                return HandleAsync(commandLineArgs, cancellationToken);
            });

            return rootCommand.Parse(args).InvokeAsync(null, CancellationToken.None);
        }

        private static async Task<int> HandleAsync(CommandLineArgs args, CancellationToken cancellationToken)
        {
            var fileNamesWithValidationFailures = new List<string>();

            var allRulesById = new SortedList<string, DiagnosticDescriptor>();
            var fixableDiagnosticIds = new HashSet<string>();
            var categories = new HashSet<string>();
            var rulesMetadata = new SortedList<string, (string path, SortedList<string, (DiagnosticDescriptor rule, string typeName, string[]? languages)> rules)>();
            foreach (string assembly in args.AssemblyList)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(assembly);
                string path = Path.Combine(args.BinDirectory, assemblyName, args.Configuration, args.Tfm, assembly);
                if (!File.Exists(path))
                {
                    await Console.Error.WriteLineAsync($"'{path}' does not exist").ConfigureAwait(false);
                    return 1;
                }

                var analyzerFileReference = new AnalyzerFileReference(path, AnalyzerAssemblyLoader.Instance);
                analyzerFileReference.AnalyzerLoadFailed += AnalyzerFileReference_AnalyzerLoadFailed;
                var analyzers = analyzerFileReference.GetAnalyzersForAllLanguages();

                var assemblyRulesMetadata = (path, rules: new SortedList<string, (DiagnosticDescriptor rule, string typeName, string[]? languages)>());

                foreach (var analyzer in analyzers)
                {
                    var analyzerType = analyzer.GetType();

                    foreach (var rule in analyzer.SupportedDiagnostics)
                    {
                        allRulesById[rule.Id] = rule;
                        categories.Add(rule.Category);
                        assemblyRulesMetadata.rules[rule.Id] = (rule, analyzerType.Name, analyzerType.GetCustomAttribute<DiagnosticAnalyzerAttribute>(true)?.Languages);
                    }
                }

                rulesMetadata.Add(assemblyName, assemblyRulesMetadata);

                foreach (var id in analyzerFileReference.GetFixers().SelectMany(fixer => fixer.FixableDiagnosticIds))
                {
                    fixableDiagnosticIds.Add(id);
                }
            }

            createRulesetAndEditorconfig(
                "AllRulesDefault",
                "All Rules with default severity",
                @"All Rules with default severity. Rules with IsEnabledByDefault = false are disabled.",
                RulesetKind.AllDefault);

            createRulesetAndEditorconfig(
                "AllRulesEnabled",
                "All Rules Enabled as build warnings",
                "All Rules are enabled as build warnings. Rules with IsEnabledByDefault = false are force enabled as build warnings.",
                RulesetKind.AllEnabled);

            createRulesetAndEditorconfig(
                "AllRulesDisabled",
                "All Rules Disabled",
                @"All Rules are disabled.",
                RulesetKind.AllDisabled);

            foreach (var category in categories)
            {
                createRulesetAndEditorconfig(
                    $"{category}RulesDefault",
                    $"{category} Rules with default severity",
                    $@"All {category} Rules with default severity. Rules with IsEnabledByDefault = false or from a different category are disabled.",
                    RulesetKind.CategoryDefault,
                    category: category);

                createRulesetAndEditorconfig(
                    $"{category}RulesEnabled",
                    $"{category} Rules Enabled as build warnings",
                    $@"All {category} Rules are enabled as build warnings. {category} Rules with IsEnabledByDefault = false are force enabled as build warnings. Rules from a different category are disabled.",
                    RulesetKind.CategoryEnabled,
                    category: category);
            }

            // We generate custom tag based rulesets only for select custom tags.
            var customTagsToGenerateRulesets = ImmutableArray.Create(
                WellKnownDiagnosticTagsExtensions.Dataflow,
                FxCopWellKnownDiagnosticTags.PortedFromFxCop);

            foreach (var customTag in customTagsToGenerateRulesets)
            {
                createRulesetAndEditorconfig(
                    $"{customTag}RulesDefault",
                    $"{customTag} Rules with default severity",
                    $@"All {customTag} Rules with default severity. Rules with IsEnabledByDefault = false and non-{customTag} rules are disabled.",
                    RulesetKind.CustomTagDefault,
                    customTag: customTag);

                createRulesetAndEditorconfig(
                    $"{customTag}RulesEnabled",
                    $"{customTag} Rules Enabled as build warnings",
                    $@"All {customTag} Rules are enabled as build warnings. {customTag} Rules with IsEnabledByDefault = false are force enabled as build warning. Non-{customTag} Rules are disabled.",
                    RulesetKind.CustomTagEnabled,
                    customTag: customTag);
            }

            createPropsFiles();

            createAnalyzerDocumentationFile();

            createAnalyzerSarifFile();

            if (args.GenerateAnalyzerRulesMissingDocumentationFile)
            {
                try
                {
                    await createAnalyzerRulesMissingDocumentationFileAsync().ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    await Console.Out.WriteLineAsync($"Failed to create analyzer rules missing documentation file. Http response timed out").ConfigureAwait(false);
                }
            }

            if (fileNamesWithValidationFailures.Count > 0)
            {
                await Console.Error.WriteLineAsync("One or more auto-generated documentation files were either edited manually, or not updated. Please revert changes made to the following files (if manually edited) and run `dotnet msbuild /t:pack` at the root of the repo to automatically update them:").ConfigureAwait(false);
                fileNamesWithValidationFailures.ForEach(fileName => Console.Error.WriteLine($"    {fileName}"));
                return 1;
            }

            if (!await createGlobalConfigFilesAsync().ConfigureAwait(false))
            {
                return 2;
            }

            CreateTargetsFile(args.TargetsFileDir, args.TargetsFileName, args.AnalyzerPackageName, categories.OrderBy(c => c));

            return 0;

            // Local functions.
            static void AnalyzerFileReference_AnalyzerLoadFailed(object? sender, AnalyzerLoadFailureEventArgs e)
                => throw e.Exception ?? new NotSupportedException(e.Message);

            void createRulesetAndEditorconfig(
                string fileName,
                string title,
                string description,
                RulesetKind rulesetKind,
                string? category = null,
                string? customTag = null)
            {
                CreateRuleset(args.AnalyzerRulesetsDir, fileName + ".ruleset", title, description, rulesetKind, category, customTag, allRulesById, args.AnalyzerPackageName);
                CreateEditorconfig(args.AnalyzerEditorconfigsDir, fileName, title, description, rulesetKind, category, customTag, allRulesById);
                return;
            }

            void createPropsFiles()
            {
                if (string.IsNullOrEmpty(args.PropsFileDir) || string.IsNullOrEmpty(args.PropsFileName))
                {
                    Debug.Assert(!args.ContainsPortedFxCopRules);
                    Debug.Assert(string.IsNullOrEmpty(args.PropsFileToDisableNetAnalyzersInNuGetPackageName));
                    return;
                }

                var disableNetAnalyzersImport = getDisableNetAnalyzersImport();

                var fileContents =
                    $"""
                    <Project>
                      {disableNetAnalyzersImport}{getCodeAnalysisTreatWarningsAsErrors()}{getCompilerVisibleProperties()}
                    </Project>
                    """;
                var directory = Directory.CreateDirectory(args.PropsFileDir);
                var fileWithPath = Path.Combine(directory.FullName, args.PropsFileName);

                // This doesn't need validation as the generated file is part of artifacts.
                File.WriteAllText(fileWithPath, fileContents);

                if (!string.IsNullOrEmpty(disableNetAnalyzersImport))
                {
                    Debug.Assert(Version.TryParse(args.AnalyzerVersion, out _));

                    fileWithPath = Path.Combine(directory.FullName, args.PropsFileToDisableNetAnalyzersInNuGetPackageName);
                    fileContents =
                        $"""
                        <Project>
                          <!--
                            PropertyGroup to disable built-in analyzers from .NET SDK that have the identical CA rules to those implemented in this package.
                            This props file should only be present in the analyzer NuGet package, it should **not** be inserted into the .NET SDK.
                          -->
                          <PropertyGroup>
                            <EnableNETAnalyzers>false</EnableNETAnalyzers>
                            <{NetAnalyzersNugetAssemblyVersionPropertyName}>{args.AnalyzerVersion}</{NetAnalyzersNugetAssemblyVersionPropertyName}>
                          </PropertyGroup>
                        </Project>
                        """;
                    // This doesn't need validation as the generated file is part of artifacts.
                    File.WriteAllText(fileWithPath, fileContents);
                }

                return;

                string getDisableNetAnalyzersImport()
                {
                    if (!string.IsNullOrEmpty(args.PropsFileToDisableNetAnalyzersInNuGetPackageName))
                    {
                        Debug.Assert(args.AnalyzerPackageName is NetAnalyzersPackageName or TextAnalyzersPackageName);

                        return $"""

                              <!--
                                This import includes an additional props file that disables built-in analyzers from .NET SDK that have the identical CA rules to those implemented in this package.
                                This additional props file should only be present in the analyzer NuGet package, it should **not** be inserted into the .NET SDK.
                              -->
                              <Import Project="{args.PropsFileToDisableNetAnalyzersInNuGetPackageName}" Condition="Exists('{args.PropsFileToDisableNetAnalyzersInNuGetPackageName}')" />

                              <!--
                                PropertyGroup to set the NetAnalyzers version installed in the SDK.
                                We rely on the additional props file '{args.PropsFileToDisableNetAnalyzersInNuGetPackageName}' not being present in the SDK.
                              -->
                              <PropertyGroup Condition="!Exists('{args.PropsFileToDisableNetAnalyzersInNuGetPackageName}')">
                                <{NetAnalyzersSDKAssemblyVersionPropertyName}>{args.AnalyzerVersion}</{NetAnalyzersSDKAssemblyVersionPropertyName}>
                              </PropertyGroup>

                            """;
                    }

                    Debug.Assert(!args.ContainsPortedFxCopRules);
                    return string.Empty;
                }
            }

            string getCodeAnalysisTreatWarningsAsErrors()
            {
                var allRuleIds = string.Join(';', allRulesById.Keys);
                return $"""

                      <!--
                        This property group handles 'CodeAnalysisTreatWarningsAsErrors = false' for the CA rule ids implemented in this package.
                      -->
                      <PropertyGroup>
                        <CodeAnalysisRuleIds>{allRuleIds}</CodeAnalysisRuleIds>
                        <EffectiveCodeAnalysisTreatWarningsAsErrors Condition="'$(EffectiveCodeAnalysisTreatWarningsAsErrors)' == ''">$(CodeAnalysisTreatWarningsAsErrors)</EffectiveCodeAnalysisTreatWarningsAsErrors>
                        <WarningsNotAsErrors Condition="'$(EffectiveCodeAnalysisTreatWarningsAsErrors)' == 'false' and '$(TreatWarningsAsErrors)' == 'true'">$(WarningsNotAsErrors);$(CodeAnalysisRuleIds)</WarningsNotAsErrors>
                      </PropertyGroup>
                    """;
            }

            string getCompilerVisibleProperties()
            {
                return args.AnalyzerPackageName switch
                {
                    ResxSourceGeneratorPackageName => """

                      <ItemGroup>
                        <CompilerVisibleProperty Include="RootNamespace" />
                        <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="WithCulture" />
                        <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="GenerateSource" />
                        <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="RelativeDir" />
                        <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="ClassName" />
                        <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="OmitGetResourceString" />
                        <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="AsConstants" />
                        <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="IncludeDefaultValues" />
                        <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="EmitFormatMethods" />
                        <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="Public" />
                        <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="NoWarn" />
                      </ItemGroup>

                    """,
                    _ => "",
                };
            }

            void createAnalyzerDocumentationFile()
            {
                if (string.IsNullOrEmpty(args.AnalyzerDocumentationFileDir) || string.IsNullOrEmpty(args.AnalyzerDocumentationFileName) || allRulesById.Count == 0)
                {
                    Debug.Assert(!args.ContainsPortedFxCopRules);
                    return;
                }

                var directory = Directory.CreateDirectory(args.AnalyzerDocumentationFileDir);
                var fileWithPath = Path.Combine(directory.FullName, args.AnalyzerDocumentationFileName);

                var builder = new StringBuilder();

                var fileTitle = Path.GetFileNameWithoutExtension(args.AnalyzerDocumentationFileName);
                builder.AppendLine($"# {fileTitle}");
                builder.AppendLine();

                var isFirstEntry = true;
                foreach (var ruleById in allRulesById)
                {
                    string ruleId = ruleById.Key;
                    DiagnosticDescriptor descriptor = ruleById.Value;

                    var ruleIdWithHyperLink = descriptor.Id;
                    if (!string.IsNullOrWhiteSpace(descriptor.HelpLinkUri))
                    {
                        ruleIdWithHyperLink = $"[{ruleIdWithHyperLink}]({descriptor.HelpLinkUri})";
                    }

                    var title = descriptor.Title.ToString(CultureInfo.InvariantCulture).Trim();

                    title = escapeMarkdown(title);

                    if (!isFirstEntry)
                    {
                        // Add separation line only when reaching next entry to avoid useless empty line at the end
                        builder.AppendLine();
                    }

                    isFirstEntry = false;
                    builder.AppendLine($"## {ruleIdWithHyperLink}: {title}");
                    builder.AppendLine();

                    var description = descriptor.Description.ToString(CultureInfo.InvariantCulture);
                    if (string.IsNullOrWhiteSpace(description))
                    {
                        description = descriptor.MessageFormat.ToString(CultureInfo.InvariantCulture);
                    }

                    // Double the line breaks to ensure they are rendered properly in markdown
                    description = Regex.Replace(description, "(\r?\n)", "$1$1");
                    description = escapeMarkdown(description);
                    // Add angle brackets around links to prevent violating MD034:
                    // https://github.com/DavidAnson/markdownlint/blob/82cf68023f7dbd2948a65c53fc30482432195de4/doc/Rules.md#md034---bare-url-used
                    // Regex taken from: https://github.com/DavidAnson/markdownlint/blob/59eaa869fc749e381fe9d53d04812dfc759595c6/helpers/helpers.js#L24
                    description = Regex.Replace(description, @"(?:http|ftp)s?:\/\/[^\s\]""']*(?:\/|[^\s\]""'\W])", "<$0>");
                    description = description.Trim();

                    builder.AppendLine(description);
                    builder.AppendLine();

                    builder.AppendLine("|Item|Value|");
                    builder.AppendLine("|-|-|");
                    builder.AppendLine($"|Category|{descriptor.Category}|");
                    builder.AppendLine($"|Enabled|{descriptor.IsEnabledByDefault}|");
                    builder.AppendLine($"|Severity|{descriptor.DefaultSeverity}|");
                    var hasCodeFix = fixableDiagnosticIds.Contains(descriptor.Id);
                    builder.AppendLine($"|CodeFix|{hasCodeFix}|");
                    builder.AppendLine("---");
                }

                if (args.ValidateOnly)
                {
                    Validate(fileWithPath, builder.ToString(), fileNamesWithValidationFailures);
                }
                else
                {
                    File.WriteAllText(fileWithPath, builder.ToString());
                }
            }

            // Escape generic arguments to ensure they are not considered as HTML elements, and also escape asterisks.
            static string escapeMarkdown(string text)
                => Regex.Replace(text, "(<.+?>)", "\\$1").Replace("*", @"\*");

            // based on https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/CommandLine/ErrorLogger.cs
            void createAnalyzerSarifFile()
            {
                if (string.IsNullOrEmpty(args.AnalyzerSarifFileDir) || string.IsNullOrEmpty(args.AnalyzerSarifFileName) || allRulesById.Count == 0)
                {
                    Debug.Assert(!args.ContainsPortedFxCopRules);
                    return;
                }

                var culture = new CultureInfo("en-us");
                var directory = Directory.CreateDirectory(args.AnalyzerSarifFileDir);
                TextWriter textWriter;
                if (args.ValidateOnly)
                {
                    // In validate mode we just write to an in memory version and compare with the existing
                    // content in `analyzerSarifFileName`.
                    textWriter = new StringWriter();
                }
                else
                {
                    var fileWithPath = Path.Combine(directory.FullName, args.AnalyzerSarifFileName);
                    textWriter = new StreamWriter(fileWithPath, false, Encoding.UTF8);
                }
                try
                {
                    using var writer = new Roslyn.Utilities.JsonWriter(textWriter);
                    writer.WriteObjectStart(); // root
                    writer.Write("$schema", "http://json.schemastore.org/sarif-1.0.0");
                    writer.Write("version", "1.0.0");
                    writer.WriteArrayStart("runs");

                    foreach (var assemblymetadata in rulesMetadata)
                    {
                        writer.WriteObjectStart(); // run

                        writer.WriteObjectStart("tool");
                        writer.Write("name", assemblymetadata.Key);

                        if (!string.IsNullOrWhiteSpace(args.AnalyzerVersion))
                        {
                            writer.Write("version", args.AnalyzerVersion);
                        }

                        writer.Write("language", culture.Name);
                        writer.WriteObjectEnd(); // tool

                        writer.WriteObjectStart("rules"); // rules

                        foreach (var rule in assemblymetadata.Value.rules)
                        {
                            var ruleId = rule.Key;
                            var descriptor = rule.Value.rule;

                            writer.WriteObjectStart(descriptor.Id); // rule
                            writer.Write("id", descriptor.Id);

                            writer.Write("shortDescription", descriptor.Title.ToString(CultureInfo.InvariantCulture));

                            string fullDescription = descriptor.Description.ToString(CultureInfo.InvariantCulture);
                            writer.Write("fullDescription", !string.IsNullOrEmpty(fullDescription) ? fullDescription.Replace("\r\n", "\n") : descriptor.MessageFormat.ToString(CultureInfo.InvariantCulture));

                            writer.Write("defaultLevel", getLevel(descriptor.DefaultSeverity));

                            if (!string.IsNullOrEmpty(descriptor.HelpLinkUri))
                            {
                                writer.Write("helpUri", descriptor.HelpLinkUri);
                            }

                            writer.WriteObjectStart("properties");

                            writer.Write("category", descriptor.Category);

                            writer.Write("isEnabledByDefault", descriptor.IsEnabledByDefault);

                            writer.Write("typeName", rule.Value.typeName);

                            if (rule.Value.languages?.Length > 0)
                            {
                                writer.WriteArrayStart("languages");

                                foreach (var language in rule.Value.languages.OrderBy(l => l, StringComparer.InvariantCultureIgnoreCase))
                                {
                                    writer.Write(language);
                                }

                                writer.WriteArrayEnd(); // languages
                            }

                            if (descriptor.CustomTags.Any())
                            {
                                writer.WriteArrayStart("tags");

                                foreach (string tag in descriptor.CustomTags)
                                {
                                    writer.Write(tag);
                                }

                                writer.WriteArrayEnd(); // tags
                            }

                            writer.WriteObjectEnd(); // properties
                            writer.WriteObjectEnd(); // rule
                        }

                        writer.WriteObjectEnd(); // rules
                        writer.WriteObjectEnd(); // run
                    }

                    writer.WriteArrayEnd(); // runs
                    writer.WriteObjectEnd(); // root

                    if (args.ValidateOnly)
                    {
                        var stringWriter = (StringWriter)textWriter;
                        Validate(Path.Combine(directory.FullName, args.AnalyzerSarifFileName), stringWriter.ToString(), fileNamesWithValidationFailures);
                    }

                    return;
                }
                finally
                {
                    textWriter.Dispose();
                }

                static string getLevel(DiagnosticSeverity severity)
                {
                    switch (severity)
                    {
                        case DiagnosticSeverity.Info:
                            return "note";

                        case DiagnosticSeverity.Error:
                            return "error";

                        case DiagnosticSeverity.Warning:
                            return "warning";

                        case DiagnosticSeverity.Hidden:
                            return "hidden";

                        default:
                            Debug.Assert(false);
                            goto case DiagnosticSeverity.Warning;
                    }
                }
            }

            async ValueTask createAnalyzerRulesMissingDocumentationFileAsync()
            {
                if (string.IsNullOrEmpty(args.AnalyzerDocumentationFileDir) || allRulesById.Count == 0)
                {
                    Debug.Assert(!args.ContainsPortedFxCopRules);
                    return;
                }

                var directory = Directory.CreateDirectory(args.AnalyzerDocumentationFileDir);
                var fileWithPath = Path.Combine(directory.FullName, "RulesMissingDocumentation.md");

                var builder = new StringBuilder();
                builder.Append("""
                    # Rules without documentation

                    Rule ID | Missing Help Link | Title |
                    --------|-------------------|-------|

                    """);
                var actualContent = Array.Empty<string>();
                if (args.ValidateOnly)
                {
                    actualContent = File.ReadAllLines(fileWithPath);
                }

                foreach (var ruleById in allRulesById)
                {
                    string ruleId = ruleById.Key;
                    DiagnosticDescriptor descriptor = ruleById.Value;

                    var helpLinkUri = descriptor.HelpLinkUri;
                    if (!string.IsNullOrWhiteSpace(helpLinkUri) &&
                        await checkHelpLinkAsync(helpLinkUri).ConfigureAwait(false))
                    {
                        // Rule with valid documentation link
                        continue;
                    }

                    // The angle brackets around helpLinkUri are added to follow MD034 rule:
                    // https://github.com/DavidAnson/markdownlint/blob/82cf68023f7dbd2948a65c53fc30482432195de4/doc/Rules.md#md034---bare-url-used
                    if (!string.IsNullOrWhiteSpace(helpLinkUri))
                    {
                        helpLinkUri = $"<{helpLinkUri}>";
                    }

                    var escapedTitle = descriptor.Title.ToString(CultureInfo.InvariantCulture).Replace("<", "\\<");
                    var line = $"{ruleId} | {helpLinkUri} | {escapedTitle} |";
                    if (args.ValidateOnly)
                    {
                        // The validation for RulesMissingDocumentation.md is different than others.
                        // We consider having "extra" entries as valid. This is to prevent CI failures due to rules being documented.
                        // However, we consider "missing" entries as invalid. This is to force updating the file when new rules are added.
                        if (!actualContent.Contains(line))
                        {
                            await Console.Error.WriteLineAsync($"Missing entry in {fileWithPath}").ConfigureAwait(false);
                            await Console.Error.WriteLineAsync(line).ConfigureAwait(false);
                            // The file is missing an entry. Mark it as invalid and break the loop as there is no need to continue validating.
                            fileNamesWithValidationFailures.Add(fileWithPath);
                            break;
                        }
                    }
                    else
                    {
                        builder.AppendLine(line);
                    }
                }

                if (!args.ValidateOnly)
                {
                    File.WriteAllText(fileWithPath, builder.ToString());
                }

                return;

                async Task<bool> checkHelpLinkAsync(string helpLink)
                {
                    try
                    {
                        if (!Uri.TryCreate(helpLink, UriKind.Absolute, out var uri))
                        {
                            return false;
                        }

                        if (args.ValidateOffline)
                        {
                            return true;
                        }

                        var request = new HttpRequestMessage(HttpMethod.Head, uri);
                        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        return response?.StatusCode == HttpStatusCode.OK;
                    }
                    catch (WebException)
                    {
                        return false;
                    }
                }
            }

            async Task<bool> createGlobalConfigFilesAsync()
            {
                using var _1 = ArrayBuilder<ReleaseTrackingData>.GetInstance(out var releaseTrackingFilesDataBuilder);
                using var _2 = PooledHashSet<Version>.GetInstance(out var versionsBuilder);

                // Validate all assemblies exist on disk and can be loaded.
                foreach (string assembly in args.AssemblyList)
                {
                    var assemblyPath = GetAssemblyPath(assembly);
                    if (!File.Exists(assemblyPath))
                    {
                        await Console.Error.WriteLineAsync($"'{assemblyPath}' does not exist").ConfigureAwait(false);
                        return false;
                    }

                    try
                    {
                        _ = Assembly.LoadFrom(assemblyPath);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
                        return false;
                    }
                }

                // Compute descriptors by rule ID and shipped analyzer release versions and shipped data.
                var sawShippedFile = false;
                foreach (string assembly in args.AssemblyList)
                {
                    var assemblyPath = GetAssemblyPath(assembly);
                    var assemblyDir = Path.GetDirectoryName(assemblyPath);
                    if (assemblyDir is null)
                    {
                        continue;
                    }

                    var assemblyName = Path.GetFileNameWithoutExtension(assembly);
                    var shippedFile = Path.Combine(assemblyDir, "AnalyzerReleases", assemblyName, ReleaseTrackingHelper.ShippedFileName);
                    var unshippedFile = Path.Combine(assemblyDir, "AnalyzerReleases", assemblyName, ReleaseTrackingHelper.UnshippedFileName);
                    var shippedFileExists = File.Exists(shippedFile);
                    var unshippedFileExists = File.Exists(unshippedFile);

                    if (shippedFileExists ^ unshippedFileExists)
                    {
                        var existingFile = shippedFileExists ? shippedFile : unshippedFile;
                        var nonExistingFile = shippedFileExists ? unshippedFile : shippedFile;
                        await Console.Error.WriteLineAsync($"Expected both '{shippedFile}' and '{unshippedFile}' to exist or not exist, but '{existingFile}' exists and '{nonExistingFile}' does not exist.").ConfigureAwait(false);
                        return false;
                    }

                    if (shippedFileExists)
                    {
                        sawShippedFile = true;

                        if (args.ReleaseTrackingOptOut)
                        {
                            await Console.Error.WriteLineAsync($"'{shippedFile}' exists but was not expected").ConfigureAwait(false);
                            return false;
                        }

                        try
                        {
                            // Read shipped file
                            using var fileStream = File.OpenRead(shippedFile);
                            var sourceText = SourceText.From(fileStream);
                            var releaseTrackingData = ReleaseTrackingHelper.ReadReleaseTrackingData(shippedFile, sourceText,
                                onDuplicateEntryInRelease: (_1, _2, _3, _4, line) => throw new InvalidOperationException($"Duplicate entry in {shippedFile} at {line.LineNumber}: '{line}'"),
                                onInvalidEntry: (line, _2, _3, _4) => throw new InvalidOperationException($"Invalid entry in {shippedFile} at {line.LineNumber}: '{line}'"),
                                isShippedFile: true);
                            releaseTrackingFilesDataBuilder.Add(releaseTrackingData);
                            versionsBuilder.AddRange(releaseTrackingData.Versions);

                            // Read unshipped file
                            using var fileStreamUnshipped = File.OpenRead(unshippedFile);
                            var sourceTextUnshipped = SourceText.From(fileStreamUnshipped);
                            var releaseTrackingDataUnshipped = ReleaseTrackingHelper.ReadReleaseTrackingData(unshippedFile, sourceTextUnshipped,
                                onDuplicateEntryInRelease: (_1, _2, _3, _4, line) => throw new InvalidOperationException($"Duplicate entry in {unshippedFile} at {line.LineNumber}: '{line}'"),
                                onInvalidEntry: (line, _2, _3, _4) => throw new InvalidOperationException($"Invalid entry in {unshippedFile} at {line.LineNumber}: '{line}'"),
                                isShippedFile: false);
                            releaseTrackingFilesDataBuilder.Add(releaseTrackingDataUnshipped);
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                        {
                            await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
                            return false;
                        }
                    }
                }

                if (!args.ReleaseTrackingOptOut && !sawShippedFile)
                {
                    await Console.Error.WriteLineAsync($"Could not find any 'AnalyzerReleases.Shipped.md' file").ConfigureAwait(false);
                    return false;
                }

                if (versionsBuilder.Count > 0)
                {
                    var releaseTrackingData = releaseTrackingFilesDataBuilder.ToImmutableArray();

                    // Generate global analyzer config files for each shipped version.
                    foreach (var version in versionsBuilder)
                    {
                        CreateGlobalConfigsForVersion(version, isShippedVersion: true, releaseTrackingData);
                    }

                    // Generate global analyzer config files for unshipped version.
                    // See https://github.com/dotnet/roslyn-analyzers/issues/6247 for details.

                    // Use 'unshippedVersion = maxShippedVersion + 1' for unshipped data.
                    var maxShippedVersion = versionsBuilder.Max();
                    var unshippedVersion = new Version(maxShippedVersion!.Major + 1, maxShippedVersion.Minor);
                    CreateGlobalConfigsForVersion(unshippedVersion, isShippedVersion: false, releaseTrackingData);
                }

                return true;

                // Local functions.
                void CreateGlobalConfigsForVersion(
                    Version version,
                    bool isShippedVersion,
                    ImmutableArray<ReleaseTrackingData> releaseTrackingData)
                {
                    var analysisLevelVersionString = GetNormalizedVersionStringForEditorconfigFileNameSuffix(version);

                    foreach (var warnAsError in new[] { true, false })
                    {
                        foreach (var analysisMode in Enum.GetValues<AnalysisMode>())
                        {
                            CreateGlobalConfig(version, isShippedVersion, analysisLevelVersionString, analysisMode, warnAsError, releaseTrackingData, category: null);
                            foreach (var category in categories!)
                            {
                                CreateGlobalConfig(version, isShippedVersion, analysisLevelVersionString, analysisMode, warnAsError, releaseTrackingData, category);
                            }
                        }
                    }
                }

                void CreateGlobalConfig(
                    Version version,
                    bool isShippedVersion,
                    string analysisLevelVersionString,
                    AnalysisMode analysisMode,
                    bool warnAsError,
                    ImmutableArray<ReleaseTrackingData> releaseTrackingData,
                    string? category)
                {
                    var analysisLevelPropName = "AnalysisLevel";
                    var title = $"Rules from '{version}' release with '{analysisMode}' analysis mode";
                    var description = $"Rules with enabled-by-default state from '{version}' release with '{analysisMode}' analysis mode. Rules that are first released in a version later than '{version}' are disabled.";

                    if (category != null)
                    {
                        analysisLevelPropName += category;
                        title = $"'{category}' {title}";
                        description = $"'{category}' {description}";
                    }

#pragma warning disable CA1308 // Normalize strings to uppercase
                    var globalconfigFileName = $"{analysisLevelPropName}_{analysisLevelVersionString}_{analysisMode!.ToString()!.ToLowerInvariant()}";
#pragma warning restore CA1308 // Normalize strings to uppercase

                    if (warnAsError)
                    {
                        globalconfigFileName += "_warnaserror";
                        title += " escalated to 'error' severity";
                        description += " Enabled rules with 'warning' severity are escalated to 'error' severity to respect 'CodeAnalysisTreatWarningsAsErrors' MSBuild property.";
                    }

                    CreateGlobalconfig(
                        args.AnalyzerGlobalconfigsDir,
                        $"{globalconfigFileName}.globalconfig",
                        title,
                        description,
                        warnAsError,
                        analysisMode,
                        category,
                        allRulesById,
                        (releaseTrackingData, version, isShippedVersion));
                }

                static string GetNormalizedVersionStringForEditorconfigFileNameSuffix(Version version)
                {
                    var fieldCount = GetVersionFieldCount(version);
                    return version.ToString(fieldCount).Replace(".", "_", StringComparison.Ordinal);

                    static int GetVersionFieldCount(Version version)
                    {
                        if (version.Revision > 0)
                        {
                            return 4;
                        }

                        if (version.Build > 0)
                        {
                            return 3;
                        }

                        if (version.Minor > 0)
                        {
                            return 2;
                        }

                        return 1;
                    }
                }

                string GetAssemblyPath(string assembly)
                {
                    var assemblyName = Path.GetFileNameWithoutExtension(assembly);
                    var assemblyDir = Path.Combine(args.BinDirectory, assemblyName, args.Configuration, args.Tfm);
                    return Path.Combine(assemblyDir, assembly);
                }
            }
        }

        private static void CreateRuleset(
            string analyzerRulesetsDir,
            string rulesetFileName,
            string rulesetTitle,
            string rulesetDescription,
            RulesetKind rulesetKind,
            string? category,
            string? customTag,
            SortedList<string, DiagnosticDescriptor> sortedRulesById,
            string analyzerPackageName)
        {
            var text = GetRulesetOrEditorconfigText(
                rulesetKind,
                startRuleset,
                endRuleset,
                startRulesSection,
                endRulesSection,
                addRuleEntry,
                getSeverityString,
                commentStart: "   <!-- ",
                commentEnd: " -->",
                category,
                customTag,
                sortedRulesById);

            var directory = Directory.CreateDirectory(analyzerRulesetsDir);
            var rulesetFilePath = Path.Combine(directory.FullName, rulesetFileName);

            // This doesn't need validation as the generated file is part of artifacts.
            File.WriteAllText(rulesetFilePath, text);
            return;

            // Local functions
            void startRuleset(StringBuilder result)
            {
                result.AppendLine(@"<?xml version=""1.0""?>");
                result.AppendLine($@"<RuleSet Name=""{rulesetTitle}"" Description=""{rulesetDescription}"" ToolsVersion=""15.0"">");
            }

            static void endRuleset(StringBuilder result)
            {
                result.AppendLine("</RuleSet>");
            }

            void startRulesSection(StringBuilder result)
            {
                result.AppendLine($@"   <Rules AnalyzerId=""{analyzerPackageName}"" RuleNamespace=""{analyzerPackageName}"">");
            }

            static void endRulesSection(StringBuilder result)
            {
                result.AppendLine("   </Rules>");
            }

            static void addRuleEntry(StringBuilder result, DiagnosticDescriptor rule, string severity)
            {
                var spacing = new string(' ', count: 15 - severity.Length);
                result.AppendLine($@"      <Rule Id=""{rule.Id}"" Action=""{severity}"" /> {spacing} <!-- {rule.Title} -->");
            }

            static string getSeverityString(DiagnosticSeverity? severity)
            {
                return severity.HasValue ? severity.ToString() ?? "None" : "None";
            }
        }

        private static void CreateEditorconfig(
            string analyzerEditorconfigsDir,
            string editorconfigFolder,
            string editorconfigTitle,
            string editorconfigDescription,
            RulesetKind rulesetKind,
            string? category,
            string? customTag,
            SortedList<string, DiagnosticDescriptor> sortedRulesById)
        {
            var text = GetRulesetOrEditorconfigText(
                rulesetKind,
                startEditorconfig,
                endEditorconfig,
                startRulesSection,
                endRulesSection,
                addRuleEntry,
                GetSeverityString,
                commentStart: "# ",
                commentEnd: string.Empty,
                category,
                customTag,
                sortedRulesById);

            var directory = Directory.CreateDirectory(Path.Combine(analyzerEditorconfigsDir, editorconfigFolder));
            var editorconfigFilePath = Path.Combine(directory.FullName, ".editorconfig");

            // This doesn't need validation as the generated file is part of artifacts.
            File.WriteAllText(editorconfigFilePath, text);
            return;

            // Local functions
            void startEditorconfig(StringBuilder result)
            {
                result.AppendLine(@"# NOTE: Requires **VS2019 16.3** or later");
                result.AppendLine();
                result.AppendLine($@"# {editorconfigTitle}");
                result.AppendLine($@"# Description: {editorconfigDescription}");
                result.AppendLine();
                result.AppendLine(@"# Code files");
                result.AppendLine(@"[*.{cs,vb}]");
                result.AppendLine();
            }

            static void endEditorconfig(StringBuilder _)
            {
            }

            static void startRulesSection(StringBuilder _)
            {
            }

            static void endRulesSection(StringBuilder _)
            {
            }

            static void addRuleEntry(StringBuilder result, DiagnosticDescriptor rule, string severity)
            {
                result.AppendLine();
                result.AppendLine($"# {rule.Id}: {rule.Title}");
                result.AppendLine($@"dotnet_diagnostic.{rule.Id}.severity = {severity}");
            }
        }

        private static string GetRulesetOrEditorconfigText(
            RulesetKind rulesetKind,
            Action<StringBuilder> startRulesetOrEditorconfig,
            Action<StringBuilder> endRulesetOrEditorconfig,
            Action<StringBuilder> startRulesSection,
            Action<StringBuilder> endRulesSection,
            Action<StringBuilder, DiagnosticDescriptor, string> addRuleEntry,
            Func<DiagnosticSeverity?, string> getSeverityString,
            string commentStart,
            string commentEnd,
            string? category,
            string? customTag,
            SortedList<string, DiagnosticDescriptor> sortedRulesById)
        {
            Debug.Assert(category == null || customTag == null);
            Debug.Assert(category != null == (rulesetKind == RulesetKind.CategoryDefault || rulesetKind == RulesetKind.CategoryEnabled));
            Debug.Assert(customTag != null == (rulesetKind == RulesetKind.CustomTagDefault || rulesetKind == RulesetKind.CustomTagEnabled));

            var result = new StringBuilder();
            startRulesetOrEditorconfig(result);
            if (category == null && customTag == null)
            {
                addRules(categoryPass: false, customTagPass: false);
            }
            else
            {
                result.AppendLine($@"{commentStart}{category ?? customTag} Rules{commentEnd}");
                addRules(categoryPass: category != null, customTagPass: customTag != null);
                result.AppendLine();
                result.AppendLine();
                result.AppendLine();
                result.AppendLine($@"{commentStart}Other Rules{commentEnd}");
                addRules(categoryPass: false, customTagPass: false);
            }

            endRulesetOrEditorconfig(result);
            return result.ToString();

            void addRules(bool categoryPass, bool customTagPass)
            {
                if (!sortedRulesById.Any(r => !shouldSkipRule(r.Value)))
                {
                    // Bail out if we don't have any rule to be added for this assembly.
                    return;
                }

                startRulesSection(result);

                foreach (var rule in sortedRulesById)
                {
                    addRule(rule.Value);
                }

                endRulesSection(result);

                return;

                void addRule(DiagnosticDescriptor rule)
                {
                    if (shouldSkipRule(rule))
                    {
                        return;
                    }

                    string severity = getRuleAction(rule);
                    addRuleEntry(result, rule, severity);
                }

                bool shouldSkipRule(DiagnosticDescriptor rule)
                {
                    switch (rulesetKind)
                    {
                        case RulesetKind.CategoryDefault:
                        case RulesetKind.CategoryEnabled:
                            if (categoryPass)
                            {
                                return rule.Category != category;
                            }
                            else
                            {
                                return rule.Category == category;
                            }

                        case RulesetKind.CustomTagDefault:
                        case RulesetKind.CustomTagEnabled:
                            if (customTagPass)
                            {
                                return !rule.CustomTags.Contains(customTag);
                            }
                            else
                            {
                                return rule.CustomTags.Contains(customTag);
                            }

                        default:
                            return false;
                    }
                }

                string getRuleAction(DiagnosticDescriptor rule)
                {
                    return rulesetKind switch
                    {
                        RulesetKind.CategoryDefault => getRuleActionCore(enable: categoryPass && rule.IsEnabledByDefault),

                        RulesetKind.CategoryEnabled => getRuleActionCore(enable: categoryPass, enableAsWarning: categoryPass),

                        RulesetKind.CustomTagDefault => getRuleActionCore(enable: customTagPass && rule.IsEnabledByDefault),

                        RulesetKind.CustomTagEnabled => getRuleActionCore(enable: customTagPass, enableAsWarning: customTagPass),

                        RulesetKind.AllDefault => getRuleActionCore(enable: rule.IsEnabledByDefault),

                        RulesetKind.AllEnabled => getRuleActionCore(enable: true, enableAsWarning: true),

                        RulesetKind.AllDisabled => getRuleActionCore(enable: false),

                        _ => throw new InvalidProgramException(),
                    };

                    string getRuleActionCore(bool enable, bool enableAsWarning = false)
                    {
                        if (!enable && enableAsWarning)
                        {
                            throw new ArgumentException($"Unexpected arguments. '{nameof(enable)}' can't be false while '{nameof(enableAsWarning)}' is true.");
                        }
                        else if (enable)
                        {
                            return getSeverityString(enableAsWarning ? DiagnosticSeverity.Warning : rule.DefaultSeverity);
                        }
                        else
                        {
                            return getSeverityString(null);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates whether <paramref name="fileContents"/> matches the contents of <paramref name="fileWithPath"/>.
        /// If they don't match, <paramref name="fileWithPath"/> is added to <paramref name="fileNamesWithValidationFailures"/>.
        /// The validation process is run within CI, so that the CI build fails when the auto-generated files are out of date.
        /// </summary>
        /// <remarks>
        /// Don't call this method with auto-generated files that are part of the artifacts because it's expected that they don't initially exist.
        /// </remarks>
        private static void Validate(string fileWithPath, string fileContents, List<string> fileNamesWithValidationFailures)
        {
            string actual = File.ReadAllText(fileWithPath);
            if (actual != fileContents)
            {
                Console.Error.WriteLine($"'{fileWithPath}' does not match the expected contents.");
                Console.Error.WriteLine("Expected contents:");
                Console.Error.WriteLine(fileContents);
                Console.Error.WriteLine("Actual contents:");
                Console.Error.WriteLine(actual);
                fileNamesWithValidationFailures.Add(fileWithPath);
            }
        }

        private static void CreateGlobalconfig(
            string folder,
            string fileName,
            string title,
            string description,
            bool warnAsError,
            AnalysisMode analysisMode,
            string? category,
            SortedList<string, DiagnosticDescriptor> sortedRulesById,
            (ImmutableArray<ReleaseTrackingData> releaseTrackingData, Version version, bool isShippedVersion) releaseTrackingDataAndVersion)
        {
            Debug.Assert(fileName.EndsWith(".globalconfig", StringComparison.Ordinal));

            var text = GetGlobalconfigText(
                title,
                description,
                warnAsError,
                analysisMode,
                category,
                sortedRulesById,
                releaseTrackingDataAndVersion);
            var directory = Directory.CreateDirectory(folder);
#pragma warning disable CA1308 // Normalize strings to uppercase - Need to use 'ToLowerInvariant' for file names in non-Windows platforms
            var configFilePath = Path.Combine(directory.FullName, fileName.ToLowerInvariant());
#pragma warning restore CA1308 // Normalize strings to uppercase
            File.WriteAllText(configFilePath, text);
            return;

            // Local functions
            static string GetGlobalconfigText(
                string title,
                string description,
                bool warnAsError,
                AnalysisMode analysisMode,
                string? category,
                SortedList<string, DiagnosticDescriptor> sortedRulesById,
                (ImmutableArray<ReleaseTrackingData> releaseTrackingData, Version version, bool isShippedVersion)? releaseTrackingDataAndVersion)
            {
                var result = new StringBuilder();
                StartGlobalconfig();
                AddRules(analysisMode, category);
                return result.ToString();

                void StartGlobalconfig()
                {
                    result.AppendLine(@"# NOTE: Requires **VS2019 16.7** or later");
                    result.AppendLine();
                    result.AppendLine($@"# {title}");
                    result.AppendLine($@"# Description: {description}");
                    result.AppendLine();
                    result.AppendLine($@"is_global = true");
                    result.AppendLine();

                    // Append 'global_level' to ensure conflicts are properly resolved between different global configs:
                    //   1. Lowest precedence (-100): Category-agnostic config generated by us.
                    //   2. Higher precedence (-99): Category-specific config generated by us.
                    //   3. Highest predence (non-negative integer): User provided config.
                    // See https://github.com/dotnet/roslyn/issues/48634 for further details.
                    var globalLevel = category != null ? -99 : -100;
                    result.AppendLine($@"global_level = {globalLevel}");
                    result.AppendLine();
                }

                bool AddRules(AnalysisMode analysisMode, string? category)
                {
                    Debug.Assert(sortedRulesById.Count > 0);

                    var addedRule = false;
                    foreach (var rule in sortedRulesById)
                    {
                        if (AddRule(rule.Value, category))
                        {
                            addedRule = true;
                        }
                    }

                    return addedRule;

                    bool AddRule(DiagnosticDescriptor rule, string? category)
                    {
                        if (category != null &&
                            !string.Equals(rule.Category, category, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }

                        var (isEnabledByDefault, severity) = GetEnabledByDefaultAndSeverity(rule, analysisMode);
                        if (warnAsError && severity == DiagnosticSeverity.Warning && isEnabledByDefault)
                        {
                            severity = DiagnosticSeverity.Error;
                        }

                        if (rule.IsEnabledByDefault == isEnabledByDefault &&
                            severity == rule.DefaultSeverity)
                        {
                            // Rule had the same default severity and enabled state in the release.
                            // We do not need to generate any entry.
                            return false;
                        }

                        string severityString = GetRuleSeverity(isEnabledByDefault, severity);

                        result.AppendLine();
                        result.AppendLine($"# {rule.Id}: {rule.Title}");
                        result.AppendLine($@"dotnet_diagnostic.{rule.Id}.severity = {severityString}");
                        return true;
                    }

                    (bool isEnabledByDefault, DiagnosticSeverity effectiveSeverity) GetEnabledByDefaultAndSeverity(DiagnosticDescriptor rule, AnalysisMode analysisMode)
                    {
                        var isEnabledByDefault = rule.IsEnabledByDefault;
                        var effectiveSeverity = rule.DefaultSeverity;

                        bool isEnabledRuleForNonDefaultAnalysisMode;
                        switch (analysisMode)
                        {
                            case AnalysisMode.None:
                                // Disable all rules by default.
                                return (isEnabledByDefault: false, DiagnosticSeverity.Warning);

                            case AnalysisMode.All:
                                // Escalate all rules with a special custom tag to be build warnings.
                                isEnabledRuleForNonDefaultAnalysisMode = rule.CustomTags.Contains(WellKnownDiagnosticTagsExtensions.EnabledRuleInAggressiveMode);
                                break;

                            case AnalysisMode.Minimum:
                                // Escalate all enabled, non-hidden rules to be build warnings.
                                isEnabledRuleForNonDefaultAnalysisMode = isEnabledByDefault && effectiveSeverity != DiagnosticSeverity.Hidden;
                                break;

                            case AnalysisMode.Recommended:
                                // Escalate all enabled rules to be build warnings.
                                isEnabledRuleForNonDefaultAnalysisMode = isEnabledByDefault;
                                break;

                            case AnalysisMode.Default:
                                // Retain the default severity and enabled by default values.
                                isEnabledRuleForNonDefaultAnalysisMode = false;
                                break;

                            default:
                                throw new NotSupportedException();
                        }

                        if (isEnabledRuleForNonDefaultAnalysisMode)
                        {
                            isEnabledByDefault = true;
                            effectiveSeverity = DiagnosticSeverity.Warning;
                        }

                        if (releaseTrackingDataAndVersion != null)
                        {
                            isEnabledByDefault = isEnabledRuleForNonDefaultAnalysisMode;
                            var maxVersion = releaseTrackingDataAndVersion.Value.isShippedVersion ?
                                releaseTrackingDataAndVersion.Value.version :
                                ReleaseTrackingHelper.UnshippedVersion;
                            var foundReleaseTrackingEntry = false;
                            foreach (var releaseTrackingData in releaseTrackingDataAndVersion.Value.releaseTrackingData)
                            {
                                if (releaseTrackingData.TryGetLatestReleaseTrackingLine(rule.Id, maxVersion, out _, out var releaseTrackingLine))
                                {
                                    foundReleaseTrackingEntry = true;

                                    if (releaseTrackingLine.EnabledByDefault.HasValue &&
                                        releaseTrackingLine.DefaultSeverity.HasValue)
                                    {
                                        isEnabledByDefault = releaseTrackingLine.EnabledByDefault.Value && !releaseTrackingLine.IsRemovedRule;
                                        effectiveSeverity = releaseTrackingLine.DefaultSeverity.Value;

                                        if (isEnabledRuleForNonDefaultAnalysisMode && !releaseTrackingLine.IsRemovedRule)
                                        {
                                            isEnabledByDefault = true;
                                            effectiveSeverity = DiagnosticSeverity.Warning;
                                        }

                                        break;
                                    }
                                }
                            }

                            if (!foundReleaseTrackingEntry)
                            {
                                // Rule is unshipped or first shipped in a version later than 'maxVersion', so mark it as disabled.
                                isEnabledByDefault = false;
                            }
                        }

                        return (isEnabledByDefault, effectiveSeverity);
                    }

                    static string GetRuleSeverity(bool isEnabledByDefault, DiagnosticSeverity defaultSeverity)
                    {
                        if (isEnabledByDefault)
                        {
                            return GetSeverityString(defaultSeverity);
                        }
                        else
                        {
                            return GetSeverityString(null);
                        }
                    }
                }
            }
        }

        private static string GetSeverityString(DiagnosticSeverity? severity)
        {
            if (!severity.HasValue)
            {
                return "none";
            }

            return severity.Value switch
            {
                DiagnosticSeverity.Error => "error",
                DiagnosticSeverity.Warning => "warning",
                DiagnosticSeverity.Info => "suggestion",
                DiagnosticSeverity.Hidden => "silent",
                _ => throw new NotImplementedException(severity.Value.ToString()),
            };
        }

        private static void CreateTargetsFile(string targetsFileDir, string targetsFileName, string packageName, IOrderedEnumerable<string> categories)
        {
            if (string.IsNullOrEmpty(targetsFileDir) || string.IsNullOrEmpty(targetsFileName))
            {
                return;
            }

            var fileContents =
                $"""
                <Project>{GetCommonContents(packageName, categories)}{GetPackageSpecificContents(packageName)}
                </Project>
                """;
            var directory = Directory.CreateDirectory(targetsFileDir);
            var fileWithPath = Path.Combine(directory.FullName, targetsFileName);
            File.WriteAllText(fileWithPath, fileContents);

            static string GetCommonContents(string packageName, IOrderedEnumerable<string> categories)
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append(GetGlobalAnalyzerConfigTargetContents(packageName, category: null));
                foreach (var category in categories)
                {
                    stringBuilder.Append(GetGlobalAnalyzerConfigTargetContents(packageName, category));
                }

                stringBuilder.Append(GetMSBuildContentForPropertyAndItemOptions());
                stringBuilder.Append(GetCodeAnalysisTreatWarningsAsErrorsTargetContents());
                return stringBuilder.ToString();
            }

            static string GetGlobalAnalyzerConfigTargetContents(string packageName, string? category)
            {
                var analysisLevelPropName = "AnalysisLevel";
                var analysisLevelPrefixPropName = "AnalysisLevelPrefix";
                var analysisLevelSuffixPropName = "AnalysisLevelSuffix";
                var analysisModePropName = nameof(AnalysisMode);
                var effectiveAnalysisLevelPropName = "EffectiveAnalysisLevel";
                var targetCondition = "'$(SkipGlobalAnalyzerConfigForPackage)' != 'true'";
                var afterTargets = string.Empty;
                var trimmedPackageName = packageName.Replace(".", string.Empty, StringComparison.Ordinal);

                if (!string.IsNullOrEmpty(category))
                {
                    analysisLevelPropName += category;
                    analysisLevelPrefixPropName += category;
                    analysisLevelSuffixPropName += category;
                    analysisModePropName += category;
                    effectiveAnalysisLevelPropName += category;

                    // For category-specific target, we also check if end-user has overriden category-specific AnalysisLevel or AnalysisMode.
                    targetCondition += $" and ('$({analysisLevelPropName})' != '' or '$({analysisModePropName})' != '')";

                    // Ensure that category-specific target executes after category-agnostic target
                    afterTargets += $@"AfterTargets=""AddGlobalAnalyzerConfigForPackage_{trimmedPackageName}"" ";

                    trimmedPackageName += category;
                }

                var packageVersionPropName = trimmedPackageName + "RulesVersion";
                var propertyStringForSettingDefaultPropertyValues = GetPropertyStringForSettingDefaultPropertyValues(
                    packageName, packageVersionPropName, category, analysisLevelPropName,
                    analysisLevelPrefixPropName, analysisLevelSuffixPropName, effectiveAnalysisLevelPropName);

                return $"""

                      <Target Name="AddGlobalAnalyzerConfigForPackage_{trimmedPackageName}" BeforeTargets="CoreCompile" {afterTargets}Condition="{targetCondition}">
                        <!-- PropertyGroup to compute global analyzer config file to be used -->
                        <PropertyGroup>{propertyStringForSettingDefaultPropertyValues}
                          <!-- Set the default analysis mode, if not set by the user -->
                          <_GlobalAnalyzerConfigAnalysisMode_{trimmedPackageName}>$({analysisLevelSuffixPropName})</_GlobalAnalyzerConfigAnalysisMode_{trimmedPackageName}>
                          <_GlobalAnalyzerConfigAnalysisMode_{trimmedPackageName} Condition="'$(_GlobalAnalyzerConfigAnalysisMode_{trimmedPackageName})' == ''">$({analysisModePropName})</_GlobalAnalyzerConfigAnalysisMode_{trimmedPackageName}>
                          <_GlobalAnalyzerConfigAnalysisMode_{trimmedPackageName} Condition="'$(_GlobalAnalyzerConfigAnalysisMode_{trimmedPackageName})' == 'AllEnabledByDefault'">{nameof(AnalysisMode.All)}</_GlobalAnalyzerConfigAnalysisMode_{trimmedPackageName}>
                          <_GlobalAnalyzerConfigAnalysisMode_{trimmedPackageName} Condition="'$(_GlobalAnalyzerConfigAnalysisMode_{trimmedPackageName})' == 'AllDisabledByDefault'">{nameof(AnalysisMode.None)}</_GlobalAnalyzerConfigAnalysisMode_{trimmedPackageName}>
                          <_GlobalAnalyzerConfigAnalysisMode_{trimmedPackageName} Condition="'$(_GlobalAnalyzerConfigAnalysisMode_{trimmedPackageName})' == ''">{nameof(AnalysisMode.Default)}</_GlobalAnalyzerConfigAnalysisMode_{trimmedPackageName}>

                          <!-- Default 'EffectiveCodeAnalysisTreatWarningsAsErrors' to 'CodeAnalysisTreatWarningsAsErrors' for escalating relevant code analysis warnings to errors. -->
                          <!-- We use a separate property to allow users to override 'CodeAnalysisTreatWarningsAsErrors' implementation from .NET7 or older SDK, which had a known issue: https://github.com/dotnet/roslyn-analyzers/issues/6281 -->
                          <EffectiveCodeAnalysisTreatWarningsAsErrors Condition="'$(EffectiveCodeAnalysisTreatWarningsAsErrors)' == ''">$(CodeAnalysisTreatWarningsAsErrors)</EffectiveCodeAnalysisTreatWarningsAsErrors>
                          <!-- Choose GlobalAnalyzerConfig file with '_warnaserror' suffix if 'EffectiveCodeAnalysisTreatWarningsAsErrors' is 'true'. -->
                          <_GlobalAnalyzerConfigFileName_{trimmedPackageName}_WarnAsErrorSuffix Condition="'$(EffectiveCodeAnalysisTreatWarningsAsErrors)' == 'true'">_warnaserror</_GlobalAnalyzerConfigFileName_{trimmedPackageName}_WarnAsErrorSuffix>

                          <!-- GlobalAnalyzerConfig file name based on user specified package version '{packageVersionPropName}', if any. We replace '.' with '_' to map the version string to file name suffix. -->
                          <_GlobalAnalyzerConfigFileName_{trimmedPackageName} Condition="'$({packageVersionPropName})' != ''">{analysisLevelPropName}_$({packageVersionPropName}.Replace(".","_"))_$(_GlobalAnalyzerConfigAnalysisMode_{trimmedPackageName})$(_GlobalAnalyzerConfigFileName_{trimmedPackageName}_WarnAsErrorSuffix).globalconfig</_GlobalAnalyzerConfigFileName_{trimmedPackageName}>
                          <_GlobalAnalyzerConfigFileName_{trimmedPackageName}>$(_GlobalAnalyzerConfigFileName_{trimmedPackageName}.ToLowerInvariant())</_GlobalAnalyzerConfigFileName_{trimmedPackageName}>

                          <_GlobalAnalyzerConfigDir_{trimmedPackageName} Condition="'$(_GlobalAnalyzerConfigDir_{trimmedPackageName})' == ''">$(MSBuildThisFileDirectory)config</_GlobalAnalyzerConfigDir_{trimmedPackageName}>
                          <_GlobalAnalyzerConfigFile_{trimmedPackageName} Condition="'$(_GlobalAnalyzerConfigFileName_{trimmedPackageName})' != ''">$(_GlobalAnalyzerConfigDir_{trimmedPackageName})\$(_GlobalAnalyzerConfigFileName_{trimmedPackageName})</_GlobalAnalyzerConfigFile_{trimmedPackageName}>
                        </PropertyGroup>

                        <ItemGroup Condition="Exists('$(_GlobalAnalyzerConfigFile_{trimmedPackageName})')">
                          <EditorConfigFiles Include="$(_GlobalAnalyzerConfigFile_{trimmedPackageName})" />
                        </ItemGroup>
                      </Target>

                    """;

                static string GetPropertyStringForSettingDefaultPropertyValues(
                    string packageName,
                    string packageVersionPropName,
                    string? category,
                    string analysisLevelPropName,
                    string analysisLevelPrefixPropName,
                    string analysisLevelSuffixPropName,
                    string effectiveAnalysisLevelPropName)
                {
                    if (packageName == NetAnalyzersPackageName)
                    {
                        var propertyStr = string.Empty;

                        if (!string.IsNullOrEmpty(category))
                        {
                            // For category-specific logic, we need to duplicate logic from SDK targets to set
                            // category-specific AnalysisLevel property values. In future, we should consider removing similar logic from
                            // SDK targets for core AnalysisLevel and instead generalize this logic.

                            propertyStr += $"""

                                      <!-- Default '{analysisLevelPropName}' to the core 'AnalysisLevel' and compute '{analysisLevelPrefixPropName}', '{analysisLevelSuffixPropName}' and '{effectiveAnalysisLevelPropName}' -->
                                      <{analysisLevelPropName} Condition="'$({analysisLevelPropName})' == ''">$(AnalysisLevel)</{analysisLevelPropName}>

                                      <!-- {analysisLevelPropName} can also contain compound values with a prefix and suffix separated by a '-' character.
                                           The prefix indicates the core AnalysisLevel for '{category}' rules and the suffix indicates the bucket of
                                           rules to enable for '{category}' rules by default. For example, some valid compound values for {analysisLevelPropName} are:
                                             1. '5-all' - Indicates core {analysisLevelPropName} = '5' with 'all' the '{category}' rules enabled by default.
                                             2. 'latest-none' - Indicates core {analysisLevelPropName} = 'latest' with 'none' of the '{category}' rules enabled by default.
                                           {analysisLevelPrefixPropName} is used to set the {effectiveAnalysisLevelPropName} below.
                                           {analysisLevelSuffixPropName} is used to map to the correct global config.
                                      -->
                                      <{analysisLevelPrefixPropName} Condition="$({analysisLevelPropName}.Contains('-'))">$([System.Text.RegularExpressions.Regex]::Replace($({analysisLevelPropName}), '-(.)*', ''))</{analysisLevelPrefixPropName}>
                                      <{analysisLevelSuffixPropName} Condition="'$({analysisLevelPrefixPropName})' != ''">$([System.Text.RegularExpressions.Regex]::Replace($({analysisLevelPropName}), '$({analysisLevelPrefixPropName})-', ''))</{analysisLevelSuffixPropName}>

                                      <!-- {effectiveAnalysisLevelPropName} is used to differentiate from user specified strings (such as 'none')
                                           and an implied numerical option (such as '4') -->
                                      <{effectiveAnalysisLevelPropName} Condition="'$({analysisLevelPropName})' == 'none' or '$({analysisLevelPrefixPropName})' == 'none'">$(_NoneAnalysisLevel)</{effectiveAnalysisLevelPropName}>
                                      <{effectiveAnalysisLevelPropName} Condition="'$({analysisLevelPropName})' == 'latest' or '$({analysisLevelPrefixPropName})' == 'latest'">$(_LatestAnalysisLevel)</{effectiveAnalysisLevelPropName}>
                                      <{effectiveAnalysisLevelPropName} Condition="'$({analysisLevelPropName})' == 'preview' or '$({analysisLevelPrefixPropName})' == 'preview'">$(_PreviewAnalysisLevel)</{effectiveAnalysisLevelPropName}>

                                      <!-- Set {effectiveAnalysisLevelPropName} to the value of {analysisLevelPropName} if it is a version number -->
                                      <{effectiveAnalysisLevelPropName} Condition="'$({effectiveAnalysisLevelPropName})' == '' And
                                                                         '$({analysisLevelPrefixPropName})' != ''">$({analysisLevelPrefixPropName})</{effectiveAnalysisLevelPropName}>
                                      <{effectiveAnalysisLevelPropName} Condition="'$({effectiveAnalysisLevelPropName})' == '' And
                                                                         '$({analysisLevelPropName})' != ''">$({analysisLevelPropName})</{effectiveAnalysisLevelPropName}>

                                """;
                        }

                        propertyStr += $"""

                                  <!-- Default '{packageVersionPropName}' to '{effectiveAnalysisLevelPropName}' with trimmed trailing '.0' -->
                                  <{packageVersionPropName} Condition="'$({packageVersionPropName})' == '' and $({effectiveAnalysisLevelPropName}) != ''">$([System.Text.RegularExpressions.Regex]::Replace($({effectiveAnalysisLevelPropName}), '(.0)*$', ''))</{packageVersionPropName}>

                            """;
                        return propertyStr;
                    }

                    return string.Empty;
                }
            }

            static string GetMSBuildContentForPropertyAndItemOptions()
            {
                var builder = new StringBuilder();

                AddMSBuildContentForPropertyOptions(builder);
                AddMSBuildContentForItemOptions(builder);

                return builder.ToString();

                static void AddMSBuildContentForPropertyOptions(StringBuilder builder)
                {
                    var compilerVisibleProperties = new List<string>();
                    foreach (var field in typeof(MSBuildPropertyOptionNames).GetFields())
                    {
                        compilerVisibleProperties.Add(field.Name);
                    }

                    // Add ItemGroup for MSBuild property names that are required to be threaded as analyzer config options.
                    AddItemGroupForCompilerVisibleProperties(compilerVisibleProperties, builder);
                }

                static void AddItemGroupForCompilerVisibleProperties(List<string> compilerVisibleProperties, StringBuilder builder)
                {
                    builder.AppendLine($"""

                          <!-- MSBuild properties to thread to the analyzers as options -->
                          <ItemGroup>
                        """);
                    foreach (var compilerVisibleProperty in compilerVisibleProperties)
                    {
                        builder.AppendLine($@"    <CompilerVisibleProperty Include=""{compilerVisibleProperty}"" />");
                    }

                    builder.AppendLine($@"  </ItemGroup>");
                }

                static void AddMSBuildContentForItemOptions(StringBuilder builder)
                {
                    // Add ItemGroup and PropertyGroup for MSBuild item names that are required to be treated as analyzer config options.
                    // The analyzer config option will have the following key/value:
                    // - Key: Item name prefixed with an '_' and suffixed with a 'List' to reduce chances of conflicts with any existing project property.
                    // - Value: Concatenated item metadata values, separated by a ',' character. See https://github.com/dotnet/sdk/issues/12706#issuecomment-668219422 for details.

                    builder.Append($"""

                          <!-- MSBuild item metadata to thread to the analyzers as options -->
                          <PropertyGroup>

                        """);
                    var compilerVisibleProperties = new List<string>();
                    foreach (var field in typeof(MSBuildItemOptionNames).GetFields())
                    {
                        // Item option name: "SupportedPlatform"
                        // Generated MSBuild property: "<_SupportedPlatformList>@(SupportedPlatform, '<separator>')</_SupportedPlatformList>"

                        var itemOptionName = field.Name;
                        var propertyName = MSBuildItemOptionNamesHelpers.GetPropertyNameForItemOptionName(itemOptionName);
                        compilerVisibleProperties.Add(propertyName);
                        builder.AppendLine($@"    <{propertyName}>@({itemOptionName}, '{MSBuildItemOptionNamesHelpers.ValuesSeparator}')</{propertyName}>");
                    }

                    builder.AppendLine($@"  </PropertyGroup>");

                    AddItemGroupForCompilerVisibleProperties(compilerVisibleProperties, builder);
                }
            }

            static string GetCodeAnalysisTreatWarningsAsErrorsTargetContents()
            {
                return $"""

                      <!--
                        Design-time target to handle 'CodeAnalysisTreatWarningsAsErrors = false' for the CA rule ids implemented in this package.
                        Note that a similar 'WarningsNotAsErrors' property group is present in the generated props file to ensure this functionality on command line builds.
                      -->
                      <Target Name="_CodeAnalysisTreatWarningsAsErrors" BeforeTargets="CoreCompile" Condition="'$(DesignTimeBuild)' == 'true' OR '$(BuildingProject)' != 'true'">
                        <PropertyGroup>
                          <EffectiveCodeAnalysisTreatWarningsAsErrors Condition="'$(EffectiveCodeAnalysisTreatWarningsAsErrors)' == ''">$(CodeAnalysisTreatWarningsAsErrors)</EffectiveCodeAnalysisTreatWarningsAsErrors>
                          <WarningsNotAsErrors Condition="'$(EffectiveCodeAnalysisTreatWarningsAsErrors)' == 'false' and '$(TreatWarningsAsErrors)' == 'true'">$(WarningsNotAsErrors);$(CodeAnalysisRuleIds)</WarningsNotAsErrors>
                        </PropertyGroup>
                      </Target>

                    """;
            }

            const string AddAllResxFilesAsAdditionalFilesTarget = """
                  <!-- Target to add all 'EmbeddedResource' files with '.resx' extension as analyzer additional files -->
                  <Target Name="AddAllResxFilesAsAdditionalFiles" DependsOnTargets="PrepareResourceNames" BeforeTargets="GenerateMSBuildEditorConfigFileCore;CoreCompile" Condition="'@(EmbeddedResource)' != '' AND '$(SkipAddAllResxFilesAsAdditionalFiles)' != 'true'">
                    <ItemGroup>
                      <EmbeddedResourceWithResxExtension Include="@(EmbeddedResource)" Condition="'%(Extension)' == '.resx'" />
                      <AdditionalFiles Include="@(EmbeddedResourceWithResxExtension)" />
                    </ItemGroup>
                  </Target>
                """;

            static string GetPackageSpecificContents(string packageName)
                => packageName switch
                {
                    CodeAnalysisAnalyzersPackageName => $"""

                    {AddAllResxFilesAsAdditionalFilesTarget}

                      <!-- Workaround for https://github.com/dotnet/roslyn/issues/4655 -->
                      <ItemGroup Condition="Exists('$(MSBuildProjectDirectory)\AnalyzerReleases.Shipped.md')" >
                    	<AdditionalFiles Include="AnalyzerReleases.Shipped.md" />
                      </ItemGroup>
                      <ItemGroup Condition="Exists('$(MSBuildProjectDirectory)\AnalyzerReleases.Unshipped.md')" >
                    	<AdditionalFiles Include="AnalyzerReleases.Unshipped.md" />
                      </ItemGroup>
                    """,
                    PublicApiAnalyzersPackageName => """


                      <!-- Workaround for https://github.com/dotnet/roslyn/issues/4655 -->
                      <ItemGroup Condition="Exists('$(MSBuildProjectDirectory)\PublicAPI.Shipped.txt')" >
                    	<AdditionalFiles Include="PublicAPI.Shipped.txt" />
                      </ItemGroup>
                      <ItemGroup Condition="Exists('$(MSBuildProjectDirectory)\PublicAPI.Unshipped.txt')" >
                    	<AdditionalFiles Include="PublicAPI.Unshipped.txt" />
                      </ItemGroup>
                    """,
                    PerformanceSensitiveAnalyzersPackageName => """

                      <PropertyGroup>
                        <GeneratePerformanceSensitiveAttribute Condition="'$(GeneratePerformanceSensitiveAttribute)' == ''">true</GeneratePerformanceSensitiveAttribute>
                        <PerformanceSensitiveAttributePath Condition="'$(PerformanceSensitiveAttributePath)' == ''">$(MSBuildThisFileDirectory)PerformanceSensitiveAttribute$(DefaultLanguageSourceExtension)</PerformanceSensitiveAttributePath>
                      </PropertyGroup>

                      <ItemGroup Condition="'$(GeneratePerformanceSensitiveAttribute)' == 'true' and Exists($(PerformanceSensitiveAttributePath))">
                        <Compile Include="$(PerformanceSensitiveAttributePath)" Visible="false" />

                        <!-- Make sure the source file is embedded in PDB to support Source Link -->
                        <EmbeddedFiles Condition="'$(DebugType)' != 'none'" Include="$(PerformanceSensitiveAttributePath)" />
                      </ItemGroup>
                    """,
                    ResxSourceGeneratorPackageName => $"""

                      <!-- Special handling for embedded resources to show as nested in Solution Explorer -->
                      <ItemGroup>
                        <!-- Localized embedded resources are just dependent on the parent RESX -->
                        <EmbeddedResource Update="**\*.??.resx;**\*.??-??.resx;**\*.??-????.resx" DependentUpon="$([System.IO.Path]::ChangeExtension($([System.IO.Path]::GetFileNameWithoutExtension(%(Identity))), '.resx'))" />
                      </ItemGroup>

                    {AddAllResxFilesAsAdditionalFilesTarget}

                      <!-- Target to add 'EmbeddedResource' files with '.resx' extension and explicit- or implicit-GenerateSource as analyzer additional files. This only needs to run when SkipAddAllResxFilesAsAdditionalFiles is set to true.
                             Explicit GenerateSource: The embedded resource has GenerateSource="true"
                             Implicit GenerateSource: The embedded resource did not set GenerateSource, and also does not have WithCulture set to true
                      -->
                      <Target Name="AddGenerateSourceResxFilesAsAdditionalFiles" BeforeTargets="GenerateMSBuildEditorConfigFileCore;CoreCompile" Condition="'@(EmbeddedResource)' != '' AND '$(SkipAddAllResxFilesAsAdditionalFiles)' == 'true' AND '$(SkipAddGenerateSourceResxFilesAsAdditionalFiles)' != 'true'">
                        <ItemGroup>
                          <EmbeddedResourceWithResxExtensionAndGenerateSource Include="@(EmbeddedResource)" Condition="'%(Extension)' == '.resx' AND ('%(EmbeddedResource.GenerateSource)' == 'true' OR ('%(EmbeddedResource.GenerateSource)' != 'false' AND '%(EmbeddedResource.WithCulture)' != 'true'))" />
                          <AdditionalFiles Include="@(EmbeddedResourceWithResxExtensionAndGenerateSource)" />
                        </ItemGroup>
                      </Target>

                    """,
                    _ => string.Empty,
                };
        }

        private enum RulesetKind
        {
            AllDefault,
            CategoryDefault,
            CustomTagDefault,
            AllEnabled,
            CategoryEnabled,
            CustomTagEnabled,
            AllDisabled,
        }

        // NOTE: **Do not** change the names of the fields for this enum - that would be a breaking change for user visible property setting for `AnalysisMode` property in MSBuild project file.
        private enum AnalysisMode
        {
            Default,
            None,
            Minimum,
            Recommended,
            All
        }

        private sealed class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
        {
            public static IAnalyzerAssemblyLoader Instance = new AnalyzerAssemblyLoader();

            private AnalyzerAssemblyLoader() { }
            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
            {
                return Assembly.LoadFrom(fullPath);
            }
        }
    }
}
