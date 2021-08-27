// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Setup
{
    [Guid(Guids.RoslynUserOptionsPackageIdString)]
    internal sealed class RoslynUserOptionsPackage : AsyncPackage
    {
        // The randomly-generated key name is used for serializing the Background Analysis Scope preference to the .SUO
        // file. It doesn't have any semantic meaning, but is intended to not conflict with any other extension that
        // might be saving an "AnalysisScope" named stream to the same file.
        // note: must be <= 31 characters long
        private const string BackgroundAnalysisScopeOptionKey = "AnalysisScope-DCE33A29A768";
        private const byte BackgroundAnalysisScopeOptionVersion = 1;

        private static RoslynUserOptionsPackage? _lazyInstance;

        private BackgroundAnalysisScope? _analysisScope;

        public RoslynUserOptionsPackage()
        {
            // We need to register an option in order for OnLoadOptions/OnSaveOptions to be called
            AddOptionKey(BackgroundAnalysisScopeOptionKey);
        }

        public BackgroundAnalysisScope? AnalysisScope
        {
            get
            {
                return _analysisScope;
            }

            set
            {
                if (_analysisScope == value)
                    return;

                _analysisScope = value;
                AnalysisScopeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? AnalysisScopeChanged;

        internal static async ValueTask<RoslynUserOptionsPackage?> GetOrLoadAsync(IThreadingContext threadingContext, IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            if (_lazyInstance is null)
            {
                await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var shell = (IVsShell7?)await serviceProvider.GetServiceAsync(typeof(SVsShell)).ConfigureAwait(true);
                Assumes.Present(shell);
                await shell.LoadPackageAsync(typeof(RoslynUserOptionsPackage).GUID);

                if (ErrorHandler.Succeeded(((IVsShell)shell).IsPackageLoaded(typeof(RoslynUserOptionsPackage).GUID, out var package)))
                {
                    _lazyInstance = (RoslynUserOptionsPackage)package;
                }
            }

            return _lazyInstance;
        }

        protected override void OnLoadOptions(string key, Stream stream)
        {
            if (key == BackgroundAnalysisScopeOptionKey)
            {
                if (stream.ReadByte() == BackgroundAnalysisScopeOptionVersion)
                {
                    var hasValue = stream.ReadByte() == 1;
                    AnalysisScope = hasValue ? (BackgroundAnalysisScope)stream.ReadByte() : null;
                }
                else
                {
                    AnalysisScope = null;
                }
            }

            base.OnLoadOptions(key, stream);
        }

        protected override void OnSaveOptions(string key, Stream stream)
        {
            if (key == BackgroundAnalysisScopeOptionKey)
            {
                stream.WriteByte(BackgroundAnalysisScopeOptionVersion);
                stream.WriteByte(AnalysisScope.HasValue ? (byte)1 : (byte)0);
                stream.WriteByte((byte)AnalysisScope.GetValueOrDefault());
            }

            base.OnSaveOptions(key, stream);
        }
    }
}
