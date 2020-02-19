// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
namespace Microsoft.CodeAnalysis.Internal.Options
#else
namespace Microsoft.CodeAnalysis.Options
#endif
{
    internal interface IEditorConfigStorageLocation2 : IEditorConfigStorageLocation
    {
        /// <summary>
        /// Gets the editorconfig string representation for this storage location.
        /// </summary>
        string GetEditorConfigString(object value, OptionSet optionSet);
    }
}
