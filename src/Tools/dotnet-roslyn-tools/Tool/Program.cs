using System.CommandLine;
using Microsoft.RoslynTools.Commands;

return await RootRoslynCommand.GetRootCommand().InvokeAsync(args);
