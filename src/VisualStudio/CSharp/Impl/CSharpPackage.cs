// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

// NOTE(DustinCa): The EditorFactory registration is in VisualStudioComponents\CSharpPackageRegistration.pkgdef.
// The reason for this is because the ProvideEditorLogicalView does not allow a name value to specified in addition to
// its GUID. This name value is used to identify untrusted logical views and link them to their physical view attributes.
// The net result is that using the attributes only causes designers to be loaded in the preview tab, even when they
// shouldn't be.

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    // TODO(DustinCa): Put all of this in CSharpPackageRegistration.pkgdef rather than using attributes
    // (See vsproject\cool\coolpkg\pkg\VCSharp_Proj_System_Reg.pkgdef for an example).
    [Guid(Guids.CSharpPackageIdString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideLanguageExtension(typeof(CSharpLanguageService), ".cs")]
    [ProvideLanguageService(Guids.CSharpLanguageServiceIdString, "CSharp", languageResourceID: 101, RequestStockColors = true, ShowDropDownOptions = true)]
    [ProvideLanguageEditorToolsOptionCategory("CSharp", "Formatting", "#107")]
    [ProvideLanguageEditorOptionPage(typeof(Options.AdvancedOptionPage), "CSharp", null, "Advanced", pageNameResourceId: "#102", keywordListResourceId: 306)]
    [ProvideLanguageEditorOptionPage(typeof(Options.Formatting.FormattingStylePage), "CSharp", null, @"Code Style", pageNameResourceId: "#114", keywordListResourceId: 313)]
    [ProvideLanguageEditorOptionPage(typeof(Options.IntelliSenseOptionPage), "CSharp", null, "IntelliSense", pageNameResourceId: "#103", keywordListResourceId: 312)]
    [ProvideLanguageEditorOptionPage(typeof(Options.Formatting.FormattingOptionPage), "CSharp", "Formatting", "General", pageNameResourceId: "#108", keywordListResourceId: 307)]
    [ProvideLanguageEditorOptionPage(typeof(Options.Formatting.FormattingIndentationOptionPage), "CSharp", "Formatting", "Indentation", pageNameResourceId: "#109", keywordListResourceId: 308)]
    [ProvideLanguageEditorOptionPage(typeof(Options.Formatting.FormattingWrappingPage), "CSharp", "Formatting", "Wrapping", pageNameResourceId: "#110", keywordListResourceId: 311)]
    [ProvideLanguageEditorOptionPage(typeof(Options.Formatting.FormattingNewLinesPage), "CSharp", "Formatting", "NewLines", pageNameResourceId: "#111", keywordListResourceId: 309)]
    [ProvideLanguageEditorOptionPage(typeof(Options.Formatting.FormattingSpacingPage), "CSharp", "Formatting", "Spacing", pageNameResourceId: "#112", keywordListResourceId: 310)]
    [ProvideAutomationProperties("TextEditor", "CSharp", Guids.TextManagerPackageString, profileNodeLabelId: 101, profileNodeDescriptionId: 106, resourcePackageGuid: Guids.CSharpPackageIdString)]
    [ProvideAutomationProperties("TextEditor", "CSharp-Specific", packageGuid: Guids.CSharpPackageIdString, profileNodeLabelId: 104, profileNodeDescriptionId: 105)]
    [ProvideService(typeof(CSharpLanguageService), ServiceName = "C# Language Service")]
    [ProvideService(typeof(ICSharpTempPECompilerService), ServiceName = "C# TempPE Compiler Service")]
    internal class CSharpPackage : AbstractPackage<CSharpPackage, CSharpLanguageService, CSharpProjectShim>, IVsUserSettingsQuery
    {
        private ObjectBrowserLibraryManager _libraryManager;
        private uint _libraryManagerCookie;

        protected override void Initialize()
        {
            try
            {
                base.Initialize();

                this.RegisterService<ICSharpTempPECompilerService>(() => new TempPECompilerService(this.Workspace));

                RegisterObjectBrowserLibraryManager();
            }
            catch (Exception e) when (FatalError.Report(e))
            {
            }
        }

        protected override VisualStudioWorkspaceImpl CreateWorkspace()
        {
            return this.ComponentModel.GetService<VisualStudioWorkspaceImpl>();
        }

        protected override void Dispose(bool disposing)
        {
            UnregisterObjectBrowserLibraryManager();

            base.Dispose(disposing);
        }

        private void RegisterObjectBrowserLibraryManager()
        {
            var objectManager = this.GetService(typeof(SVsObjectManager)) as IVsObjectManager2;
            if (objectManager != null)
            {
                _libraryManager = new ObjectBrowserLibraryManager(this);

                if (ErrorHandler.Failed(objectManager.RegisterSimpleLibrary(_libraryManager, out _libraryManagerCookie)))
                {
                    _libraryManagerCookie = 0;
                }
            }
        }

        private void UnregisterObjectBrowserLibraryManager()
        {
            if (_libraryManagerCookie != 0)
            {
                var objectManager = this.GetService(typeof(SVsObjectManager)) as IVsObjectManager2;
                if (objectManager != null)
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
                var optionService = workspace.Services.GetService<IOptionService>();
                return new Options.AutomationObject(optionService);
            }

            return base.GetAutomationObject(name);
        }

        protected override IEnumerable<IVsEditorFactory> CreateEditorFactories()
        {
            var editorFactory = new CSharpEditorFactory(this);
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
                ".csx",
                CSharpParseOptions.Default);
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
