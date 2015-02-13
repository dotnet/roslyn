using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;

namespace Microsoft.VisualStudio.LanguageServices.Setup
{
    internal interface IRoslynTelemetrySetup
    {
        void Initialize(IServiceProvider componentModel);
    }
}
