// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace Microsoft.NET.ProjectData;

public static partial class ProjectDataDonorIndex
{
	private const int GitQueryTimeoutMilliseconds = 2000;
	private const int GitCancellationPollMilliseconds = 50;

	private static int GitDistance(
		string workspaceRoot,
		string? leftHead,
		string? rightHead,
		GitQueryContext gitQueryContext)
	{
		if (string.IsNullOrEmpty(leftHead) || string.IsNullOrEmpty(rightHead))
		{
			return int.MaxValue;
		}

		if (string.Equals(leftHead, rightHead, StringComparison.Ordinal))
		{
			return 0;
		}

		string? count = RunGit(gitQueryContext, workspaceRoot, "rev-list", "--count", $"{leftHead}...{rightHead}");
		return int.TryParse(count, out int parsed) ? parsed : int.MaxValue;
	}

	internal static string GetRecipientMetadataFingerprint(string workspaceRoot, GitQueryContext gitQueryContext)
	{
		if (gitQueryContext.IsCancellationRequested)
		{
			return "git-interrupted";
		}

		string gitPath = Path.Combine(workspaceRoot, ".git");
		try
		{
			string? gitDirectory = null;
			string? commonGitDirectory = null;
			if (Directory.Exists(gitPath))
			{
				gitDirectory = gitPath;
				commonGitDirectory = gitPath;
			}
			else if (File.Exists(gitPath) && TryReadGitFile(gitPath, out string? worktreeGitDirectory))
			{
				gitDirectory = worktreeGitDirectory;
				commonGitDirectory = ResolveCommonGitDirectory(worktreeGitDirectory);
			}

			if (gitDirectory is null || commonGitDirectory is null)
			{
				return "nogit";
			}

			string headPath = Path.Combine(gitDirectory, "HEAD");
			if (gitQueryContext.IsCancellationRequested)
			{
				return "git-interrupted";
			}
			string head = File.Exists(headPath) ? File.ReadAllText(headPath).Trim() : string.Empty;
			StringBuilder fingerprint = new();
			AppendFileFingerprint(fingerprint, headPath, gitQueryContext);
			fingerprint.Append('|').Append(head);

			const string RefPrefix = "ref:";
			if (head.StartsWith(RefPrefix, StringComparison.Ordinal))
			{
				string refName = head.Substring(RefPrefix.Length).Trim();
				AppendRefFingerprint(fingerprint, gitDirectory, commonGitDirectory, refName, gitQueryContext);
			}

			AppendFileFingerprint(fingerprint, Path.Combine(commonGitDirectory, "packed-refs"), gitQueryContext);
			AppendFileFingerprint(fingerprint, Path.Combine(commonGitDirectory, "reftable", "tables.list"), gitQueryContext);
			if (!PathComparer.Equals(gitDirectory, commonGitDirectory))
			{
				AppendFileFingerprint(fingerprint, Path.Combine(gitDirectory, "reftable", "tables.list"), gitQueryContext);
			}

			if (gitQueryContext.IsCancellationRequested)
			{
				return "git-interrupted";
			}
			return fingerprint.ToString();
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			if (gitQueryContext.IsCancellationRequested)
			{
				return "git-interrupted";
			}
			return "git-unavailable|" + File.GetLastWriteTimeUtc(gitPath).Ticks.ToString();
		}
	}

	private static string? RunGit(GitQueryContext gitQueryContext, string workingDirectory, params string[] args)
	{
		if (!Directory.Exists(workingDirectory))
		{
			return null;
		}

		if (gitQueryContext.GetRemainingMilliseconds() == 0)
		{
			return null;
		}

		try
		{
			using Process process = new()
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "git",
					Arguments = string.Join(" ", args.Select(QuoteArgument)),
					WorkingDirectory = workingDirectory,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				},
			};

			if (!process.Start())
			{
				return null;
			}

			Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
			Task<string> errorTask = process.StandardError.ReadToEndAsync();
			bool exited = false;
			try
			{
				while (true)
				{
					int remainingMilliseconds = gitQueryContext.GetRemainingMilliseconds();
					if (remainingMilliseconds == 0)
					{
						return null;
					}

					if (process.WaitForExit(Math.Min(remainingMilliseconds, GitCancellationPollMilliseconds)))
					{
						exited = true;
						break;
					}
				}
			}
			finally
			{
				if (!exited)
				{
					try { process.Kill(); }
					catch (InvalidOperationException) { }
					WaitForGitOutput(100, outputTask, errorTask);
				}
			}

