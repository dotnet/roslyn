// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    /// <summary>
    /// This service is created on the UI thread during package initialization, but it must not
    /// block the initialization process. If the expansion manager is an IExpansionManager,
    /// then we can use the asynchronous population mechanism it provides. Otherwise, getting
    /// snippet information from the <see cref="IVsExpansionManager"/> must be done synchronously
    /// through on the UI thread, which we do after package initialization at a lower priority.
    /// </summary>
    /// <remarks>
    /// IExpansionManager was introduced in Visual Studio 2015 Update 1, but
    /// will be enabled by default for the first time in Visual Studio 2015 Update 2. However,
    /// the platform still supports returning the <see cref="IVsExpansionManager"/> if a major
    /// problem in the IExpansionManager is discovered, so we must continue 
    /// supporting the fallback.
    /// </remarks>
    internal abstract class AbstractSnippetInfoService : ForegroundThreadAffinitizedObject, ISnippetInfoService, IVsExpansionEvents
    {
        private readonly Guid _languageGuidForSnippets;
        private readonly IVsExpansionManager _expansionManager;

        /// <summary>
        /// Initialize these to empty values. When returning from <see cref="GetSnippetsIfAvailable "/> 
        /// and <see cref="SnippetShortcutExists_NonBlocking"/>, we return the current set of known 
        /// snippets rather than waiting for initial results.
        /// </summary>
        protected IList<SnippetInfo> snippets = SpecializedCollections.EmptyList<SnippetInfo>();
        protected ISet<string> snippetShortcuts = SpecializedCollections.EmptySet<string>();

        // Guard the snippets and snippetShortcut fields so that returned result sets are always
        // complete.
        protected object cacheGuard = new object();

        private readonly AggregateAsynchronousOperationListener _waiter;

        public AbstractSnippetInfoService(
            Shell.SVsServiceProvider serviceProvider,
            Guid languageGuidForSnippets,
            IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            AssertIsForeground();

            if (serviceProvider != null)
            {
                var textManager = (IVsTextManager2)serviceProvider.GetService(typeof(SVsTextManager));
                if (textManager.GetExpansionManager(out _expansionManager) == VSConstants.S_OK)
                {
                    ComEventSink.Advise<IVsExpansionEvents>(_expansionManager, this);
                    _waiter = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.Snippets);
                    _languageGuidForSnippets = languageGuidForSnippets;
                    PopulateSnippetCaches();
                }
            }
        }

        public int OnAfterSnippetsUpdate()
        {
            AssertIsForeground();

            if (_expansionManager != null)
            {
                PopulateSnippetCaches();
            }

            return VSConstants.S_OK;
        }

        public int OnAfterSnippetsKeyBindingChange([ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]uint dwCmdGuid, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]uint dwCmdId, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")]int fBound)
        {
            return VSConstants.S_OK;
        }

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
            // Check against the known set of snippets, even if we're still in the process of
            // calculating a more up-to-date list.
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
            Debug.Assert(_expansionManager != null);

            // Ideally we'd fork execution here based on whether the expansion manager is an
            // IExpansionManager or not. Unfortunately, we cannot mention that type by name until
            // the Roslyn build machines are upgraded to Visual Studio 2015 Update 1. We therefore
            // need to try using IExpansionManager dynamically, from a background thread. If that
            // fails, then we come back to the UI thread for using IVsExpansionManager instead.

            var token = _waiter.BeginAsyncOperation(GetType().Name + ".Start");

            Task.Factory.StartNew(async () => await PopulateSnippetCacheOnBackgroundWithForegroundFallback().ConfigureAwait(false),
                            CancellationToken.None,
                            TaskCreationOptions.None,
                            TaskScheduler.Default).CompletesAsyncOperation(token);
        }

        private async Task PopulateSnippetCacheOnBackgroundWithForegroundFallback()
        {
            AssertIsBackground();

            try
            {
                IVsExpansionEnumeration expansionEnumerator = await ((dynamic)_expansionManager).EnumerateExpansionsAsync(
                    _languageGuidForSnippets,
                    0, // shortCutOnly
                    Array.Empty<string>(), // types
                    0, // countTypes
                    1, // includeNULLTypes
                    1 // includeDulicates: Allows snippets with the same title but different shortcuts
                    ).ConfigureAwait(false);

                // The rest of the process requires being on the UI thread, see the explanation on 
                // PopulateSnippetCacheFromExpansionEnumeration for details
                await Task.Factory.StartNew(() => PopulateSnippetCacheFromExpansionEnumeration(expansionEnumerator),
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    ForegroundTaskScheduler).ConfigureAwait(false);
            }
            catch (RuntimeBinderException)
            {
                // The IExpansionManager.EnumerateExpansionsAsync could not be found. Use 
                // IVsExpansionManager.EnumerateExpansions instead, but from the UI thread.
                await Task.Factory.StartNew(() => PopulateSnippetCacheOnForeground(),
                            CancellationToken.None,
                            TaskCreationOptions.None,
                            ForegroundTaskScheduler).ConfigureAwait(false);
            }
        }

        /// <remarks>
        /// Changes to the <see cref="IVsExpansionManager.EnumerateExpansions"/> invocation
        /// should also be made to the IExpansionManager.EnumerateExpansionsAsync
        /// invocation in <see cref="PopulateSnippetCacheOnBackgroundWithForegroundFallback"/>.
        /// </remarks>
        private void PopulateSnippetCacheOnForeground()
        {
            AssertIsForeground();

            IVsExpansionEnumeration expansionEnumerator = null;
            _expansionManager.EnumerateExpansions(
                _languageGuidForSnippets,
                fShortCutOnly: 0,
                bstrTypes: null,
                iCountTypes: 0,
                fIncludeNULLType: 1,
                fIncludeDuplicates: 1, // Allows snippets with the same title but different shortcuts
                pEnum: out expansionEnumerator);

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
        /// CExpansionEnumeratorShim` goes smoothly and everything works.
        ///
        /// When we call from a background thread, the COM marshaller has to move execution to the
        /// UI thread, and as part of this process it uses the interface as defined in the idl to
        /// set up the appropriate arguments to pass. The same parameter from the idl is defined as
        ///    [out, size_is(celt), length_is(*pceltFetched)] VsExpansion **rgelt
        ///
        /// Because rgelt is specified as an `out` parameter, the marshaller is discarding the
        /// pointer we passed and substituting the null reference. This then causes a null
        /// reference exception in the shim. Calling from the UI thread avoids this marshaller.
        /// </remarks>
        void PopulateSnippetCacheFromExpansionEnumeration(IVsExpansionEnumeration expansionEnumerator)
        {
            AssertIsForeground();

            var updatedSnippets = ExtractSnippetInfo(expansionEnumerator);
            var updatedSnippetShortcuts = GetShortcutsHashFromSnippets(updatedSnippets);

            lock (cacheGuard)
            {
                snippets = updatedSnippets;
                snippetShortcuts = updatedSnippetShortcuts;
            }
        }

        private IList<SnippetInfo> ExtractSnippetInfo(IVsExpansionEnumeration expansionEnumerator)
        {
            AssertIsForeground();

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

        protected static HashSet<string> GetShortcutsHashFromSnippets(IList<SnippetInfo> updatedSnippets)
        {
            return new HashSet<string>(updatedSnippets.Select(s => s.Shortcut), StringComparer.OrdinalIgnoreCase);
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
