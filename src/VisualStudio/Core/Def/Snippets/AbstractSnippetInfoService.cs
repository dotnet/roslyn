// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    /// <summary>
    /// This service is created on the UI thread during package initialization, but it must not
    /// block the initialization process.
    /// </summary>
    internal abstract class AbstractSnippetInfoService : ISnippetInfoService, IVsExpansionEvents
    {
        private readonly Guid _languageGuidForSnippets;
        private IVsExpansionManager? _expansionManager;

        /// <summary>
        /// Initialize these to empty values. When returning from <see cref="GetSnippetsIfAvailable "/> 
        /// and <see cref="SnippetShortcutExists_NonBlocking"/>, we return the current set of known 
        /// snippets rather than waiting for initial results.
        /// </summary>
        protected ImmutableArray<SnippetInfo> snippets = ImmutableArray.Create<SnippetInfo>();
        protected IImmutableSet<string> snippetShortcuts = ImmutableHashSet.Create<string>();

        // Guard the snippets and snippetShortcut fields so that returned result sets are always
        // complete.
        protected object cacheGuard = new();

        private readonly IAsynchronousOperationListener _waiter;
        private readonly IThreadingContext _threadingContext;

        public AbstractSnippetInfoService(
            IThreadingContext threadingContext,
            Shell.IAsyncServiceProvider serviceProvider,
            Guid languageGuidForSnippets,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _waiter = listenerProvider.GetListener(FeatureAttribute.Snippets);
            _languageGuidForSnippets = languageGuidForSnippets;
            _threadingContext = threadingContext;

            _threadingContext.RunWithShutdownBlockAsync((_) => InitializeAndPopulateSnippetsCacheAsync(serviceProvider));
        }

        private async Task InitializeAndPopulateSnippetsCacheAsync(Shell.IAsyncServiceProvider asyncServiceProvider)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
            var textManager = (IVsTextManager2?)await asyncServiceProvider.GetServiceAsync(typeof(SVsTextManager)).ConfigureAwait(true);
            Assumes.Present(textManager);

            if (textManager.GetExpansionManager(out _expansionManager) == VSConstants.S_OK)
            {
                ComEventSink.Advise<IVsExpansionEvents>(_expansionManager, this);
                await PopulateSnippetCacheAsync().ConfigureAwait(false);
            }
        }

        public int OnAfterSnippetsUpdate()
        {
            _threadingContext.ThrowIfNotOnUIThread();

            if (_expansionManager != null)
            {
                _threadingContext.RunWithShutdownBlockAsync((_) => PopulateSnippetCacheAsync());
            }

            return VSConstants.S_OK;
        }

        public int OnAfterSnippetsKeyBindingChange([ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")] uint dwCmdGuid, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")] uint dwCmdId, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")] int fBound)
            => VSConstants.S_OK;

        public IEnumerable<SnippetInfo> GetSnippetsIfAvailable()
        {
            // Immediately return the known set of snippets, even if we're still in the process
            // of calculating a more up-to-date list.
            lock (cacheGuard)
            {
                return snippets;
            }
        }

        public bool SnippetShortcutExists_NonBlocking(string shortcut)
        {
            if (shortcut == null)
            {
                return false;
            }

            // Check against the known set of snippets, even if we're still in the process of
            // calculating a more up-to-date list.
            lock (cacheGuard)
            {
                return snippetShortcuts.Contains(shortcut);
            }
        }

        public virtual bool ShouldFormatSnippet(SnippetInfo snippetInfo)
            => false;

        private async Task PopulateSnippetCacheAsync()
        {
            using var token = _waiter.BeginAsyncOperation(GetType().Name + ".Start");
            RoslynDebug.Assert(_expansionManager != null);

            // In Dev14 Update2+ the platform always provides an IExpansion Manager
            var expansionManager = (IExpansionManager)_expansionManager;
            // Call the asynchronous IExpansionManager API from a background thread
            await TaskScheduler.Default;
            var expansionEnumerator = await expansionManager.EnumerateExpansionsAsync(
                _languageGuidForSnippets,
                0, // shortCutOnly
                Array.Empty<string>(), // types
                0, // countTypes
                1, // includeNULLTypes
                1 // includeDulicates: Allows snippets with the same title but different shortcuts
                ).ConfigureAwait(false);

            // The rest of the process requires being on the UI thread, see the explanation on
            // PopulateSnippetCacheFromExpansionEnumeration for details
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
            PopulateSnippetCacheFromExpansionEnumeration(expansionEnumerator);
        }

        /// <remarks>
        /// This method must be called on the UI thread because it eventually calls into
        /// IVsExpansionEnumeration.Next, which must be called on the UI thread due to an issue
        /// with how the call is marshalled.
        /// 
        /// The second parameter for IVsExpansionEnumeration.Next is defined like this:
        ///    [ComAliasName("Microsoft.VisualStudio.TextManager.Interop.VsExpansion")] IntPtr[] rgelt
        ///
        /// We pass a pointer for rgelt that we expect to be populated as the result. This
        /// eventually calls into the native CExpansionEnumeratorShim::Next method, which has the
        /// same contract of expecting a non-null rgelt that it can drop expansion data into. When
        /// we call from the UI thread, this transition from managed code to the
        /// CExpansionEnumeratorShim goes smoothly and everything works.
        ///
        /// When we call from a background thread, the COM marshaller has to move execution to the
        /// UI thread, and as part of this process it uses the interface as defined in the idl to
        /// set up the appropriate arguments to pass. The same parameter from the idl is defined as
        ///    [out, size_is(celt), length_is(*pceltFetched)] VsExpansion **rgelt
        ///
        /// Because rgelt is specified as an <c>out</c> parameter, the marshaller is discarding the
        /// pointer we passed and substituting the null reference. This then causes a null
        /// reference exception in the shim. Calling from the UI thread avoids this marshaller.
        /// </remarks>
        private void PopulateSnippetCacheFromExpansionEnumeration(IVsExpansionEnumeration expansionEnumerator)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var updatedSnippets = ExtractSnippetInfo(expansionEnumerator);
            var updatedSnippetShortcuts = GetShortcutsHashFromSnippets(updatedSnippets);

            lock (cacheGuard)
            {
                snippets = updatedSnippets;
                snippetShortcuts = updatedSnippetShortcuts;
            }
        }

        private ImmutableArray<SnippetInfo> ExtractSnippetInfo(IVsExpansionEnumeration expansionEnumerator)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var snippetListBuilder = ImmutableArray.CreateBuilder<SnippetInfo>();
            var snippetInfo = new VsExpansion();
            var pSnippetInfo = new IntPtr[1];

            try
            {
                // Allocate enough memory for one VSExpansion structure. This memory is filled in by the Next method.
                pSnippetInfo[0] = Marshal.AllocCoTaskMem(Marshal.SizeOf(snippetInfo));

                expansionEnumerator.GetCount(out var count);

                for (uint i = 0; i < count; i++)
                {
                    expansionEnumerator.Next(1, pSnippetInfo, out var fetched);
                    if (fetched > 0)
                    {
                        // Convert the returned blob of data into a structure that can be read in managed code.
                        snippetInfo = ConvertToVsExpansionAndFree(pSnippetInfo[0]);

                        if (!string.IsNullOrEmpty(snippetInfo.shortcut))
                        {
                            snippetListBuilder.Add(new SnippetInfo(snippetInfo.shortcut, snippetInfo.title, snippetInfo.description, snippetInfo.path));
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(pSnippetInfo[0]);
            }

            return snippetListBuilder.ToImmutable();
        }

        protected static IImmutableSet<string> GetShortcutsHashFromSnippets(ImmutableArray<SnippetInfo> updatedSnippets)
        {
            return new HashSet<string>(updatedSnippets.Select(s => s.Shortcut), StringComparer.OrdinalIgnoreCase)
                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
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

        private static void ConvertToStringAndFree(ref IntPtr ptr, ref string? str)
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
