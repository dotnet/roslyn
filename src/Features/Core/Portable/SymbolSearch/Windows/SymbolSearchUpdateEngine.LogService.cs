using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;
using static System.FormattableString;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal partial class SymbolSearchUpdateEngine
    {
        private sealed static class LogService
        {
        }
    }
}
