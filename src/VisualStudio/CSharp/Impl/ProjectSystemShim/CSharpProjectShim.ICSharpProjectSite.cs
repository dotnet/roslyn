// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    internal partial class CSharpProjectShim : ICSharpProjectSite
    {
        /// <summary>
        /// When the project property page calls GetValidStartupClasses on us, it assumes
        /// the strings passed to it are in the native C# language service's string table 
        /// and never frees them. To avoid leaking our strings, we allocate them on the 
        /// native heap for each call and keep the pointers here. On subsequent calls
        /// or on disposal, we free the old strings before allocating the new ones.
        /// </summary>
        private IntPtr[]? _startupClasses = null;

        public void GetCompiler(out ICSCompiler compiler, out ICSInputSet inputSet)
        {
            compiler = this;
            inputSet = this;
        }

        public bool CheckInputFileTimes(System.Runtime.InteropServices.ComTypes.FILETIME output)
            => throw new NotImplementedException();

        public void BuildProject(object progress)
            => throw new NotImplementedException();

        public void Unused()
            => throw new NotImplementedException();

        public void OnSourceFileAdded(string filename)
        {
            // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
            //var sourceCodeKind = extension.Equals(".csx", StringComparison.OrdinalIgnoreCase)
            //    ? SourceCodeKind.Script
            //    : SourceCodeKind.Regular;
            AddFile(filename, SourceCodeKind.Regular);
        }

        public void OnSourceFileRemoved(string filename)
            => RemoveFile(filename);

        public int OnResourceFileAdded(string filename, string resourceName, bool embedded)
            => VSConstants.S_OK;

        public int OnResourceFileRemoved(string filename)
            => VSConstants.S_OK;

        public int OnImportAdded(string filename, string project)
        {
            // OnImportAdded is superseded by OnImportAddedEx. We maintain back-compat by treating
            // it as a non-NoPIA reference.
            return OnImportAddedEx(filename, project, CompilerOptions.OPTID_IMPORTS);
        }

        public int OnImportAddedEx(string filename, string project, CompilerOptions optionID)
        {
            if (optionID is not CompilerOptions.OPTID_IMPORTS and not CompilerOptions.OPTID_IMPORTSUSINGNOPIA)
            {
                throw new ArgumentException("optionID was an unexpected value.", nameof(optionID));
            }

            var embedInteropTypes = optionID == CompilerOptions.OPTID_IMPORTSUSINGNOPIA;
            ProjectSystemProject.AddMetadataReference(filename, new MetadataReferenceProperties(embedInteropTypes: embedInteropTypes));

            return VSConstants.S_OK;
        }

        public void OnImportRemoved(string filename, string project)
        {
            filename = FileUtilities.NormalizeAbsolutePath(filename);

            ProjectSystemProject.RemoveMetadataReference(filename, properties: ProjectSystemProject.GetPropertiesForMetadataReference(filename).Single());
        }

        public void OnOutputFileChanged(string filename)
        {
            // We have nothing to do here
        }

        public void OnActiveConfigurationChanged(string configName)
        {
            // We have nothing to do here
        }

        public void OnProjectLoadCompletion()
        {
            // Despite the name, this is not necessarily called when the project has actually been
            // completely loaded. If you plan on using this, be careful!
        }

        public int CreateCodeModel(object parent, out EnvDTE.CodeModel codeModel)
        {
            codeModel = ProjectCodeModel.GetOrCreateRootCodeModel((EnvDTE.Project)parent);
            return VSConstants.S_OK;
        }

        public int CreateFileCodeModel(string fileName, object parent, out EnvDTE.FileCodeModel ppFileCodeModel)
        {
            ppFileCodeModel = ProjectCodeModel.GetOrCreateFileCodeModel(fileName, parent);
            return VSConstants.S_OK;
        }

        public void OnModuleAdded(string filename)
            => throw new NotImplementedException();

        public void OnModuleRemoved(string filename)
            => throw new NotImplementedException();

        public int GetValidStartupClasses(IntPtr[] classNames, ref int count)
        {
            var project = Workspace.CurrentSolution.GetRequiredProject(ProjectSystemProject.Id);
            var compilation = project.GetRequiredCompilationAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            var entryPoints = EntryPointFinder.FindEntryPoints(compilation.SourceModule.GlobalNamespace);

            // If classNames is NULL, then we need to populate the number of valid startup
            // classes only
            if (classNames == null)
            {
                count = entryPoints.Count();
                return VSConstants.S_OK;
            }
            else
            {
                // We return S_FALSE if we have more entrypoints than places in the array.
                var entryPointNames = entryPoints.Select(e => e.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))).ToArray();

                if (entryPointNames.Length > classNames.Length)
                {
                    return VSConstants.S_FALSE;
                }

                // The old language service stored startup class names in its string table,
                // so the property page never freed them. To avoid leaking memory, we're 
                // going to allocate our strings on the native heap and keep the pointers to them.
                // Subsequent calls to this function will free the old strings and allocate the 
                // new ones. The last set of marshalled strings is freed in the destructor.
                if (_startupClasses != null)
                {
                    foreach (var @class in _startupClasses)
                    {
                        Marshal.FreeHGlobal(@class);
                    }
                }

                _startupClasses = entryPointNames.Select(Marshal.StringToHGlobalUni).ToArray();
                Array.Copy(_startupClasses, classNames, _startupClasses.Length);

                count = entryPointNames.Length;
                return VSConstants.S_OK;
            }
        }

        public void OnAliasesChanged(string file, string project, int previousAliasesCount, string[] previousAliases, int currentAliasesCount, string[] currentAliases)
        {
            using (ProjectSystemProject.CreateBatchScope())
            {
                var existingProperties = ProjectSystemProject.GetPropertiesForMetadataReference(file).Single();
                ProjectSystemProject.RemoveMetadataReference(file, existingProperties);
                ProjectSystemProject.AddMetadataReference(file, existingProperties.WithAliases(currentAliases));
            }
        }
    }
}
