// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private sealed class GlobalOptionChangedEventSource(IGlobalOptionService globalOptions, IOption2 globalOption) : AbstractTaggerEventSource
        {
            private readonly IOption2 _globalOption = globalOption;
            private readonly IGlobalOptionService _globalOptions = globalOptions;

            public override void Connect()
            {
                _globalOptions.AddOptionChangedHandler(this, OnGlobalOptionChanged);
            }

            public override void Disconnect()
            {
                _globalOptions.RemoveOptionChangedHandler(this, OnGlobalOptionChanged);
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
