﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

#if CODE_STYLE
namespace Microsoft.CodeAnalysis.Internal.Options
{
    public interface IEditorConfigStorageLocation2 : IEditorConfigStorageLocation
#else
namespace Microsoft.CodeAnalysis.Options
{
    internal interface IEditorConfigStorageLocation2 : IEditorConfigStorageLocation
#endif
    {
        string KeyName { get; }

        /// <summary>
        /// Gets the editorconfig string representation for this storage location.
        /// </summary>
        string GetEditorConfigString(object? value, OptionSet optionSet);
    }
}
