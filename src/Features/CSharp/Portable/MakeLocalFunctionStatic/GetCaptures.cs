using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic
{
    internal class MakeLocalFunctionStaticService: ILanguageService
    {
        
        public Task<Solution> CreateParameterSymbol(Document document, LocalFunctionStatementSyntax localfunction, CancellationToken cancellationToken)
        {



        }
    }




}
