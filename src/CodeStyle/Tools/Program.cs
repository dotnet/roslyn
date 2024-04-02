// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace CodeStyleConfigFileGenerator
{
    public static class Program
    {
        private const int ExpectedArguments = 4;

        private static readonly string s_neverTag = EnforceOnBuild.Never.ToCustomTag();
        private static readonly string s_whenExplicitlyEnabledTag = EnforceOnBuild.WhenExplicitlyEnabled.ToCustomTag();
        private static readonly string s_recommendedTag = EnforceOnBuild.Recommended.ToCustomTag();
        private static readonly string s_highlyRecommendedTag = EnforceOnBuild.HighlyRecommended.ToCustomTag();

        public static int Main(string[] args)
        {
            if (args.Length != ExpectedArguments)
            {
                Console.Error.WriteLine($"Excepted {ExpectedArguments} arguments, found {args.Length}: {string.Join(';', args)}");
                return 1;
            }

            var language = args[0];
            var outputDir = args[1];
            var targetsFileName = args[2];
            var assemblyList = args[3].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();

            CreateGlobalConfigFiles(language, outputDir, assemblyList);
            CreateTargetsFile(language, outputDir, targetsFileName);
            return 0;
        }

        private static void CreateGlobalConfigFiles(string language, string outputDir, ImmutableArray<string> assemblyList)
        {
            Debug.Assert(language is "CSharp" or "VisualBasic");
            var languageForGetAnalyzers = language == "CSharp" ? LanguageNames.CSharp : LanguageNames.VisualBasic;

            var allRulesById = new SortedList<string, DiagnosticDescriptor>();
            foreach (var assembly in assemblyList)
            {
                var analyzerFileReference = new AnalyzerFileReference(assembly, AnalyzerAssemblyLoader.Instance);
                analyzerFileReference.AnalyzerLoadFailed += AnalyzerFileReference_AnalyzerLoadFailed;
                var analyzers = analyzerFileReference.GetAnalyzers(languageForGetAnalyzers);

                foreach (var analyzer in analyzers)
                {
                    foreach (var rule in analyzer.SupportedDiagnostics)
                    {
                        allRulesById[rule.Id] = rule;
                    }
                }
            }

            var configDir = Path.Combine(outputDir, "config");
            foreach (var analysisMode in Enum.GetValues(typeof(AnalysisMode)))
            {
                CreateGlobalconfig(
                    configDir,
                    $"AnalysisLevelStyle_{analysisMode}.globalconfig".ToLowerInvariant(),
                    (AnalysisMode)analysisMode!,
                    allRulesById);
            }

            return;

            static void AnalyzerFileReference_AnalyzerLoadFailed(object? sender, AnalyzerLoadFailureEventArgs e)
                => throw e.Exception ?? new InvalidOperationException(e.Message);

            static void CreateGlobalconfig(
                string folder,
                string configFileName,
                AnalysisMode analysisMode,
                SortedList<string, DiagnosticDescriptor> sortedRulesById)
            {
                var text = GetGlobalconfigText(analysisMode, sortedRulesById);
                var directory = Directory.CreateDirectory(folder);
                var configFilePath = Path.Combine(directory.FullName, configFileName);
                File.WriteAllText(configFilePath, text);
                return;

                // Local functions
                static string GetGlobalconfigText(AnalysisMode analysisMode, SortedList<string, DiagnosticDescriptor> sortedRulesById)
                {
                    var result = new StringBuilder();
                    StartGlobalconfig();
                    AddRules(analysisMode);
                    return result.ToString();

                    void StartGlobalconfig()
                    {
                        result.AppendLine("# NOTE: Requires **VS2019 16.7** or later");
                        result.AppendLine();
                        result.AppendLine($"# Style rules with '{analysisMode}' analysis mode");
                        result.AppendLine();
                        result.AppendLine("is_global = true");
                        result.AppendLine();

                        // Append 'global_level' to ensure conflicts are properly resolved between different global configs:
                        //   1. Lowest precedence (-99): SDK config file generated by us.
                        //   2. Highest predence (non-negative integer): User provided config.
                        // See https://github.com/dotnet/roslyn/issues/48634 for further details.
                        result.AppendLine("global_level = -99");
                        result.AppendLine();
                    }

                    bool AddRules(AnalysisMode analysisMode)
                    {
                        Debug.Assert(sortedRulesById.Count > 0);

                        var addedRule = false;
                        foreach (var rule in sortedRulesById)
                        {
                            if (AddRule(rule.Value))
                            {
                                addedRule = true;
                            }
                        }

                        return addedRule;

                        bool AddRule(DiagnosticDescriptor rule)
                        {
                            var (isEnabledByDefault, severity) = GetEnabledByDefaultAndSeverity(rule, analysisMode);
                            if (rule.IsEnabledByDefault == isEnabledByDefault &&
                                severity == rule.DefaultSeverity)
                            {
                                // Rule had the same default severity and enabled state.
                                // We do not need to generate any entry.
                                return false;
                            }

                            var severityString = isEnabledByDefault ? severity.ToEditorConfigString() : EditorConfigSeverityStrings.None;

                            result.AppendLine();
                            result.AppendLine($"# {rule.Id}: {rule.Title}");
                            result.AppendLine($"dotnet_diagnostic.{rule.Id}.severity = {severityString}");
                            return true;
                        }

                        (bool isEnabledByDefault, DiagnosticSeverity effectiveSeverity) GetEnabledByDefaultAndSeverity(DiagnosticDescriptor rule, AnalysisMode analysisMode)
                        {
                            Debug.Assert(rule.CustomTags.Any(c => c == s_neverTag || c == s_whenExplicitlyEnabledTag || c == s_recommendedTag || c == s_highlyRecommendedTag),
                                $"DiagnosticDescriptor for '{rule.Id}' must have a {nameof(EnforceOnBuild)} custom tag");

                            bool isEnabledInNonDefaultMode;
                            switch (analysisMode)
                            {
                                case AnalysisMode.None:
                                    // Disable all rules by default.
                                    return (isEnabledByDefault: false, DiagnosticSeverity.Warning);

                                case AnalysisMode.All:
                                    // Escalate all rules which can be enabled on build.
                                    isEnabledInNonDefaultMode = !rule.CustomTags.Contains(s_neverTag);
                                    break;

                                case AnalysisMode.Minimum:
                                    // Escalate all highly recommended rules.
                                    isEnabledInNonDefaultMode = rule.CustomTags.Contains(s_highlyRecommendedTag);
                                    break;

                                case AnalysisMode.Recommended:
                                    // Escalate all recommended and highly recommended rules.
                                    isEnabledInNonDefaultMode = rule.CustomTags.Contains(s_highlyRecommendedTag) || rule.CustomTags.Contains(s_recommendedTag);
                                    break;

                                case AnalysisMode.Default:
                                    // Retain the default severity and enabled by default values.
                                    isEnabledInNonDefaultMode = false;
                                    break;

                                default:
                                    throw ExceptionUtilities.UnexpectedValue(analysisMode);
                            }

                            return (isEnabledByDefault: rule.IsEnabledByDefault,
                                    effectiveSeverity: isEnabledInNonDefaultMode ? DiagnosticSeverity.Warning : rule.DefaultSeverity);
                        }
                    }
                }
            }
        }

        private static void CreateTargetsFile(string language, string outputDir, string targetsFileName)
        {
            var fileContents =
$@"<Project>{GetTargetContents(language)}
</Project>";

            var directory = Directory.CreateDirectory(outputDir);
            var fileWithPath = Path.Combine(directory.FullName, targetsFileName);
            File.WriteAllText(fileWithPath, fileContents);
            return;

            static string GetTargetContents(string language)
            {
                return $"""

                      <Target Name="AddGlobalAnalyzerConfigForPackage_MicrosoftCodeAnalysis{language}CodeStyle" BeforeTargets="GenerateMSBuildEditorConfigFileCore;CoreCompile" Condition="'$(SkipGlobalAnalyzerConfigForPackage)' != 'true'">
                        <!-- PropertyGroup to compute global analyzer config file to be used -->
                        <PropertyGroup>
                          <!-- Default 'AnalysisLevelStyle' to the core 'AnalysisLevel' -->
                          <AnalysisLevelStyle Condition="'$(AnalysisLevelStyle)' == ''">$(AnalysisLevel)</AnalysisLevelStyle>

                          <!-- AnalysisLevelStyle can also contain compound values with a prefix and suffix separated by a '-' character.
                               The prefix indicates the core AnalysisLevel for 'Style' rules and the suffix indicates the bucket of
                               rules to enable for 'Style' rules by default. For example, some valid compound values for AnalysisLevelStyle are:
                                 1. '5-all' - Indicates core AnalysisLevelStyle = '5' with 'all' the 'Style' rules enabled by default.
                                 2. 'latest-none' - Indicates core AnalysisLevelStyle = 'latest' with 'none' of the 'Style' rules enabled by default.
                               AnalysisLevelPrefixStyle is used to set the EffectiveAnalysisLevelStyle below.
                               AnalysisLevelSuffixStyle is used to map to the correct global config.
                          -->
                          <AnalysisLevelPrefixStyle Condition="$(AnalysisLevelStyle.Contains('-'))">$([System.Text.RegularExpressions.Regex]::Replace($(AnalysisLevelStyle), '-(.)*', ''))</AnalysisLevelPrefixStyle>
                          <AnalysisLevelSuffixStyle Condition="'$(AnalysisLevelPrefixStyle)' != ''">$([System.Text.RegularExpressions.Regex]::Replace($(AnalysisLevelStyle), '$(AnalysisLevelPrefixStyle)-', ''))</AnalysisLevelSuffixStyle>

                          <!-- Default 'AnalysisLevelSuffixStyle' to the core 'AnalysisLevelSuffix' -->
                          <AnalysisLevelSuffixStyle Condition="'$(AnalysisLevelSuffixStyle)' == ''">$(AnalysisLevelSuffix)</AnalysisLevelSuffixStyle>
                          <!-- Default 'AnalysisModeStyle' to the core 'AnalysisMode' -->
                          <AnalysisModeStyle Condition="'$(AnalysisModeStyle)' == ''">$(AnalysisMode)</AnalysisModeStyle>

                          <!-- EffectiveAnalysisLevelStyle is used to differentiate from user specified strings (such as 'none')
                               and an implied numerical option (such as '4') -->
                          <EffectiveAnalysisLevelStyle Condition="'$(AnalysisLevelStyle)' == 'none' or '$(AnalysisLevelPrefixStyle)' == 'none'">$(_NoneAnalysisLevel)</EffectiveAnalysisLevelStyle>
                          <EffectiveAnalysisLevelStyle Condition="'$(AnalysisLevelStyle)' == 'latest' or '$(AnalysisLevelPrefixStyle)' == 'latest'">$(_LatestAnalysisLevel)</EffectiveAnalysisLevelStyle>
                          <EffectiveAnalysisLevelStyle Condition="'$(AnalysisLevelStyle)' == 'preview' or '$(AnalysisLevelPrefixStyle)' == 'preview'">$(_PreviewAnalysisLevel)</EffectiveAnalysisLevelStyle>

                          <!-- Set EffectiveAnalysisLevelStyle to the value of AnalysisLevelStyle if it is a version number -->
                          <EffectiveAnalysisLevelStyle Condition="'$(EffectiveAnalysisLevelStyle)' == '' And
                                                              '$(AnalysisLevelPrefixStyle)' != ''">$(AnalysisLevelPrefixStyle)</EffectiveAnalysisLevelStyle>
                          <EffectiveAnalysisLevelStyle Condition="'$(EffectiveAnalysisLevelStyle)' == '' And
                                                              '$(AnalysisLevelStyle)' != ''">$(AnalysisLevelStyle)</EffectiveAnalysisLevelStyle>

                          <!-- Set the default analysis mode, if not set by the user -->
                          <_GlobalAnalyzerConfigAnalysisMode_MicrosoftCodeAnalysis{language}CodeStyle>$(AnalysisLevelSuffixStyle)</_GlobalAnalyzerConfigAnalysisMode_MicrosoftCodeAnalysis{language}CodeStyle>
                          <_GlobalAnalyzerConfigAnalysisMode_MicrosoftCodeAnalysis{language}CodeStyle Condition="'$(_GlobalAnalyzerConfigAnalysisMode_MicrosoftCodeAnalysis{language}CodeStyle)' == ''">$(AnalysisModeStyle)</_GlobalAnalyzerConfigAnalysisMode_MicrosoftCodeAnalysis{language}CodeStyle>
                          <_GlobalAnalyzerConfigAnalysisMode_MicrosoftCodeAnalysis{language}CodeStyle Condition="'$(_GlobalAnalyzerConfigAnalysisMode_MicrosoftCodeAnalysis{language}CodeStyle)' == 'AllEnabledByDefault'">{nameof(AnalysisMode.All)}</_GlobalAnalyzerConfigAnalysisMode_MicrosoftCodeAnalysis{language}CodeStyle>
                          <_GlobalAnalyzerConfigAnalysisMode_MicrosoftCodeAnalysis{language}CodeStyle Condition="'$(_GlobalAnalyzerConfigAnalysisMode_MicrosoftCodeAnalysis{language}CodeStyle)' == 'AllDisabledByDefault'">{nameof(AnalysisMode.None)}</_GlobalAnalyzerConfigAnalysisMode_MicrosoftCodeAnalysis{language}CodeStyle>
                          <_GlobalAnalyzerConfigAnalysisMode_MicrosoftCodeAnalysis{language}CodeStyle Condition="'$(_GlobalAnalyzerConfigAnalysisMode_MicrosoftCodeAnalysis{language}CodeStyle)' == ''">{nameof(AnalysisMode.Default)}</_GlobalAnalyzerConfigAnalysisMode_MicrosoftCodeAnalysis{language}CodeStyle>

                          <_GlobalAnalyzerConfigFileName_MicrosoftCodeAnalysis{language}CodeStyle>AnalysisLevelStyle_$(_GlobalAnalyzerConfigAnalysisMode_MicrosoftCodeAnalysis{language}CodeStyle).globalconfig</_GlobalAnalyzerConfigFileName_MicrosoftCodeAnalysis{language}CodeStyle>
                          <_GlobalAnalyzerConfigFileName_MicrosoftCodeAnalysis{language}CodeStyle>$(_GlobalAnalyzerConfigFileName_MicrosoftCodeAnalysis{language}CodeStyle.ToLowerInvariant())</_GlobalAnalyzerConfigFileName_MicrosoftCodeAnalysis{language}CodeStyle>

                          <_GlobalAnalyzerConfigDir_MicrosoftCodeAnalysis{language}CodeStyle Condition="'$(_GlobalAnalyzerConfigDir_MicrosoftCodeAnalysis{language}CodeStyle)' == ''">$(MSBuildThisFileDirectory)config</_GlobalAnalyzerConfigDir_MicrosoftCodeAnalysis{language}CodeStyle>
                          <_GlobalAnalyzerConfigFile_MicrosoftCodeAnalysis{language}CodeStyle Condition="'$(_GlobalAnalyzerConfigFileName_MicrosoftCodeAnalysis{language}CodeStyle)' != ''">$(_GlobalAnalyzerConfigDir_MicrosoftCodeAnalysis{language}CodeStyle)\$(_GlobalAnalyzerConfigFileName_MicrosoftCodeAnalysis{language}CodeStyle)</_GlobalAnalyzerConfigFile_MicrosoftCodeAnalysis{language}CodeStyle>
                        </PropertyGroup>

                        <!-- From .NET 9, the global config is systematically added if the file exists. Please check https://github.com/dotnet/roslyn/pull/71173 for more info. -->
                        <ItemGroup Condition="Exists('$(_GlobalAnalyzerConfigFile_MicrosoftCodeAnalysis{language}CodeStyle)') and
                                               ('$(AnalysisLevelStyle)' != '$(AnalysisLevel)' or '$(AnalysisModeStyle)' != '$(AnalysisMode)' or ('$(EffectiveAnalysisLevelStyle)' != '' and $([MSBuild]::VersionGreaterThanOrEquals('$(EffectiveAnalysisLevelStyle)', '9.0'))))">
                          <EditorConfigFiles Include="$(_GlobalAnalyzerConfigFile_MicrosoftCodeAnalysis{language}CodeStyle)" />
                        </ItemGroup>

                        <!-- Pass the MSBuild property value for 'EffectiveAnalysisLevelStyle' to the analyzers via analyzer config options. -->
                        <ItemGroup>
                          <CompilerVisibleProperty Include="EffectiveAnalysisLevelStyle" />
                        </ItemGroup>
                      </Target>

                    """;
            }
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

            public void AddDependencyLocation(string fullPath) { }

            public Assembly LoadFromPath(string fullPath) => Assembly.LoadFrom(fullPath);
        }
    }
}
