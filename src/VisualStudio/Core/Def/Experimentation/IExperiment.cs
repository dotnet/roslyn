// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
