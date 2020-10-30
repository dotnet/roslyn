// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Diagnostics;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
//using Microsoft.CodeAnalysis.Shared.TestHooks;
//using Microsoft.CodeAnalysis.Snippets;
//using Microsoft.VisualStudio.Text.Editor.Expansion;
//using Microsoft.VisualStudio.Utilities;
//using Roslyn.Utilities;

//namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
//{
//    /// <summary>
//    /// This service is created on the UI thread during package initialization, but it must not
//    /// block the initialization process.
//    /// </summary>
//    internal abstract class AbstractSnippetInfoService : ForegroundThreadAffinitizedObject, ISnippetInfoService 
//    {
//        private readonly IContentType _contentType;
//        private readonly IExpansionManager _expansionManager;

//        /// <summary>
//        /// Initialize these to empty values. When returning from <see cref="GetSnippetsIfAvailable "/> 
//        /// and <see cref="SnippetShortcutExists_NonBlocking"/>, we return the current set of known 
//        /// snippets rather than waiting for initial results.
//        /// </summary>
//        protected ImmutableArray<SnippetInfo> snippets = ImmutableArray.Create<SnippetInfo>();
//        protected IImmutableSet<string> snippetShortcuts = ImmutableHashSet.Create<string>();

//        // Guard the snippets and snippetShortcut fields so that returned result sets are always
//        // complete.
//        protected object cacheGuard = new object();

//        private readonly IAsynchronousOperationListener _waiter;

//        public AbstractSnippetInfoService(
//            IThreadingContext threadingContext,
//            IExpansionManager expansionManager,
//            IContentType contentType,
//            IAsynchronousOperationListenerProvider listenerProvider)
//            : base(threadingContext)
//        {
//            _expansionManager = expansionManager;
//            _waiter = listenerProvider.GetListener(FeatureAttribute.Snippets);
//            _contentType = contentType;
//            PopulateSnippetCaches();
//        }

//        public void OnAfterSnippetsUpdate()
//        {
//            if (_expansionManager != null)
//            {
//                PopulateSnippetCaches();
//            }
//        }

//        public IEnumerable<SnippetInfo> GetSnippetsIfAvailable()
//        {
//            // Immediately return the known set of snippets, even if we're still in the process
//            // of calculating a more up-to-date list.
//            lock (cacheGuard)
//            {
//                return snippets;
//            }
//        }

//        public bool SnippetShortcutExists_NonBlocking(string shortcut)
//        {
//            if (shortcut == null)
//            {
//                return false;
//            }

//            // Check against the known set of snippets, even if we're still in the process of
//            // calculating a more up-to-date list.
//            lock (cacheGuard)
//            {
//                return snippetShortcuts.Contains(shortcut);
//            }
//        }

//        public virtual bool ShouldFormatSnippet(SnippetInfo snippetInfo)
//        {
//            return false;
//        }

//        private void PopulateSnippetCaches()
//        {
//            Debug.Assert(_expansionManager != null);

//            PopulateSnippetCacheFromExpansionEnumeration(_expansionManager.EnumerateExpansions(_contentType));
//        }

//        /// <remarks>
//        /// This method must be called on the UI thread because it eventually calls into
//        /// IVsExpansionEnumeration.Next, which must be called on the UI thread due to an issue
//        /// with how the call is marshalled.
//        /// 
//        /// The second parameter for IVsExpansionEnumeration.Next is defined like this:
//        ///    [ComAliasName("Microsoft.VisualStudio.TextManager.Interop.VsExpansion")] IntPtr[] rgelt
//        ///
//        /// We pass a pointer for rgelt that we expect to be populated as the result. This
//        /// eventually calls into the native CExpansionEnumeratorShim::Next method, which has the
//        /// same contract of expecting a non-null rgelt that it can drop expansion data into. When
//        /// we call from the UI thread, this transition from managed code to the
//        /// CExpansionEnumeratorShim goes smoothly and everything works.
//        ///
//        /// When we call from a background thread, the COM marshaller has to move execution to the
//        /// UI thread, and as part of this process it uses the interface as defined in the idl to
//        /// set up the appropriate arguments to pass. The same parameter from the idl is defined as
//        ///    [out, size_is(celt), length_is(*pceltFetched)] VsExpansion **rgelt
//        ///
//        /// Because rgelt is specified as an <c>out</c> parameter, the marshaller is discarding the
//        /// pointer we passed and substituting the null reference. This then causes a null
//        /// reference exception in the shim. Calling from the UI thread avoids this marshaller.
//        /// </remarks>
//        private void PopulateSnippetCacheFromExpansionEnumeration(IEnumerable<ExpansionTemplate> expansionEnumerator)
//        {
//            var updatedSnippets = ExtractSnippetInfo(expansionEnumerator);
//            var updatedSnippetShortcuts = GetShortcutsHashFromSnippets(updatedSnippets);

//            lock (cacheGuard)
//            {
//                snippets = updatedSnippets;
//                snippetShortcuts = updatedSnippetShortcuts;
//            }
//        }

//        private ImmutableArray<SnippetInfo> ExtractSnippetInfo(IEnumerable<ExpansionTemplate> expansionEnumerator)
//        {
//            var snippetListBuilder = ImmutableArray.CreateBuilder<SnippetInfo>();

//            foreach (var expansionTemplate in expansionEnumerator)
//            {
//                if (!string.IsNullOrEmpty(expansionTemplate.Snippet.Shortcut))
//                {
//                    snippetListBuilder.Add(new SnippetInfo(expansionTemplate.Snippet.Shortcut, expansionTemplate.Snippet.Title, expansionTemplate.Snippet.Description, expansionTemplate.Snippet.FilePath));
//                }
//            }

//            return snippetListBuilder.ToImmutable();
//        }

//        protected static IImmutableSet<string> GetShortcutsHashFromSnippets(ImmutableArray<SnippetInfo> updatedSnippets)
//        {
//            return new HashSet<string>(updatedSnippets.Select(s => s.Shortcut), StringComparer.OrdinalIgnoreCase)
//                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
//        }
//    }
//}
