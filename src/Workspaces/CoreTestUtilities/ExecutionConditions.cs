using System;
using System.IO;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    internal static class PathHelper
    {
        public static bool NetFrameworkReferenceAssembliesFolderExists(string name)
        {
            // Windows-only for now
            if (Path.DirectorySeparatorChar != '\\')
            {
                return false;
            }

            var programFilesX86Folder = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
            if (programFilesX86Folder == null)
            {
                return false;
            }

            var netFramework40Folder = Path.Combine(programFilesX86Folder, "Reference Assemblies", "Microsoft", "Framework", ".NETFramework", name);

            return Directory.Exists(netFramework40Folder);
        }
    }

    public class RequiresNetFramework40 : ExecutionCondition
    {
        private static readonly bool s_pathExists = PathHelper.NetFrameworkReferenceAssembliesFolderExists("v4.0");

        public override bool ShouldSkip => !s_pathExists;

        public override string SkipReason => "Test not supported with .NET Framework 4.0 reference assemblies are missing.";
    }

    public class RequiresNetFramework45 : ExecutionCondition
    {
        private static readonly bool s_pathExists = PathHelper.NetFrameworkReferenceAssembliesFolderExists("v4.5");

        public override bool ShouldSkip => !s_pathExists;

        public override string SkipReason => "Test not supported with .NET Framework 4.0 reference assemblies are missing.";
    }
}
