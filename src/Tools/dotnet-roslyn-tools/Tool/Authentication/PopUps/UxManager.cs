// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.RoslynTools.Authentication.PopUps
{
    internal class UxManager
    {
        private readonly string _editorPath;
        private readonly ILogger _logger;
        private bool _popUpClosed = false;

        public UxManager(string gitLocation, ILogger logger)
        {
            _editorPath = LocalHelpers.GetEditorPath(gitLocation, logger);
            _logger = logger;
        }

        /// <summary>
        ///     Rather than popping up the window, read the result of the popup from
        ///     stdin and process the contents.  This is primarily used for testing purposes.
        /// </summary>
        /// <param name="popUp">Popup to run</param>
        /// <returns>Success or error code</returns>
        public int ReadFromStdIn(EditorPopUp popUp)
        {
            int result;

            try
            {
                // File to write from stdin to, which will be processed by the popup closing handler
                var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), popUp.Path);
                var dirPath = Path.GetDirectoryName(path)!;

                Directory.CreateDirectory(dirPath);
                using (var streamWriter = new StreamWriter(path))
                {
                    string? line;
                    while ((line = Console.ReadLine()) != null)
                    {
                        streamWriter.WriteLineAsync(line);
                    }
                }

                // Now run the closed event and process the contents
                var contents = EditorPopUp.OnClose(path);
                result = popUp.ProcessContents(contents);
                Directory.Delete(dirPath, true);
                if (result != Constants.SuccessCode)
                {
                    _logger.LogError("Inputs were invalid.");
                }

                return result;
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "There was an exception processing YAML input from stdin.");
                result = Constants.ErrorCode;
            }

            return result;
        }

        /// <summary>
        ///     Pop up the editor and allow the user to edit the contents.
        /// </summary>
        /// <param name="popUp">Popup to run</param>
        /// <returns>Success or error code</returns>
        public int PopUp(EditorPopUp popUp)
        {
            if (string.IsNullOrEmpty(_editorPath))
            {
                _logger.LogError("Failed to define an editor for the pop ups. Please verify that your git settings (`git config core.editor`) specify the path correctly.");
                return Constants.ErrorCode;
            }

            var result = Constants.ErrorCode;
            var tries = Constants.MaxPopupTries;

            var parsedCommand = GetParsedCommand(_editorPath);

            try
            {
                var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), popUp.Path);
                var dirPath = Path.GetDirectoryName(path)!;

                Directory.CreateDirectory(dirPath);
                File.WriteAllLines(path, popUp.Contents.Select(l => l.Text));

                while (tries-- > 0 && result != Constants.SuccessCode)
                {
                    using var process = new Process();

                    _popUpClosed = false;
                    process.EnableRaisingEvents = true;
                    process.Exited += (sender, e) =>
                    {
                        var contents = EditorPopUp.OnClose(path);

                        result = popUp.ProcessContents(contents);

                        // If succeeded, delete the temp file, otherwise keep it around
                        // for another popup iteration.
                        if (result == Constants.SuccessCode)
                        {
                            Directory.Delete(dirPath, true);
                        }
                        else if (tries > 0)
                        {
                            _logger.LogError("Inputs were invalid, please try again...");
                        }
                        else
                        {
                            Directory.Delete(dirPath, true);
                            _logger.LogError("Maximum number of tries reached, aborting.");
                        }

                        _popUpClosed = true;
                    };
                    process.StartInfo.FileName = parsedCommand.FileName;
                    process.StartInfo.UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    process.StartInfo.Arguments = $"{parsedCommand.Arguments} {path}";
                    process.Start();

                    var waitForMilliseconds = 100;
                    while (!_popUpClosed)
                    {
                        Thread.Sleep(waitForMilliseconds);
                    }
                }
            }
            catch (Win32Exception exc)
            {
                _logger.LogError(exc, "Cannot start editor '{FileName}'. Please verify that your git settings (`git config core.editor`) specify the path correctly.", parsedCommand.FileName);
                result = Constants.ErrorCode;
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"There was an exception while trying to pop up an editor window.");
                result = Constants.ErrorCode;
            }

            return result;
        }

        public static ParsedCommand GetParsedCommand(string command)
        {
            string fileName;
            var arguments = string.Empty;

            // If it's quoted then find the end of the quoted string.
            // If non quoted find a space or the end of the string.
            command = command.Trim();
            if (command.StartsWith("'") || command.StartsWith("\""))
            {
                var start = 1;
                var end = command.IndexOf("'", start);
                if (end == -1)
                {
                    end = command.IndexOf("\"", start);
                    if (end == -1)
                    {
                        // Unterminated quoted string.  Use full command as file name
                        fileName = command[1..];
                        return new(fileName, arguments);
                    }
                }
                fileName = command[start..end];
                arguments = command[(end + 1)..];
                return new(fileName, arguments);
            }
            else
            {
                // Find a space after the command name, if there are args, then parse them out,
                // otherwise just return the whole string as the filename.
                var fileNameEnd = command.IndexOf(" ");
                if (fileNameEnd != -1)
                {
                    fileName = command[..fileNameEnd];
                    arguments = command[fileNameEnd..];
                }
                else
                {
                    fileName = command;
                }
                return new(fileName, arguments);
            }
        }
    }

    /// <summary>
    /// Process needs the file name and the arguments splitted apart. This represent these two.
    /// </summary>
    internal record ParsedCommand(string FileName, string Arguments)
    {
    }
}
