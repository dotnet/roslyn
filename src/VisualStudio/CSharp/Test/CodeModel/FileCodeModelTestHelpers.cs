// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.UnitTests;
using static Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CodeModelTestHelpers;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    internal static class FileCodeModelTestHelpers
    {
        // If something is *really* wrong with our COM marshalling stuff, the creation of the CodeModel will probably
        // throw some sort of AV or other Very Bad exception. We still want to be able to catch them, so we can clean up
        // the workspace. If we don't, we leak the workspace and it'll take down the process when it throws in a
        // finalizer complaining we didn't clean it up. Catching AVs is of course not safe, but this is balancing
        // "probably not crash" as an improvement over "will crash when the finalizer throws."
        [HandleProcessCorruptedStateExceptions]
        public static async Task<Tuple<TestWorkspace, EnvDTE.FileCodeModel>> CreateWorkspaceAndFileCodeModelAsync(string file)
        {
            var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceFromFileAsync(file, exportProvider: VisualStudioTestExportProvider.ExportProvider);

            try
            {
                var project = workspace.CurrentSolution.Projects.Single();
                var document = project.Documents.Single().Id;

                var componentModel = new MockComponentModel(workspace.ExportProvider);
                var serviceProvider = new MockServiceProvider(componentModel);
                WrapperPolicy.s_ComWrapperFactory = MockComWrapperFactory.Instance;

                var visualStudioWorkspaceMock = new MockVisualStudioWorkspace(workspace);

                var state = new CodeModelState(serviceProvider, project.LanguageServices, visualStudioWorkspaceMock);

                var codeModel = FileCodeModel.Create(state, null, document, new MockTextManagerAdapter()).Handle;

                return Tuple.Create(workspace, (EnvDTE.FileCodeModel)codeModel);
            }
            catch
            {
                // We threw during creation of the FileCodeModel. Make sure we clean up our workspace or else we leak it
                workspace.Dispose();
                throw;
            }
        }
    }
}
