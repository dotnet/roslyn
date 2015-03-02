// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly IContentTypeRegistryService _contentTypeRegistry;
        private readonly IStandardClassificationService _standardClassification;

        [ImportingConstructor]
        public InteractiveCommandsFactory(IContentTypeRegistryService contentTypeRegistry, IStandardClassificationService classification)
        {
            _contentTypeRegistry = contentTypeRegistry;
            _standardClassification = classification;
        }

        public IInteractiveWindowCommands CreateInteractiveCommands(IInteractiveWindow window, string prefix, IEnumerable<IInteractiveWindowCommand> commands)
        {
            return new Commands(window, prefix, commands.ToArray(), _contentTypeRegistry, _standardClassification);
        }
    }
}
