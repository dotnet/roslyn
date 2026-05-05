// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Razor.Extensions;
using Microsoft.VisualStudio.Razor.Settings;
using Microsoft.VisualStudio.Razor.Snippets;
using Microsoft.VisualStudio.Razor.Telemetry;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.RazorExtension.Snippets;

internal class SnippetService
{
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly IAsyncServiceProvider _serviceProvider;
    private readonly SnippetCache _snippetCache;
    private readonly IAdvancedSettingsStorage _advancedSettingsStorage;
    private IVsExpansionManager? _vsExpansionManager;

    private static readonly Guid s_CSharpLanguageId = new("694dd9b6-b865-4c5b-ad85-86356e9c88dc");
    private static readonly Guid s_HtmlLanguageId = new("9bbfd173-9770-47dc-b191-651b7ff493cd");

    private static readonly Dictionary<Guid, ImmutableHashSet<string>> s_builtInSnippets = new()
    {
        {
            s_CSharpLanguageId,
            ImmutableHashSet.Create(
                "~", "Attribute", "checked", "class", "ctor", "cw", "do", "else", "enum", "equals", "Exception", "for", "foreach", "forr",
                "if", "indexer", "interface", "invoke", "iterator", "iterindex", "lock", "mbox", "namespace", "#if", "#region", "prop",
                "propfull", "propg", "sim", "struct", "svm", "switch", "try", "tryf", "unchecked", "unsafe", "using", "while")
        },
        {
            s_HtmlLanguageId,
            ImmutableHashSet.Create(
                "a", "audio", "base", "br", "charset", "content", "dd", "div", "figure", "form", "html", "html4f", "html4s", "html4t", "html5", "iframe",
                "img", "input", "link", "meta", "metaviewport", "picture", "region", "script", "scriptr", "scriptr2", "select", "selfclosing",
                "source", "style", "svg", "table", "ul", "video", "xhtml10f", "xhtml10s", "xhtml10t", "xhtml11", "xhtml5")
        }
    };

    public SnippetService(
        JoinableTaskFactory joinableTaskFactory,
        IAsyncServiceProvider serviceProvider,
        SnippetCache snippetCache,
        IAdvancedSettingsStorage advancedSettingsStorage)
    {
        _joinableTaskFactory = joinableTaskFactory;
        _serviceProvider = serviceProvider;
        _snippetCache = snippetCache;
        _advancedSettingsStorage = advancedSettingsStorage;
        _joinableTaskFactory.RunAsync(InitializeAsync).FileAndForget(TelemetryReporter.GetEventName("SnippetService_Initialize"));
    }

    private async Task InitializeAsync()
    {
        await _advancedSettingsStorage.OnChangedAsync(_ =>
        {
            // If the settings changed before we were able to get the expansion manager, skip population because we can't do it.
            // After getting the manager for the first time, we'll populate again, so it will all work out.
            if (_vsExpansionManager is null)
            {
                return;
            }

            PopulateAsync().FileAndForget(TelemetryReporter.GetEventName("SnippetService_Populate"));
        }).ConfigureAwait(false);

        await _joinableTaskFactory.SwitchToMainThreadAsync();
        var textManager = (IVsTextManager2?)await _serviceProvider.GetServiceAsync(typeof(SVsTextManager)).ConfigureAwait(true);
        if (textManager is null)
        {
            return;
        }

        if (textManager.GetExpansionManager(out _vsExpansionManager) == VSConstants.S_OK)
        {
            // Call the asynchronous IExpansionManager API from a background thread
            await TaskScheduler.Default;
            await PopulateAsync().ConfigureAwait(false);
        }
    }

    private async Task PopulateAsync()
    {
        var csharpExpansionEnumerator = await GetExpansionEnumeratorAsync(s_CSharpLanguageId).ConfigureAwait(false);
        var htmlExpansionEnumerator = await GetExpansionEnumeratorAsync(s_HtmlLanguageId).ConfigureAwait(false);

        // The rest of the process requires being on the UI thread, see the explanation on
        // PopulateSnippetCacheFromExpansionEnumeration for details
        await _joinableTaskFactory.SwitchToMainThreadAsync();
        PopulateSnippetCacheFromExpansionEnumeration(
            (SnippetLanguage.CSharp, csharpExpansionEnumerator),
            (SnippetLanguage.Html, htmlExpansionEnumerator));
    }

