using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Services.Host
{
    /// <summary>
    /// factory service that returns option service for the given document
    /// </summary>
    internal interface IOptionServiceFactoryService : IWorkspaceService
    {
        IOptionService GetOptionService(IDocument document);
    }
}
