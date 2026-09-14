// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.NET.ProjectData.Tests;

[Collection(DotnetRootEnvCollection.Name)]
public sealed class UnsupportedProjectDataMarkerTests : IDisposable
{
	private readonly string workDir;
	private readonly string previousCacheRoot;

	public UnsupportedProjectDataMarkerTests()
	{
		this.workDir = Path.Combine(Path.GetTempPath(), "unsupported-projectdata-marker-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(this.workDir);
		this.previousCacheRoot = Environment.GetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR") ?? string.Empty;
		Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", Path.Combine(this.workDir, "cache"));
	}

	public void Dispose()
	{
		Environment.SetEnvironmentVariable("DOTNET_PROJECTDATA_CACHE_DIR", string.IsNullOrEmpty(this.previousCacheRoot) ? null : this.previousCacheRoot);
		try
		{
			Directory.Delete(this.workDir, recursive: true);
		}
		catch
		{
		}
	}

	[Fact]
	public void Write_CreatesValidMarkerBesideUserCachePath()
	{
		string projectFile = this.WriteProject("App.csproj");

		string markerPath = UnsupportedProjectDataMarker.Write(projectFile, "CannotWriteProjectData");

		Assert.EndsWith(".unsupported", markerPath);
		Assert.StartsWith(Path.Combine(this.workDir, "cache"), markerPath);
		Assert.True(UnsupportedProjectDataMarker.TryReadValid(projectFile, TestContext.Current.CancellationToken, out UnsupportedProjectDataMarkerData marker));
		Assert.Equal("CannotWriteProjectData", marker.Reason);
		Assert.Equal(markerPath, marker.MarkerFilePath);
	}

	[Fact]
	public void TryReadValid_ReturnsFalseWhenProjectChanges()
	{
		string projectFile = this.WriteProject("App.csproj");
		UnsupportedProjectDataMarker.Write(projectFile, "CannotWriteProjectData");

		File.AppendAllText(projectFile, Environment.NewLine);

		Assert.False(UnsupportedProjectDataMarker.TryReadValid(projectFile, TestContext.Current.CancellationToken, out _));
	}

	[Fact]
	public void TryReadValid_ReturnsFalseWhenAncestorInputChanges()
	{
		string projectFile = this.WriteProject("src/App/App.csproj");
		string directoryBuildProps = Path.Combine(this.workDir, "Directory.Build.props");
		File.WriteAllText(directoryBuildProps, "<Project />");
		UnsupportedProjectDataMarker.Write(projectFile, "CannotWriteProjectData");

		File.WriteAllText(directoryBuildProps, "<Project><PropertyGroup><LangVersion>preview</LangVersion></PropertyGroup></Project>");

		Assert.False(UnsupportedProjectDataMarker.TryReadValid(projectFile, TestContext.Current.CancellationToken, out _));
	}

	[Fact]
	public void TryReadValid_CanceledBeforeValidation_Throws()
	{
		string projectFile = this.WriteProject("App.csproj");
		UnsupportedProjectDataMarker.Write(projectFile, "CannotWriteProjectData");

		Assert.Throws<OperationCanceledException>(() =>
			UnsupportedProjectDataMarker.TryReadValid(
				projectFile,
				new CancellationToken(canceled: true),
				out _));
	}

	[Fact]
	public void Delete_RemovesMarker()
	{
		string projectFile = this.WriteProject("App.csproj");
		string markerPath = UnsupportedProjectDataMarker.Write(projectFile, "CannotWriteProjectData");

		UnsupportedProjectDataMarker.Delete(projectFile);

		Assert.False(File.Exists(markerPath));
	}

	private string WriteProject(string relativePath)
	{
		string projectFile = Path.Combine(this.workDir, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(projectFile)!);
		File.WriteAllText(
			projectFile,
			"""
			<Project Sdk="Microsoft.NET.Sdk">
			  <PropertyGroup>
			    <TargetFramework>net8.0</TargetFramework>
			  </PropertyGroup>
			</Project>
			""");
		return projectFile;
	}
}
