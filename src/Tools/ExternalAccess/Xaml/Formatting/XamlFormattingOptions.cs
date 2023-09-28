// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml.Formatting;

internal class XamlFormattingOptions(int tabSize, bool insertSpaces, IDictionary<string, object>? otherOptions)
{
    public bool InsertSpaces { get; } = insertSpaces;
    public int TabSize { get; } = tabSize;
    public IDictionary<string, object>? OtherOptions { get; } = otherOptions;
}
