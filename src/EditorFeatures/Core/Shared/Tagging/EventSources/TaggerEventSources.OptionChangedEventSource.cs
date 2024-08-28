// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging;

internal partial class TaggerEventSources
{
    private sealed class GlobalOptionChangedEventSource(IGlobalOptionService globalOptions, Func<IOption2, bool> predicate) : AbstractTaggerEventSource
    {
        public override void Connect()
        {
            globalOptions.AddOptionChangedHandler(this, OnGlobalOptionChanged);
        }

        public override void Disconnect()
        {
            globalOptions.RemoveOptionChangedHandler(this, OnGlobalOptionChanged);
        }

        private void OnGlobalOptionChanged(object sender, object target, OptionChangedEventArgs e)
        {
            if (e.HasOption(predicate))
            {
                RaiseChanged();
            }
        }
    }
}
