using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    [Export(typeof(IInteractiveWindowCommandsFactory))]
    internal class InteractiveCommandsFactory : IInteractiveWindowCommandsFactory
    {
        private readonly IContentTypeRegistryService contentTypeRegistry;
        private readonly IStandardClassificationService standardClassification;

        [ImportingConstructor]
        public InteractiveCommandsFactory(IContentTypeRegistryService contentTypeRegistry, IStandardClassificationService classification)
        {
            this.contentTypeRegistry = contentTypeRegistry;
            this.standardClassification = classification;
        }

        public IInteractiveWindowCommands CreateInteractiveCommands(IInteractiveWindow window, string prefix, IEnumerable<IInteractiveWindowCommand> commands)
        {
            return new Commands(window, prefix, commands.ToArray(), contentTypeRegistry, standardClassification);
        }
    }
}
