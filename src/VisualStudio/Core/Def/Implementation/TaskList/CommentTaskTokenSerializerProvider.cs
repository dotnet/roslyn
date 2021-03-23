// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using ITaskList = Microsoft.VisualStudio.Shell.ITaskList;
using SVsServiceProvider = Microsoft.VisualStudio.Shell.SVsServiceProvider;
using SVsTaskList = Microsoft.VisualStudio.Shell.Interop.SVsTaskList;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    [Export(typeof(IOptionPersisterProvider))]
    internal sealed class CommentTaskTokenSerializerProvider : IOptionPersisterProvider
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalOptionService _optionService;
        private CommentTaskTokenSerializer? _lazyPersister;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CommentTaskTokenSerializerProvider(
            IThreadingContext threadingContext,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IGlobalOptionService optionService)
        {
            _threadingContext = threadingContext;
            _serviceProvider = serviceProvider;
            _optionService = optionService;
        }

        public IOptionPersister GetPersister()
        {
            if (_lazyPersister is not null)
            {
                return _lazyPersister;
            }

            _threadingContext.ThrowIfNotOnUIThread();

            var taskList = _serviceProvider.GetService<SVsTaskList, ITaskList>();
            _lazyPersister ??= new CommentTaskTokenSerializer(_optionService, taskList);
            return _lazyPersister;
        }
    }
}
