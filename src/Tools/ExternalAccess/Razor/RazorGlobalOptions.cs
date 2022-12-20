﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Formatting;
using System.Linq;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    [Export(typeof(RazorGlobalOptions)), Shared]
    internal sealed class RazorGlobalOptions
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be marked with 'ObsoleteAttribute'", Justification = "Used in test code")]
        public RazorGlobalOptions(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public RazorAutoFormattingOptions GetAutoFormattingOptions()
            => new(_globalOptions.GetAutoFormattingOptions(LanguageNames.CSharp));

        public bool UseTabs
        {
            get => _globalOptions.GetOption(RazorLineFormattingOptionsStorage.UseTabs);
            set => _globalOptions.SetGlobalOption(new OptionKey(RazorLineFormattingOptionsStorage.UseTabs), value);
        }

        public int TabSize
        {
            get => _globalOptions.GetOption(RazorLineFormattingOptionsStorage.TabSize);
            set => _globalOptions.SetGlobalOption(new OptionKey(RazorLineFormattingOptionsStorage.TabSize), value);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        /// <summary>
        /// For testing purposes only. Razor does not use MEF composition for host services so we need to return a mock.
        /// </summary>
        public static RazorGlobalOptions GetGlobalOptions(Workspace workspace)
            => new(new TestGlobalOptionService());
#pragma warning restore

        private sealed class TestGlobalOptionService : IGlobalOptionService
        {
#pragma warning disable CS0067 // Remove unused event
            public event EventHandler<OptionChangedEventArgs>? OptionChanged;
#pragma warning restore

            public T GetOption<T>(PerLanguageOption2<T> option, string? languageName)
                => default!;

            public T GetOption<T>(Option2<T> option) => throw new NotImplementedException();
            public object? GetOption(OptionKey optionKey) => throw new NotImplementedException();
            public ImmutableArray<object?> GetOptions(ImmutableArray<OptionKey> optionKeys) => throw new NotImplementedException();
            public IEnumerable<IOption> GetRegisteredOptions() => throw new NotImplementedException();
            public ImmutableHashSet<IOption> GetRegisteredSerializableOptions(ImmutableHashSet<string> languages) => throw new NotImplementedException();
            public void RefreshOption(OptionKey optionKey, object? newValue) => throw new NotImplementedException();
            public void RegisterWorkspace(Workspace workspace) => throw new NotImplementedException();
            public void SetGlobalOption(OptionKey optionKey, object? value) => throw new NotImplementedException();
            public void SetGlobalOptions(ImmutableArray<OptionKey> optionKeys, ImmutableArray<object?> values) => throw new NotImplementedException();
            public void SetOptions(OptionSet optionSet, IEnumerable<OptionKey> optionKeys) => throw new NotImplementedException();
            public void UnregisterWorkspace(Workspace workspace) => throw new NotImplementedException();
        }
    }
}
