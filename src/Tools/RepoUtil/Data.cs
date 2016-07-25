using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    class Data
    {
        /// <summary>
        /// The dependencies in these project.json files are expected to be regularly updated by 
        /// repo full stack builds.
        /// </summary>
        private static readonly string[] s_floatingList = new[]
        {
            @"build\MSBuildToolset\project.json",
            @"build\ToolsetPackages\project.json",
            @"src\Compilers\Core\MSBuildTask\Desktop\project.json",
            @"src\Compilers\Core\MSBuildTask\Portable\project.json",
            @"src\Compilers\Core\MSBuildTaskTests\project.json",
            @"src\Compilers\CSharp\csc\project.json",
            @"src\Compilers\Extension\project.json",
            @"src\Compilers\Server\VBCSCompilerTests\project.json",
            @"src\Compilers\VisualBasic\vbc\project.json",
            @"src\Dependencies\Composition\project.json",
            @"src\Dependencies\DiaSymReader\project.json",
            @"src\Dependencies\DiaSymReader.PortablePdb\project.json",
            @"src\Dependencies\Immutable\project.json",
            @"src\Dependencies\Metadata\project.json",
            @"src\Dependencies\Moq.net\project.json",
            @"src\Dependencies\VisualStudio\project.json",
            @"src\Dependencies\VisualStudioEditor\project.json",
            @"src\Dependencies\VisualStudioText\project.json",
            @"src\Dependencies\xUnit.net\project.json",
            @"src\EditorFeatures\Core\project.json",
            @"src\EditorFeatures\Next\project.json",
            @"src\EditorFeatures\Test\project.json",
            @"src\ExpressionEvaluator\Core\Source\ResultProvider\NetFX20\project.json",
            @"src\ExpressionEvaluator\Core\Test\ExpressionCompiler\project.json",
            @"src\ExpressionEvaluator\Core\Test\ResultProvider\project.json",
            @"src\ExpressionEvaluator\CSharp\Source\ResultProvider\NetFX20\project.json",
            @"src\ExpressionEvaluator\CSharp\Test\ExpressionCompiler\project.json",
            @"src\ExpressionEvaluator\VisualBasic\Source\ResultProvider\NetFX20\project.json",
            @"src\InteractiveWindow\EditorTest\project.json",
            @"src\InteractiveWindow\VisualStudio\project.json",
            @"src\Scripting\Core\project.json",
            @"src\Scripting\CSharp\project.json",
            @"src\Scripting\CSharpTest\project.json",
            @"src\Scripting\CSharpTest.Desktop\project.json",
            @"src\Scripting\VisualBasic\project.json",
            @"src\Scripting\VisualBasicTest\project.json",
            @"src\Test\DeployCoreClrTestRuntime\project.json",
            @"src\Test\PdbUtilities\project.json",
            @"src\Test\Perf\Runner\project.json",
            @"src\Test\Utilities\Desktop\project.json",
            @"src\Test\Utilities\Portable\project.json",
            @"src\Test\Utilities\Portable.FX45\project.json",
            @"src\VisualStudio\Core\Def\project.json",
            @"src\VisualStudio\Core\Impl\project.json",
            @"src\VisualStudio\Core\SolutionExplorerShim\project.json",
            @"src\VisualStudio\Setup\project.json",
            @"src\VisualStudio\TestSetup\project.json",
            @"src\Workspaces\Core\Desktop\project.json",
            @"src\Workspaces\Core\Portable\project.json",
        };

        /// <summary>
        /// The dependencies listed in this file are not expected to change on repo builds.  Only when 
        /// the tool itself needs to be updated.
        /// </summary>
        private static readonly string[] s_staticList = new[]
        {
            @"src\Tools\BuildUtil\BuildUtil\project.json",
            @"src\Tools\CommonCoreClrRuntime\project.json",
            @"src\Tools\CommonNetCoreReferences\project.json",
            @"src\Tools\ProcessWatchdog\project.json",
            @"src\Tools\RepoUtil\project.json",
            @"src\Tools\SignRoslyn\project.json",
            @"src\Tools\Source\CompilerGeneratorTools\DeployCompilerGeneratorToolsRuntime\project.json",
            @"src\Tools\Source\CompilerGeneratorTools\Source\BoundTreeGenerator\project.json",
            @"src\Tools\Source\CompilerGeneratorTools\Source\CSharpErrorFactsGenerator\project.json",
            @"src\Tools\Source\CompilerGeneratorTools\Source\CSharpSyntaxGenerator\project.json",
            @"src\Tools\Source\CompilerGeneratorTools\Source\VisualBasicErrorFactsGenerator\project.json",
            @"src\Tools\Source\CompilerGeneratorTools\Source\VisualBasicSyntaxGenerator\project.json",
            @"src\Tools\Source\RunTests\project.json",
        };

        internal static readonly ImmutableArray<string> FloatingList = ImmutableArray.Create(s_floatingList);
        internal static readonly ImmutableArray<string> StaticList = ImmutableArray.Create(s_staticList);

        internal static ImmutableArray<FileName> GetFloatingFileNames(string sourcesPath)
        {
            return s_floatingList.Select(x => new FileName(sourcesPath, x)).ToImmutableArray();
        }
    }
}
