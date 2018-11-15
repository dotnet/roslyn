// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IEditorConfigStorageLocation2 : IEditorConfigStorageLocation
    {
        /// <summary>
        /// Gets the editorconfig string representation for this storage location.
        /// </summary>
        string GetEditorConfigString(object value, AnalyzerConfigOptions optionSet);
    }
}
