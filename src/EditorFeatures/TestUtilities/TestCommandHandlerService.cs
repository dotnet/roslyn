using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CommandHandlers;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [Export(typeof(ICommandHandlerService))]
    class TestCommandHandlerService : ICommandHandlerService
    {
        List<ICommandHandler> _handlers;
        private readonly SignatureHelpCommandHandler signatureHelpCommandHandler;

        public CompletionCommandHandler completionCommandHandler { get; }

        public TestCommandHandlerService(SignatureHelpCommandHandler signatureHelpCommandHandler, CompletionCommandHandler completionCommandHandler)
        {
            this.signatureHelpCommandHandler = signatureHelpCommandHandler;
            this.completionCommandHandler = completionCommandHandler;
        }

        public bool Execute<T>(IContentType contentType, ITextViewRoleSet textViewRoles, bool isReadOnlyBuffer, T args) where T : CommandArgs => throw new NotImplementedException();
        public bool ExecuteHandler<T>(string commandHandlerName, T args) where T : CommandArgs => throw new NotImplementedException();
        public CommandState GetCommandState<T>(IContentType contentType, ITextViewRoleSet textViewRoles, bool isReadonlyBuffer, T args) where T : CommandArgs => throw new NotImplementedException();

        private class MockEditorCommandHandler : ICommandHandler
        {

        }

        public void AddHandlers(params ICommandHandler[] handlers)
        {
            this._handlers.AddRange(handlers);
        }
    }
}
