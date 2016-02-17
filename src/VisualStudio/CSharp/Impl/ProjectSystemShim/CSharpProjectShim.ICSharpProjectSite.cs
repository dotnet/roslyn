// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
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
        private IntPtr[] _startupClasses = null;

        public void GetCompiler(out ICSCompiler compiler, out ICSInputSet inputSet)
        {
            compiler = this;
            inputSet = this;
        }

        public bool CheckInputFileTimes(System.Runtime.InteropServices.ComTypes.FILETIME output)
        {
            throw new NotImplementedException();
        }

        public void BuildProject(object progress)
        {
            throw new NotImplementedException();
        }

        public void Unused()
        {
            throw new NotImplementedException();
        }

        public void OnSourceFileAdded(string filename)
        {
            var extension = Path.GetExtension(filename);

            // The Workflow MSBuild targets and CompileWorkflowTask choose to pass the .xoml files to the language
            // service as if they were actual C# files. We should just ignore them.
            if (extension.Equals(".xoml", StringComparison.OrdinalIgnoreCase))
            {
                AddUntrackedFile(filename);
                return;
            }

            // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
            //var sourceCodeKind = extension.Equals(".csx", StringComparison.OrdinalIgnoreCase)
            //    ? SourceCodeKind.Script
            //    : SourceCodeKind.Regular;
            var sourceCodeKind = SourceCodeKind.Regular;

            IVsHierarchy foundHierarchy;
            uint itemId;
            if (ErrorHandler.Succeeded(_projectRoot.GetHierarchyAndItemID(filename, out foundHierarchy, out itemId)))
            {
                Debug.Assert(foundHierarchy == this.Hierarchy);
            }
            else
            {
                // Unfortunately, the project system does pass us some files which aren't part of
                // the project as far as the hierarchy and itemid are concerned.  We'll just used
                // VSITEMID.Nil for them.

                foundHierarchy = null;
                itemId = (uint)VSConstants.VSITEMID.Nil;
            }

            AddFile(filename, sourceCodeKind, itemId, CanUseTextBuffer);
        }

        public void OnSourceFileRemoved(string filename)
        {
            RemoveFile(filename);
        }

        public int OnResourceFileAdded(string filename, string resourceName, bool embedded)
        {
            return VSConstants.S_OK;
        }

        public int OnResourceFileRemoved(string filename)
        {
            return VSConstants.S_OK;
        }

        public int OnImportAdded(string filename, string project)
        {
            // OnImportAdded is superseded by OnImportAddedEx. We maintain back-compat by treating
            // it as a non-NoPIA reference.
            return OnImportAddedEx(filename, project, CompilerOptions.OPTID_IMPORTS);
        }

        public int OnImportAddedEx(string filename, string project, CompilerOptions optionID)
        {
            filename = FileUtilities.NormalizeAbsolutePath(filename);

            if (optionID != CompilerOptions.OPTID_IMPORTS && optionID != CompilerOptions.OPTID_IMPORTSUSINGNOPIA)
            {
                throw new ArgumentException("optionID was an unexpected value.", "optionID");
            }

            bool embedInteropTypes = optionID == CompilerOptions.OPTID_IMPORTSUSINGNOPIA;
            var properties = new MetadataReferenceProperties(embedInteropTypes: embedInteropTypes);

            return AddMetadataReferenceAndTryConvertingToProjectReferenceIfPossible(filename, properties);
        }

        public void OnImportRemoved(string filename, string project)
        {
            filename = FileUtilities.NormalizeAbsolutePath(filename);

            RemoveMetadataReference(filename);
        }

        public void OnOutputFileChanged(string filename)
        {
            // We have nothing to do here (yet)
        }

        public void OnActiveConfigurationChanged(string configName)
        {
            // We have nothing to do here (yet)
        }

        public void OnProjectLoadCompletion()
        {
            // Despite the name, this is not necessarily called when the project has actually been
            // completely loaded. If you plan on using this, be careful!
        }

        protected virtual bool CanUseTextBuffer(ITextBuffer textBuffer)
        {
            return true;
        }

        public abstract int CreateCodeModel(object parent, out EnvDTE.CodeModel codeModel);
        public abstract int CreateFileCodeModel(string fileName, object parent, out EnvDTE.FileCodeModel ppFileCodeModel);

        public void OnModuleAdded(string filename)
        {
            throw new NotImplementedException();
        }

        public void OnModuleRemoved(string filename)
        {
            throw new NotImplementedException();
        }

        public int GetValidStartupClasses(IntPtr[] classNames, ref int count)
        {
            // If classNames is NULL, then we need to populate the number of valid startup
            // classes only
            var project = VisualStudioWorkspace.CurrentSolution.GetProject(Id);
            var compilation = project.GetCompilationAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);

            var entryPoints = GetEntryPoints(project, compilation);

            if (classNames == null)
            {
                count = entryPoints.Count();
                return VSConstants.S_OK;
            }
            else
            {
                // We return s_false if we have more entrypoints than places in the array.
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

        private IEnumerable<INamedTypeSymbol> GetEntryPoints(Project project, Compilation compilation)
        {
            return EntryPointFinder.FindEntryPoints(compilation.Assembly.GlobalNamespace);
        }

        public void OnAliasesChanged(string file, string project, int previousAliasesCount, string[] previousAliases, int currentAliasesCount, string[] currentAliases)
        {
            UpdateMetadataReferenceAliases(file, ImmutableArray.CreateRange(currentAliases));
        }
    }
}
