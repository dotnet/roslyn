﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell.Interop;
using IVsAsyncFileChangeEx = Microsoft.VisualStudio.Shell.IVsAsyncFileChangeEx;
using SVsServiceProvider = Microsoft.VisualStudio.Shell.SVsServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Export(typeof(FileChangeWatcherProvider))]
    internal sealed class FileChangeWatcherProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FileChangeWatcherProvider(IThreadingContext threadingContext, [Import(typeof(SVsServiceProvider))] Shell.IAsyncServiceProvider serviceProvider)
        {
            // We do not want background work to implicitly block on the availability of the SVsFileChangeEx to avoid any deadlock risk,
            // since the first fetch for a file watcher might end up happening on the background.
            Watcher = new FileChangeWatcher(async () =>
            {
                await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, threadingContext.DisposalToken);

                var fileChangeService = (IVsAsyncFileChangeEx?)await serviceProvider.GetServiceAsync(typeof(SVsFileChangeEx)).ConfigureAwait(true);
                Assumes.Present(fileChangeService);

                return fileChangeService;
            });
        }

        public FileChangeWatcher Watcher { get; }
    }
}
