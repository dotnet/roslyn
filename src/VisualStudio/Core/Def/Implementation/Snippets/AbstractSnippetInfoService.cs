// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    internal abstract class AbstractSnippetInfoService : ForegroundThreadAffinitizedObject, ISnippetInfoService, IVsExpansionEvents
    {
        private readonly Guid _languageGuidForSnippets;
        private readonly IVsExpansionManager _expansionManager;

        /// <summary>
        /// This service is created on the UI thread during package initialization, but it must not
        /// block the initialization process. Getting snippet information from the <see cref="IVsExpansionManager"/>
        /// must be done on the UI thread, so do this work in a task that will run on the UI thread
        /// with lower priority.
        /// </summary>
        protected readonly Task InitialCachePopulationTask;

        protected object cacheGuard = new object();

        // Initialize these to empty values. When returning from GetSnippetsIfAvailable and
        // SnippetShortcutExists_NonBlocking, we can return without checking the status
        // of InitialCachePopulationTask.
        protected IList<SnippetInfo> snippets = SpecializedCollections.EmptyList<SnippetInfo>();
        protected ISet<string> snippetShortcuts = SpecializedCollections.EmptySet<string>();

        public bool InsertSnippetCommandBound { get; private set; }

        public AbstractSnippetInfoService(
            Shell.SVsServiceProvider serviceProvider,
            Guid languageGuidForSnippets,
            IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _languageGuidForSnippets = languageGuidForSnippets;

            if (serviceProvider != null)
            {
                var textManager = (IVsTextManager2)serviceProvider.GetService(typeof(SVsTextManager));
                if (textManager.GetExpansionManager(out _expansionManager) == VSConstants.S_OK)
                {
                    ComEventSink.Advise<IVsExpansionEvents>(_expansionManager, this);
                }
            }

            IAsynchronousOperationListener waiter = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.Snippets);
            var token = waiter.BeginAsyncOperation(GetType().Name + ".Start");

            InitialCachePopulationTask = Task.Factory.StartNew(() => PopulateSnippetCaches(),
                CancellationToken.None,
                TaskCreationOptions.None,
                ForegroundTaskScheduler).CompletesAsyncOperation(token);
        }

        public int OnAfterSnippetsUpdate()
        {
            PopulateSnippetCaches();
            return VSConstants.S_OK;
        }

        public int OnAfterSnippetsKeyBindingChange([ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]uint dwCmdGuid, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]uint dwCmdId, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")]int fBound)
        {
            InsertSnippetCommandBound = fBound != 0;
            return VSConstants.S_OK;
        }

        public IEnumerable<SnippetInfo> GetSnippetsIfAvailable()
        {
            // This function used to be async and wait for the cache population task.
            // Since the cache population task must run on the UI thread, this could
            // deadlock if completion blocked on the UI thread before the 
            // population task started. We now simply return whatever snippets were
            // there. When we're told to update snippets, we'll return stale data
            // until that process is complete, which is fine.
            lock (cacheGuard)
            {
                return snippets;
            }
        }

        public bool SnippetShortcutExists_NonBlocking(string shortcut)
        {
            // This function used to be async and wait for the cache population task.
            // Since the cache population task must run on the UI thread, this could
            // deadlock if completion blocked on the UI thread before the 
            // population task started. We now simply return whatever snippets were
            // there. When we're told to update snippets, we'll return stale data
            // until that process is complete, which is fine.
            lock (cacheGuard)
            {
                return snippetShortcuts.Contains(shortcut);
            }
        }

        public virtual bool ShouldFormatSnippet(SnippetInfo snippetInfo)
        {
            return false;
        }

        private void PopulateSnippetCaches()
        {
            var updatedSnippets = GetSnippetInfoList();
            var updatedSnippetShortcuts = GetShortcutsHashFromSnippets(updatedSnippets);

            lock (cacheGuard)
            {
                snippets = updatedSnippets;
                snippetShortcuts = updatedSnippetShortcuts;
            }
        }

        protected static HashSet<string> GetShortcutsHashFromSnippets(IList<SnippetInfo> updatedSnippets)
        {
            return new HashSet<string>(updatedSnippets.Select(s => s.Shortcut), StringComparer.OrdinalIgnoreCase);
        }

        private IList<SnippetInfo> GetSnippetInfoList()
        {
            IVsExpansionEnumeration expansionEnumerator;
            if (TryGetVsSnippets(out expansionEnumerator))
            {
                return ExtractSnippetInfo(expansionEnumerator);
            }

            return SpecializedCollections.EmptyList<SnippetInfo>();
        }

        private bool TryGetVsSnippets(out IVsExpansionEnumeration expansionEnumerator)
        {
            expansionEnumerator = null;
            if (_expansionManager != null)
            {
                _expansionManager.EnumerateExpansions(
                    _languageGuidForSnippets,
                    fShortCutOnly: 0,
                    bstrTypes: null,
                    iCountTypes: 0,
                    fIncludeNULLType: 1,
                    fIncludeDuplicates: 1, // Allows snippets with the same title but different shortcuts
                    pEnum: out expansionEnumerator);
            }

            return expansionEnumerator != null;
        }

        private static IList<SnippetInfo> ExtractSnippetInfo(IVsExpansionEnumeration expansionEnumerator)
        {
            IList<SnippetInfo> snippetList = new List<SnippetInfo>();

            uint count = 0;
            uint fetched = 0;
            VsExpansion snippetInfo = new VsExpansion();
            IntPtr[] pSnippetInfo = new IntPtr[1];

            try
            {
                // Allocate enough memory for one VSExpansion structure. This memory is filled in by the Next method.
                pSnippetInfo[0] = Marshal.AllocCoTaskMem(Marshal.SizeOf(snippetInfo));
                expansionEnumerator.GetCount(out count);

                for (uint i = 0; i < count; i++)
                {
                    expansionEnumerator.Next(1, pSnippetInfo, out fetched);
                    if (fetched > 0)
                    {
                        // Convert the returned blob of data into a structure that can be read in managed code.
                        snippetInfo = ConvertToVsExpansionAndFree(pSnippetInfo[0]);

                        if (!string.IsNullOrEmpty(snippetInfo.shortcut))
                        {
                            snippetList.Add(new SnippetInfo(snippetInfo.shortcut, snippetInfo.title, snippetInfo.description, snippetInfo.path));
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(pSnippetInfo[0]);
            }

            return snippetList;
        }

        private static VsExpansion ConvertToVsExpansionAndFree(IntPtr expansionPtr)
        {
            var buffer = (VsExpansionWithIntPtrs)Marshal.PtrToStructure(expansionPtr, typeof(VsExpansionWithIntPtrs));
            var expansion = new VsExpansion();

            ConvertToStringAndFree(ref buffer.DescriptionPtr, ref expansion.description);
            ConvertToStringAndFree(ref buffer.PathPtr, ref expansion.path);
            ConvertToStringAndFree(ref buffer.ShortcutPtr, ref expansion.shortcut);
            ConvertToStringAndFree(ref buffer.TitlePtr, ref expansion.title);

            return expansion;
        }

        private static void ConvertToStringAndFree(ref IntPtr ptr, ref string str)
        {
            if (ptr != IntPtr.Zero)
            {
                str = Marshal.PtrToStringBSTR(ptr);
                Marshal.FreeBSTR(ptr);
                ptr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// This structure is used to facilitate the interop calls with IVsExpansionEnumeration.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct VsExpansionWithIntPtrs
        {
            public IntPtr PathPtr;
            public IntPtr TitlePtr;
            public IntPtr ShortcutPtr;
            public IntPtr DescriptionPtr;
        }
    }
}
