using Xunit;

namespace Metalama.Compiler.UnitTests;

public class TransformedPathGeneratorTests
{
    const string projectDirectory = "c:\\MySolution\\MyProject";
    const string outputDirectory = "c:\\MySolution\\MyProject\\obj\\Debug\\metalama";

    [Theory]
    [InlineData("MyFile.cs", $"{outputDirectory}\\MyFile.cs")]
    [InlineData("SubDirectory\\MyFile.cs", $"{outputDirectory}\\SubDirectory\\MyFile.cs")]
    [InlineData("..\\MyFile.cs", $"{outputDirectory}\\links\\MyFile.cs")]
    [InlineData("c:\\MyFile.cs", $"{outputDirectory}\\links\\MyFile.cs")]
    public void TestOne(string inputPath, string expectedOutput)
    {
        var generator = new TransformedPathGenerator(projectDirectory, outputDirectory, projectDirectory);

        Assert.Equal(expectedOutput, generator.GetOutputPath(inputPath));
    }

    [Fact]
    public void Duplicate()
    {
        var generator = new TransformedPathGenerator(projectDirectory, outputDirectory, projectDirectory);

        Assert.Equal($"{outputDirectory}\\links\\MyFile.cs", generator.GetOutputPath("c:\\MyFile.cs"));

        // Same.
        Assert.Equal($"{outputDirectory}\\links\\MyFile_2.cs", generator.GetOutputPath("c:\\MyFile.cs"));

        // Case difference.
        Assert.Equal($"{outputDirectory}\\links\\myfile_3.cs", generator.GetOutputPath("c:\\myfile.cs"));
    }
}
