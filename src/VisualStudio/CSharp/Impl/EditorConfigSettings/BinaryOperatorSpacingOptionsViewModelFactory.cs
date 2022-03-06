// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.EditorConfigSettings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.EditorConfigSettings
{
    [Export(typeof(IEnumSettingViewModelFactory)), Shared]
    internal class BinaryOperatorSpacingOptionsViewModelFactory : IEnumSettingViewModelFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public BinaryOperatorSpacingOptionsViewModelFactory()
        {
        }

        public IEnumSettingViewModel CreateViewModel(WhitespaceSetting setting)
        {
            return new BinaryOperatorSpacingOptionsViewModel(setting);
        }

        public bool IsSupported(OptionKey2 key)
            => key.Option.Type == typeof(BinaryOperatorSpacingOptions);
    }
}
