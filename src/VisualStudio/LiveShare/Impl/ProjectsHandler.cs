// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    /// <summary>
    /// TODO - Move to lower layer once the protocol converter is figured out.
    /// </summary>
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, RoslynMethods.ProjectsName)]
    internal class ProjectsHandler : ILspRequestHandler<object, CustomProtocol.Project[], Solution>
    {
        public async Task<CustomProtocol.Project[]> HandleAsync(object param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var projects = new ArrayBuilder<CustomProtocol.Project>();
            var solution = requestContext.Context;
            foreach (var project in solution.Projects)
            {
                var externalUris = new ArrayBuilder<Uri>();
                foreach (var sourceFile in project.Documents)
                {
                    var uri = new Uri(sourceFile.FilePath);
                    if (!requestContext.ProtocolConverter.IsContainedInRootFolders(uri))
                    {
                        externalUris.Add(uri);
                    }
                }
                await requestContext.ProtocolConverter.RegisterExternalFilesAsync(externalUris.ToArrayAndFree()).ConfigureAwait(false);

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
