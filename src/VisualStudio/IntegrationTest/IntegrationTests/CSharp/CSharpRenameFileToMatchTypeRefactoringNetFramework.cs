using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    public class CSharpRenameFileToMatchTypeRefactoringNetFramework : CSharpRenameFileToMatchTypeRefactoringBase
    {
        public CSharpRenameFileToMatchTypeRefactoringNetFramework(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper) 
            : base(instanceFactory, testOutputHelper, WellKnownProjectTemplates.ClassLibrary)
        {
        }
    }
}
