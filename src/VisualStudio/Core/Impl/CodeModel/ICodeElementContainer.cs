// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
