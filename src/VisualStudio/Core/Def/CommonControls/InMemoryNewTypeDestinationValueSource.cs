// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;

namespace Microsoft.VisualStudio.LanguageServices.CommonControls;

internal class InMemoryNewTypeDestinationValueSource() : INewTypeDestinationValueSource
{
    public NewTypeDestination NewTypeDestination { get; set; }
}
