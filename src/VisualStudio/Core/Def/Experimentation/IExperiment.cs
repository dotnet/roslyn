// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Experimentation
{
    /// <summary>
    /// Experiments that need to be initialized on VS startup, after the solution has been loaded.
    /// </summary>
    internal interface IExperiment
    {
        Task InitializeAsync();
    }
}
