// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

// NOTE(DustinCa): The EditorFactory registration is in VisualStudioComponents\CSharpPackageRegistration.pkgdef.
// The reason for this is because the ProvideEditorLogicalView does not allow a name value to specified in addition to
// its GUID. This name value is used to identify untrusted logical views and link them to their physical view attributes.
// The net result is that using the attributes only causes designers to be loaded in the preview tab, even when they
// shouldn't be.

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [Guid(Guids.CSharpPackageIdString)]
    internal class CSharpPackage : AbstractPackage<CSharpPackage, CSharpLanguageService>, IVsUserSettingsQuery
    {
        private ObjectBrowserLibraryManager _libraryManager;
        private uint _libraryManagerCookie;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            try
            {
                await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);

                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                this.RegisterService<ICSharpTempPECompilerService>(async ct =>
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync(ct);
                    return new TempPECompilerService(this.Workspace.Services.GetService<IMetadataService>());
                });
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
            }
        }

        protected override VisualStudioWorkspaceImpl CreateWorkspace()
        {
            return this.ComponentModel.GetService<VisualStudioWorkspaceImpl>();
        }

        protected override async Task RegisterObjectBrowserLibraryManagerAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (await GetServiceAsync(typeof(SVsObjectManager)).ConfigureAwait(true) is IVsObjectManager2 objectManager)
            {
                _libraryManager = new ObjectBrowserLibraryManager(this, ComponentModel, Workspace);

                if (ErrorHandler.Failed(objectManager.RegisterSimpleLibrary(_libraryManager, out _libraryManagerCookie)))
                {
                    _libraryManagerCookie = 0;
                }
            }
        }

        protected override async Task UnregisterObjectBrowserLibraryManagerAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (_libraryManagerCookie != 0)
            {
                if (await GetServiceAsync(typeof(SVsObjectManager)).ConfigureAwait(true) is IVsObjectManager2 objectManager)
                {
                    objectManager.UnregisterLibrary(_libraryManagerCookie);
                    _libraryManagerCookie = 0;
                }

                _libraryManager.Dispose();
                _libraryManager = null;
            }
        }

        int IVsUserSettingsQuery.NeedExport(string pageID, out int needExport)
        {
            // We need to override MPF's definition of NeedExport since it doesn't know about our automation object
            needExport = (pageID == "TextEditor.CSharp-Specific") ? 1 : 0;

            return VSConstants.S_OK;
        }

        protected override object GetAutomationObject(string name)
        {
            if (name == "CSharp-Specific")
            {
                var workspace = this.ComponentModel.GetService<VisualStudioWorkspace>();
                return new Options.AutomationObject(workspace);
            }

            return base.GetAutomationObject(name);
        }

        protected override IEnumerable<IVsEditorFactory> CreateEditorFactories()
        {
            var editorFactory = new CSharpEditorFactory(this.ComponentModel);
            var codePageEditorFactory = new CSharpCodePageEditorFactory(editorFactory);

            return new IVsEditorFactory[] { editorFactory, codePageEditorFactory };
        }

        protected override CSharpLanguageService CreateLanguageService()
        {
            return new CSharpLanguageService(this);
        }

        protected override void RegisterMiscellaneousFilesWorkspaceInformation(MiscellaneousFilesWorkspace miscellaneousFilesWorkspace)
        {
            miscellaneousFilesWorkspace.RegisterLanguage(
                Guids.CSharpLanguageServiceId,
                LanguageNames.CSharp,
                ".csx");
        }

        protected override string RoslynLanguageName
        {
            get
            {
                return LanguageNames.CSharp;
            }
        }
    }
}
