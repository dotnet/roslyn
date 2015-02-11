using System.ComponentModel.Composition;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    [Export(typeof(IInteractiveWindowCommand))]
    internal sealed class ClearScreenCommand : InteractiveWindowCommand
    {
        public override Task<ExecutionResult> Execute(IInteractiveWindow window, string arguments)
        {
            window.Operations.ClearView();
            return ExecutionResult.Succeeded;
        }

        public override string Description
        {
            get { return "Clears the contents of the REPL editor window, leaving history and execution context intact."; }
        }

        public override string Name
        {
            get { return "cls"; }
        }
    }
}
