// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;

internal abstract class Snapshot
{
    public abstract int Count { get; }
    public abstract EnvDTE.CodeElement this[int index] { get; }
}
