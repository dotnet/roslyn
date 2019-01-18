// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.UnitTests;
using static Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CodeModelTestHelpers;
using Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.Mocks;

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
        public static Tuple<TestWorkspace, EnvDTE.FileCodeModel> CreateWorkspaceAndFileCodeModel(string file)
        {
            var workspace = TestWorkspace.CreateCSharp(file, exportProvider: VisualStudioTestExportProvider.Factory.CreateExportProvider());

            try
            {
                var project = workspace.CurrentSolution.Projects.Single();
                var document = project.Documents.Single().Id;

                var componentModel = new MockComponentModel(workspace.ExportProvider);
                var serviceProvider = new MockServiceProvider(componentModel);
                WrapperPolicy.s_ComWrapperFactory = MockComWrapperFactory.Instance;

                var visualStudioWorkspaceMock = new MockVisualStudioWorkspace(workspace);
                var threadingContext = workspace.ExportProvider.GetExportedValue<IThreadingContext>();

                var state = new CodeModelState(
                    threadingContext,
                    serviceProvider,
                    project.LanguageServices,
                    visualStudioWorkspaceMock,
                    new ProjectCodeModelFactory(visualStudioWorkspaceMock, serviceProvider, threadingContext));

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
