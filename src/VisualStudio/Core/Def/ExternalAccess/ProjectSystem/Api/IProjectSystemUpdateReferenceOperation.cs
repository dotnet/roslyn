// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api
{
    internal interface IProjectSystemUpdateReferenceOperation
    {
        /// <summary>
        /// Applies a reference update operation to the project file.
        /// </summary>
        /// <returns>A boolean indicating success.</returns>
        Task<bool> ApplyAsync();

        /// <summary>
        /// Reverts a reference update operation to the project file.
        /// </summary>
        /// <returns>A boolean indicating success.</returns>
        Task<bool> RevertAsync();
    }
}
