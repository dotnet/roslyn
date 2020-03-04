﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    /// <summary>
    /// Interface implemented by code model objects that have a CodeElements collection.
    /// </summary>
    internal interface ICodeElementContainer<T>
    {
        EnvDTE.CodeElements GetCollection();
    }
}
