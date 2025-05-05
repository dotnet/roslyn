// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp;

internal sealed class ModelUpdatedEventsArgs : EventArgs
{
    public ModelUpdatedEventsArgs(Model? newModel)
    {
        NewModel = newModel;
    }

    public Model? NewModel { get; }
}
