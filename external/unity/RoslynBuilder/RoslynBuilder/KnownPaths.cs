using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using NiceIO;

namespace RoslynBuilder
{
	class KnownPaths
	{
		private static NPath _roslynRoot;
		private static NPath _msbuild;
		private static NPath _crossgen;
		private static NPath _windows10SDK;

		public static NPath RoslynRoot
		{
			get
			{
				if (_roslynRoot != null)
					return _roslynRoot;

				var currentExecutablePath = Assembly.GetEntryAssembly().Location.ToNPath();
				_roslynRoot = currentExecutablePath.ParentContaining("Roslyn.sln");

				if (_roslynRoot == null)
					throw new DirectoryNotFoundException("Could not find roslyn root!");

				return _roslynRoot;
			}
		}

		public static NPath NuGet => RoslynRoot.Combine("external", "NuGet", "NuGet.exe");
		public static NPath DotNet => RoslynRoot.Combine("Binaries", "Tools", "dotnet", "dotnet.exe");
		public static NPath CscBinariesDirectory => RoslynRoot.Combine("Binaries", "Release", "Exes", "CscCore");
		public static NPath BuildsZipDirectory => RoslynRoot.Combine("Artifacts", "Builds");

		public static NPath MSBuild
		{
			get
			{
				if (_msbuild != null)
					return _msbuild;

				// TO DO: prioritize VS 2017 when we have that installed on the build farm
				var programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
				if (string.IsNullOrEmpty(programFiles))
				{
					programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
					if (string.IsNullOrEmpty(programFiles))
						programFiles = @"C:\Program Files (x86)";
				}

				var msbuild = programFiles.ToNPath().Combine("MSBuild", "14.0", "bin", "MSBuild.exe");
				if (!msbuild.FileExists())
					throw new FileNotFoundException($"Could not find msbuild.exe at {msbuild.Parent}!");

				_msbuild = msbuild;
				return _msbuild;
			}
		}

		public static NPath CrossGen
		{
			get
			{
				if (_crossgen == null)
					throw new Exception("Cannot retrieve CrossGen path before it was set!");

				return _crossgen;
			}
			set
			{
				_crossgen = value;
			}
		}

		public static NPath Windows10SDK
		{
			get
			{
				if (_windows10SDK != null)
					return _windows10SDK;

				_windows10SDK = GetWindows10SDKPathFromRegistry();
				if (_windows10SDK == null)
					throw new DirectoryNotFoundException("Could find Windows 10 SDK path. Is it installed?");

				return _windows10SDK;
			}
		}

		private static NPath GetWindows10SDKPathFromRegistry()
		{
			var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Microsoft SDKs\Windows\v10.0");

			var sdkDir = (string)key?.GetValue("InstallationFolder");
			if (string.IsNullOrEmpty(sdkDir))
				return null;

			return sdkDir.ToNPath();
		}
	}
}