    private Task<IVsExpansionEnumeration> GetExpansionEnumeratorAsync(Guid languageGuid)
    {
        _vsExpansionManager.AssumeNotNull();
        var expansionManager = (IExpansionManager)_vsExpansionManager;

        return expansionManager.EnumerateExpansionsAsync(
                languageGuid,
                0, // shortCutOnly
                Array.Empty<string>(), // types
                0, // countTypes
                1, // includeNULLTypes
                1 // includeDulicates: Allows snippets with the same title but different shortcuts
        );
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
    private void PopulateSnippetCacheFromExpansionEnumeration(params (SnippetLanguage language, IVsExpansionEnumeration expansionEnumerator)[] enumerators)
    {
        _joinableTaskFactory.Context.AssertUIThread();
        var snippetSetting = _advancedSettingsStorage.GetAdvancedSettings().SnippetSetting;
        foreach (var (language, enumerator) in enumerators)
        {
            _snippetCache.Update(language, ExtractSnippetInfo(language, enumerator, snippetSetting));
        }
    }

    private ImmutableArray<SnippetInfo> ExtractSnippetInfo(SnippetLanguage language, IVsExpansionEnumeration expansionEnumerator, SnippetSetting snippetSetting)
    {
        _joinableTaskFactory.Context.AssertUIThread();

        if (snippetSetting == SnippetSetting.None)
        {
            return ImmutableArray<SnippetInfo>.Empty;
        }

        var snippetInfo = new VsExpansion();
        var pSnippetInfo = new IntPtr[1];

        try
        {
            // Allocate enough memory for one VSExpansion structure. This memory is filled in by the Next method.
            pSnippetInfo[0] = Marshal.AllocCoTaskMem(Marshal.SizeOf(snippetInfo));

            var result = expansionEnumerator.GetCount(out var count);
            if (result != HResult.OK)
            {
                return ImmutableArray<SnippetInfo>.Empty;
            }

            var ignoredSnippets = GetIgnoredSnippets(language, snippetSetting);
            using var snippetListBuilder = new PooledArrayBuilder<SnippetInfo>();

            for (uint i = 0; i < count; i++)
            {
                result = expansionEnumerator.Next(1, pSnippetInfo, out var fetched);
                if (result != HResult.OK)
                {
                    continue;
                }

                if (fetched > 0)
                {
                    // Convert the returned blob of data into a structure that can be read in managed code.
                    snippetInfo = ConvertToVsExpansionAndFree(pSnippetInfo[0]);

                    if (!string.IsNullOrEmpty(snippetInfo.shortcut) && !ignoredSnippets.Contains(snippetInfo.shortcut))
                    {
                        snippetListBuilder.Add(new SnippetInfo(snippetInfo.shortcut, snippetInfo.title, snippetInfo.description, snippetInfo.path, language));
                    }
                }
            }

            return snippetListBuilder.ToImmutable();
        }
        finally
        {
            Marshal.FreeCoTaskMem(pSnippetInfo[0]);
        }
    }

    private static ImmutableHashSet<string> GetIgnoredSnippets(SnippetLanguage language, SnippetSetting snippetSetting)
    {
        if (language == SnippetLanguage.CSharp)
        {
            // In call cases for C# we want to filter out the built in snippets. The C#
            // language server will handle returning these and isn't required for the
            // Razor code to add them.
            return s_builtInSnippets[s_CSharpLanguageId];
        }

        if (snippetSetting == SnippetSetting.All)
        {
            return ImmutableHashSet<string>.Empty;
        }

        // As of writing, Html and CSharp are the only languages we actually
        // get snippets for. This assert acts as both a testament to that and
        // a catch for any future soul who might change that behavior. This
        // code will need to be updated accordingly, as well as the
        // the s_buildInSnippets dictionary
        Debug.Assert(language == SnippetLanguage.Html);
        return s_builtInSnippets[s_HtmlLanguageId];
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
