using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Test.Utilities.Commands
{
    internal static class CommandHandlerExtensions
    {
        public static void ExecuteCommand<T>(this ICommandHandler handler, T args, Func<T, bool> successor) where T : CommandArgs
        {
            var typedHandler = (ICommandHandler<T>)handler;
            if (!typedHandler.ExecuteCommand(args))
            {
                successor(args);
            }
        }
            
        public static void ExecuteCommand<T>(this ICommandHandler handler, T args, Func<bool> successor) where T : CommandArgs
        {
            var typedHandler = (ICommandHandler<T>)handler;
            if (!typedHandler.ExecuteCommand(args))
            {
                successor();
            }
        }

        public static void ExecuteCommand<T>(this ICommandHandler handler, T args, Action<T> successor) where T : CommandArgs
        {
            var typedHandler = (ICommandHandler<T>)handler;
            if (!typedHandler.ExecuteCommand(args))
            {
                successor(args);
            }
        }
    }
}
