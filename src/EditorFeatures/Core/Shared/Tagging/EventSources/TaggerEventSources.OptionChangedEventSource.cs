// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private sealed class GlobalOptionChangedEventSource : AbstractTaggerEventSource
        {
            private readonly IOption _globalOption;
            private readonly IGlobalOptionService _globalOptions;

            public GlobalOptionChangedEventSource(IGlobalOptionService globalOptions, IOption globalOption)
            {
                _globalOptions = globalOptions;
                _globalOption = globalOption;
            }

            public override void Connect()
            {
                _globalOptions.OptionChanged += OnGlobalOptionChanged;
            }

            public override void Disconnect()
            {
                _globalOptions.OptionChanged -= OnGlobalOptionChanged;
            }

            private void OnGlobalOptionChanged(object? sender, OptionChangedEventArgs e)
            {
                if (e.Option == _globalOption)
                {
                    RaiseChanged();
                }
            }
        }
    }
}
