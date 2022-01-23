// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    /// <summary>
    /// TODO - Move to lower layer once the protocol converter is figured out.
    /// </summary>
    internal class ProjectsHandler : ILspRequestHandler<object, object[], Solution>
    {
        public async Task<object[]> HandleAsync(object param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var projects = new ArrayBuilder<CustomProtocol.Project>();
            var solution = requestContext.Context;
            foreach (var project in solution.Projects)
            {
                var externalUris = new ArrayBuilder<Uri>();
                foreach (var sourceFile in project.Documents)
                {
                    var uri = new Uri(sourceFile.FilePath);
#pragma warning disable 0612
                    if (!requestContext.ProtocolConverter.IsContainedInRootFolders(uri))
#pragma warning restore 0612
                    {
                        externalUris.Add(uri);
                    }
                }
#pragma warning disable 0612
                await requestContext.ProtocolConverter.RegisterExternalFilesAsync(externalUris.ToArrayAndFree()).ConfigureAwait(false);
#pragma warning restore 0612

                var lspProject = new CustomProtocol.Project
                {
                    Name = project.Name,
                    SourceFiles = project.Documents.Select(d => requestContext.ProtocolConverter.ToProtocolUri(new Uri(d.FilePath))).ToArray(),
                    Language = project.Language
                };

                projects.Add(lspProject);
            }

            return projects.ToArrayAndFree();
        }
    }
}
