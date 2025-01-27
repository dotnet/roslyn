// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditorConfigSettings.DataProvider
{
    public partial class DataProviderTests
    {
        private class TestViewModel : ISettingsEditorViewModel
        {
            public void NotifyOfUpdate() { }

            Task<SourceText> ISettingsEditorViewModel.UpdateEditorConfigAsync(SourceText sourceText)
            {
                throw new NotImplementedException();
            }
        }
    }
}
