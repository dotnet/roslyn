using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Services.Formatting;

namespace Roslyn.Services.Formatting.Options
{
    internal interface IFormattingOptionsFactoryService : IWorkspaceService
    {
        FormattingOptions GetFormattingOptions(Document document);
    }
}
