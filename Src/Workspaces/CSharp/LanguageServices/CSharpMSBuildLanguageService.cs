using System;
using System.ComponentModel.Composition;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Host;
using Roslyn.Services.MSBuild;
using Roslyn.Utilities;
using MSB = Microsoft.Build.Evaluation;

namespace Roslyn.Services.CSharp
{
    [ExportMSBuildLanguageService(typeof(IMSBuildLanguageService), LanguageNames.CSharp, projectType: "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC", projectFileExtension: "csproj")]
    internal partial class CSharpMSBuildLanguageService : AbstractMSBuildLanguageService
    {
        private static readonly ParseOptions defaultParseOptions = new ParseOptions(languageVersion: LanguageVersion.CSharp6);

        [ImportingConstructor]
        internal CSharpMSBuildLanguageService()
        {
        }

        public override ICompilationOptions GetCompilationOptions(MSB.Project project)
        {
            var options = CompilationOptions.Default;

            var outputType = project.GetPropertyValue("OutputType");
            switch (outputType)
            {
                case "Library":
                    options = options.Copy(assemblyKind: AssemblyKind.DynamicallyLinkedLibrary);
                    break;
                case "Exe":
                    options = options.Copy(assemblyKind: AssemblyKind.ConsoleApplication);
                    break;
                case "WinExe":
                    options = options.Copy(assemblyKind: AssemblyKind.WindowsApplication);
                    break;
            }

            var startupObjectProperty = project.GetProperty("StartupObject");
            if (startupObjectProperty != null)
            {
                var startupObject = startupObjectProperty.EvaluatedValue;
                if (!string.IsNullOrEmpty(startupObject) && options.MainTypeName != startupObject)
                {
                    options = options.Copy(mainTypeName: startupObject);
                }
            }

            var checkForOverflowProperty = project.GetProperty("CheckForOverflowUnderflow");
            if (checkForOverflowProperty != null)
            {
                bool checkForOverflow;
                if (bool.TryParse(checkForOverflowProperty.EvaluatedValue, out checkForOverflow) && options.CheckOverflow != checkForOverflow)
                {
                    options = options.Copy(checkOverflow: checkForOverflow);
                }
            }

            var optimizeProperty = project.GetProperty("Optimize");
            if (optimizeProperty != null)
            {
                bool optimize;
                if (bool.TryParse(optimizeProperty.EvaluatedValue, out optimize) && options.Optimize != optimize)
                {
                    options = options.Copy(optimize: optimize);
                }
            }

            return options;
        }

        public override SourceCodeKind GetSourceCodeKind(string documentFileName)
        {
            if (documentFileName.EndsWith(".csx"))
            {
                return SourceCodeKind.Script;
            }

            return SourceCodeKind.Regular;
        }

        public override string GetDocumentExtension(SourceCodeKind sourceCodeKind)
        {
            switch (sourceCodeKind)
            {
                case SourceCodeKind.Script:
                    return ".csx";
                default:
                    return ".cs";
            }
        }

        public override IParseOptions GetParseOptions(MSB.Project project)
        {
            return GetParseOptions(project, defaultParseOptions);
        }

        private ParseOptions GetParseOptions(MSB.Project project, ParseOptions options)
        {
            var languageVersionProperty = project.GetProperty("LangVersion");
            if (languageVersionProperty != null)
            {
                var languageVersionText = languageVersionProperty.EvaluatedValue;
                var version = GetLanguageVersion(languageVersionText);
                if (options.LanguageVersion != version)
                {
                    options = options.Copy(languageVersion: version);
                }

                var mode = GetCompatibilityMode(languageVersionText);
                if (options.Compatibility != mode)
                {
                    options = options.Copy(compatibility: mode);
                }
            }

            var defineConstantsProperty = project.GetProperty("DefineConstants");
            if (defineConstantsProperty != null)
            {
                options = (ParseOptions)options.SetOption("PreprocessorSymbols", defineConstantsProperty.EvaluatedValue);
            }

            var documentationFileProperty = project.GetProperty("DocumentationFile");
            var suppress = (documentationFileProperty == null) || string.IsNullOrEmpty(documentationFileProperty.EvaluatedValue);
            if (suppress != options.SuppressDocumentationCommentParse)
            {
                options = options.Copy(suppressDocumentationCommentParse: suppress);
            }

            return options;
        }

        private CompatibilityMode GetCompatibilityMode(string projectLanguageVersion)
        {
            switch (projectLanguageVersion)
            {
                case "ISO-1":
                    return CompatibilityMode.ECMA1;
                case "ISO-2":
                    return CompatibilityMode.ECMA2;
                default:
                    return CompatibilityMode.None;
            }
        }

        private LanguageVersion GetLanguageVersion(string projectLanguageVersion)
        {
            switch (projectLanguageVersion)
            {
                case "ISO-1":
                    return LanguageVersion.CSharp1;
                case "ISO-2":
                    return LanguageVersion.CSharp2;
                default:
                    if (!string.IsNullOrEmpty(projectLanguageVersion))
                    {
                        int version;
                        if (int.TryParse(projectLanguageVersion, out version))
                        {
                            return (LanguageVersion)version;
                        }
                    }

                    // use default;
                    return defaultParseOptions.LanguageVersion;
            }
        }
    }
}