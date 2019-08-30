// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim
{
    internal static class CSharpHelpers
    {
        public static CSharpProjectShim CreateCSharpProject(TestEnvironment environment, string projectName)
        {
            var projectBinPath = Path.GetTempPath();
            var hierarchy = environment.CreateHierarchy(projectName, projectBinPath, projectRefPath: null, projectCapabilities: "CSharp");

            return CreateCSharpProject(environment, projectName, hierarchy);
        }

        public static CSharpProjectShim CreateCSharpProject(TestEnvironment environment, string projectName, IVsHierarchy hierarchy)
        {
            return new CSharpProjectShim(
                new MockCSharpProjectRoot(hierarchy),
                projectSystemName: projectName,
                hierarchy: hierarchy,
                serviceProvider: environment.ServiceProvider,
                threadingContext: environment.ThreadingContext,
                hostDiagnosticUpdateSourceOpt: null,
                commandLineParserServiceOpt: new CSharpCommandLineParserService());
        }

        public static CPSProject CreateCSharpCPSProject(TestEnvironment environment, string projectName, params string[] commandLineArguments)
        {
            return CreateCSharpCPSProject(environment, projectName, projectGuid: Guid.NewGuid(), commandLineArguments: commandLineArguments);
        }

        public static CPSProject CreateCSharpCPSProject(TestEnvironment environment, string projectName, Guid projectGuid, params string[] commandLineArguments)
        {
            var projectFilePath = Path.GetTempPath();
            var binOutputPath = GetOutputPathFromArguments(commandLineArguments) ?? Path.Combine(projectFilePath, projectName + ".dll");

            return CreateCSharpCPSProject(environment, projectName, projectFilePath, binOutputPath, projectGuid, commandLineArguments);
        }

        public static CPSProject CreateCSharpCPSProject(TestEnvironment environment, string projectName, string binOutputPath, params string[] commandLineArguments)
        {
            var projectFilePath = Path.GetTempPath();
            return CreateCSharpCPSProject(environment, projectName, projectFilePath, binOutputPath, projectGuid: Guid.NewGuid(), commandLineArguments: commandLineArguments);
        }

        public static unsafe void SetOption(this CSharpProjectShim csharpProject, CompilerOptions optionID, object value)
        {
            Assert.Equal(8 + 2 * IntPtr.Size, sizeof(HACK_VariantStructure));
            Assert.Equal(8, (int)Marshal.OffsetOf<HACK_VariantStructure>("_booleanValue"));

            HACK_VariantStructure variant = default;
            Marshal.GetNativeVariantForObject(value, (IntPtr)(&variant));
            csharpProject.SetOption(optionID, variant);
        }

        public static CPSProject CreateCSharpCPSProject(TestEnvironment environment, string projectName, string projectFilePath, string binOutputPath, Guid projectGuid, params string[] commandLineArguments)
        {
            var hierarchy = environment.CreateHierarchy(projectName, binOutputPath, projectRefPath: null, "CSharp");
            var cpsProjectFactory = environment.ExportProvider.GetExportedValue<IWorkspaceProjectContextFactory>();
            var cpsProject = (CPSProject)cpsProjectFactory.CreateProjectContext(
                LanguageNames.CSharp,
                projectName,
                projectFilePath,
                projectGuid,
                hierarchy,
                binOutputPath);

            var commandLineForOptions = string.Join(" ", commandLineArguments);
            cpsProject.SetOptions(commandLineForOptions);

            return cpsProject;
        }

        public static CPSProject CreateNonCompilableProject(TestEnvironment environment, string projectName, string projectFilePath)
        {
            var hierarchy = environment.CreateHierarchy(projectName, projectBinPath: null, projectRefPath: null, "");
            var cpsProjectFactory = environment.ExportProvider.GetExportedValue<IWorkspaceProjectContextFactory>();

            return (CPSProject)cpsProjectFactory.CreateProjectContext(
                NoCompilationConstants.LanguageName,
                projectName,
                projectFilePath,
                Guid.NewGuid(),
                hierarchy,
                binOutputPath: null);
        }

        private static string GetOutputPathFromArguments(string[] commandLineArguments)
        {
            const string outPrefix = "/out:";
            string outputPath = null;
            foreach (var arg in commandLineArguments)
            {
                var index = arg.IndexOf(outPrefix);
                if (index >= 0)
                {
                    outputPath = arg.Substring(index + outPrefix.Length);
                }
            }

            return outputPath;
        }

        private sealed class TestCSharpCommandLineParserService : ICommandLineParserService
        {
            public CommandLineArguments Parse(IEnumerable<string> arguments, string baseDirectory, bool isInteractive, string sdkDirectory)
            {
                if (baseDirectory == null || !Directory.Exists(baseDirectory))
                {
                    baseDirectory = Path.GetTempPath();
                }

                return CSharpCommandLineParser.Default.Parse(arguments, baseDirectory, sdkDirectory);
            }
        }

        private class MockCSharpProjectRoot : ICSharpProjectRoot
        {
            private readonly IVsHierarchy _hierarchy;

            public MockCSharpProjectRoot(IVsHierarchy hierarchy)
            {
                _hierarchy = hierarchy;
            }

            int ICSharpProjectRoot.BelongsToProject(string pszFileName)
            {
                throw new NotImplementedException();
            }

            string ICSharpProjectRoot.BuildPerConfigCacheFileName()
            {
                throw new NotImplementedException();
            }

            bool ICSharpProjectRoot.CanCreateFileCodeModel(string pszFile)
            {
                throw new NotImplementedException();
            }

            void ICSharpProjectRoot.ConfigureCompiler(ICSCompiler compiler, ICSInputSet inputSet, bool addSources)
            {
                throw new NotImplementedException();
            }

            object ICSharpProjectRoot.CreateFileCodeModel(string pszFile, ref Guid riid)
            {
                throw new NotImplementedException();
            }

            string ICSharpProjectRoot.GetActiveConfigurationName()
            {
                throw new NotImplementedException();
            }

            string ICSharpProjectRoot.GetFullProjectName()
            {
                throw new NotImplementedException();
            }

            int ICSharpProjectRoot.GetHierarchyAndItemID(string pszFile, out IVsHierarchy ppHier, out uint pItemID)
            {
                ppHier = _hierarchy;

                // Each item should have it's own ItemID, but for simplicity we'll just hard-code a value of
                // no particular significance.
                pItemID = 42;

                return VSConstants.S_OK;
            }

            void ICSharpProjectRoot.GetHierarchyAndItemIDOptionallyInProject(string pszFile, out IVsHierarchy ppHier, out uint pItemID, bool mustBeInProject)
            {
                throw new NotImplementedException();
            }

            string ICSharpProjectRoot.GetProjectLocation()
            {
                throw new NotImplementedException();
            }

            object ICSharpProjectRoot.GetProjectSite(ref Guid riid)
            {
                throw new NotImplementedException();
            }

            void ICSharpProjectRoot.SetProjectSite(ICSharpProjectSite site)
            {
                throw new NotImplementedException();
            }
        }
    }
}
