// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    // The option page configuration is duplicated in PackageRegistration.pkgdef.
    //
    // C# option pages tree:
    //   CSharp
    //     General (from editor)
    //     Scroll Bars (from editor)
    //     Tabs (from editor)
    //     Advanced
    //     Code Style (category)
    //       General
    //       Formatting (category)
    //         General
    //         Indentation
    //         New Lines
    //         Spacing
    //         Wrapping
    //       Naming
    //     IntelliSense

    [ProvideLanguageEditorOptionPage(typeof(Options.AdvancedOptionPage), "CSharp", null, "Advanced", pageNameResourceId: "#102", keywordListResourceId: 306)]
    [ProvideLanguageEditorToolsOptionCategory("CSharp", "Code Style", "#114")]
    [ProvideLanguageEditorOptionPage(typeof(Options.Formatting.CodeStylePage), "CSharp", @"Code Style", "General", pageNameResourceId: "#108", keywordListResourceId: 313)]
    [ProvideLanguageEditorToolsOptionCategory("CSharp", @"Code Style\Formatting", "#107")]
    [ProvideLanguageEditorOptionPage(typeof(Options.Formatting.FormattingOptionPage), "CSharp", @"Code Style\Formatting", "General", pageNameResourceId: "#108", keywordListResourceId: 307)]
    [ProvideLanguageEditorOptionPage(typeof(Options.Formatting.FormattingIndentationOptionPage), "CSharp", @"Code Style\Formatting", "Indentation", pageNameResourceId: "#109", keywordListResourceId: 308)]
    [ProvideLanguageEditorOptionPage(typeof(Options.Formatting.FormattingWrappingPage), "CSharp", @"Code Style\Formatting", "Wrapping", pageNameResourceId: "#110", keywordListResourceId: 311)]
    [ProvideLanguageEditorOptionPage(typeof(Options.Formatting.FormattingNewLinesPage), "CSharp", @"Code Style\Formatting", "NewLines", pageNameResourceId: "#111", keywordListResourceId: 309)]
    [ProvideLanguageEditorOptionPage(typeof(Options.Formatting.FormattingSpacingPage), "CSharp", @"Code Style\Formatting", "Spacing", pageNameResourceId: "#112", keywordListResourceId: 310)]
    [ProvideLanguageEditorOptionPage(typeof(Options.NamingStylesOptionPage), "CSharp", @"Code Style", "Naming", pageNameResourceId: "#115", keywordListResourceId: 314)]
    [ProvideLanguageEditorOptionPage(typeof(Options.IntelliSenseOptionPage), "CSharp", null, "IntelliSense", pageNameResourceId: "#103", keywordListResourceId: 312)]
    [ProvideSettingsManifest(PackageRelativeManifestFile = @"UnifiedSettings\csharpSettings.registration.json")]
    [Guid(Guids.CSharpPackageIdString)]
    internal sealed class CSharpPackage : AbstractPackage<CSharpPackage, CSharpLanguageService>, IVsUserSettingsQuery
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
                    var workspace = this.ComponentModel.GetService<VisualStudioWorkspace>();
                    await JoinableTaskFactory.SwitchToMainThreadAsync(ct);
                    return new TempPECompilerService(workspace.Services.GetService<IMetadataService>());
                });
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.General))
            {
            }
        }

        protected override async Task RegisterObjectBrowserLibraryManagerAsync(CancellationToken cancellationToken)
        {
            var workspace = this.ComponentModel.GetService<VisualStudioWorkspace>();

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (await GetServiceAsync(typeof(SVsObjectManager)).ConfigureAwait(true) is IVsObjectManager2 objectManager)
            {
                _libraryManager = new ObjectBrowserLibraryManager(this, ComponentModel, workspace);

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
                return new Options.AutomationObject(ComponentModel.GetService<ILegacyGlobalOptionService>());
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
            => new(this);

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