			WaitForGitOutput(100, outputTask, errorTask);
			string output = outputTask.Status == TaskStatus.RanToCompletion ? outputTask.Result : string.Empty;
			return process.ExitCode == 0 ? output.Trim() : null;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
		{
			return null;
		}
	}

	private static void WaitForGitOutput(int timeoutMilliseconds, params Task[] tasks)
	{
		try
		{
			if (Task.WaitAll(tasks, timeoutMilliseconds))
			{
				return;
			}
		}
		catch (AggregateException)
		{
			return;
		}

		_ = Task.WhenAll(tasks).ContinueWith(
			static completedTask => _ = completedTask.Exception,
			CancellationToken.None,
			TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
			TaskScheduler.Default);
	}

	internal sealed class GitQueryContext
	{
		private readonly Stopwatch stopwatch = Stopwatch.StartNew();
		private readonly CancellationToken cancellationToken;
		private readonly int timeoutMilliseconds;
		private bool wasCancelled;
		private bool timedOut;

		public GitQueryContext(CancellationToken cancellationToken)
			: this(cancellationToken, GitQueryTimeoutMilliseconds)
		{
		}

		internal GitQueryContext(CancellationToken cancellationToken, int timeoutMilliseconds)
		{
			if (timeoutMilliseconds < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
			}

			this.cancellationToken = cancellationToken;
			this.timeoutMilliseconds = timeoutMilliseconds;
		}

		public bool WasCancelled => Volatile.Read(ref this.wasCancelled);

		public bool TimedOut => Volatile.Read(ref this.timedOut);

		public bool WasInterrupted => this.WasCancelled || this.TimedOut;

		public int TimeoutMilliseconds => this.timeoutMilliseconds;

		public bool IsCancellationRequested
		{
			get
			{
				if (!this.cancellationToken.IsCancellationRequested)
				{
					return false;
				}

				Volatile.Write(ref this.wasCancelled, true);
				return true;
			}
		}

		public int GetRemainingMilliseconds()
		{
			if (this.IsCancellationRequested)
			{
				return 0;
			}

			long remainingMilliseconds = this.timeoutMilliseconds - this.stopwatch.ElapsedMilliseconds;
			if (remainingMilliseconds <= 0)
			{
				Volatile.Write(ref this.timedOut, true);
				return 0;
			}

			return (int)remainingMilliseconds;
		}
	}

	private static void AppendRefFingerprint(
		StringBuilder builder,
		string gitDirectory,
		string commonGitDirectory,
		string refName,
		GitQueryContext gitQueryContext)
	{
		string relativePath = refName.Replace('/', Path.DirectorySeparatorChar);
		string worktreeRefPath = Path.Combine(gitDirectory, relativePath);
		AppendFileFingerprint(
			builder,
			File.Exists(worktreeRefPath)
				? worktreeRefPath
				: Path.Combine(commonGitDirectory, relativePath),
			gitQueryContext);
	}

	private static void AppendFileFingerprint(StringBuilder builder, string filePath, GitQueryContext gitQueryContext)
	{
		if (gitQueryContext.IsCancellationRequested)
		{
			return;
		}

		builder.Append('|').Append(filePath);
		try
		{
			FileInfo fileInfo = new(filePath);
			if (fileInfo.Exists)
			{
				builder.Append(':').Append(fileInfo.LastWriteTimeUtc.Ticks).Append(':').Append(fileInfo.Length);
				return;
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
		}

		builder.Append(":missing");
	}

	private static string QuoteArgument(string argument)
	{
		if (argument.Length == 0)
		{
			return "\"\"";
		}

		bool needsQuotes = argument.Any(static ch => char.IsWhiteSpace(ch) || ch == '"');
		if (!needsQuotes)
		{
			return argument;
		}

		StringBuilder builder = new();
		builder.Append('"');
		int backslashes = 0;
		foreach (char ch in argument)
		{
			if (ch == '\\')
			{
				backslashes++;
				continue;
			}

			if (ch == '"')
			{
				builder.Append('\\', backslashes * 2 + 1);
				builder.Append('"');
				backslashes = 0;
				continue;
			}

			builder.Append('\\', backslashes);
			backslashes = 0;
			builder.Append(ch);
		}

		builder.Append('\\', backslashes * 2);
		builder.Append('"');
		return builder.ToString();
	}

}
