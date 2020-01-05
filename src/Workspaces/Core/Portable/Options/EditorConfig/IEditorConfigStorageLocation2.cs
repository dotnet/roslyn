// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IEditorConfigStorageLocation2 : IEditorConfigStorageLocation
    {
        /// <summary>
        /// Gets the editorconfig string representation for this storage location.
        /// </summary>
        string GetEditorConfigString(object value, OptionSet optionSet);
    }
}
