using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynBuilder
{
	interface IBuildStep
	{
		void Execute();
	}
}
