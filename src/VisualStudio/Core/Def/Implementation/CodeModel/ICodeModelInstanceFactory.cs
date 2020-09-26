// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal interface ICodeModelInstanceFactory
    {
        /// <summary>
        /// Requests the project system to create a <see cref="EnvDTE.FileCodeModel"/> through the project system.
        /// </summary>
        /// <remarks>
        /// This is sometimes necessary because it's possible to go from one <see cref="EnvDTE.FileCodeModel"/> to another,
        /// but the language service can't create that instance directly, as it doesn't know what the <see cref="EnvDTE.FileCodeModel.Parent"/>
        /// member should be. The expectation is the implementer of this will do what is necessary and call back into <see cref="IProjectCodeModel.GetOrCreateFileCodeModel(string, object)"/>
        /// handing it the appropriate parent.</remarks>
        EnvDTE.FileCodeModel TryCreateFileCodeModelThroughProjectSystem(string filePath);
    }
}
