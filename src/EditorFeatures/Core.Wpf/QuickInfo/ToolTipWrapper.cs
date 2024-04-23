// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    /// <summary>
    /// Wraps and truncates text for consumption in a tooltip.
    /// </summary>
    /// <typeparam name="TTargetType">Target tooltip type.</typeparam>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ToolTipWrapper{TTargetType}"/> class.
    /// </remarks>
    /// <param name="target">Target tooltip.</param>
    /// <param name="lineIndentSize">Set the indent size of each line on the target.</param>
    internal abstract class ToolTipWrapper<TTargetType>(TTargetType target, uint? lineIndentSize = 0)
    {
        private const uint DefaultMaxLines = 10;
        private const uint DefaultMaxWidth = 120;
        private const int DefaultLineIndentSpace = 0;

        private readonly TTargetType _target = target;
        private readonly StringBuilder _currentLine = new();
        private readonly StringBuilder _currentWord = new();
        private readonly uint _lineIndentSize = lineIndentSize ?? DefaultLineIndentSpace;

        private uint _maxLines;
        private uint _maxWidth;
        private int _currentLines;

        /// <summary>
        /// Gets a string of spaces of the size of lineIndentSpace.
        /// </summary>
        private string LineIndent => new(' ', (int)_lineIndentSize);

        /// <summary>
        /// Gets the target object.
        /// </summary>
        protected TTargetType Target => _target;

        /// <summary>
        /// Gets a value indicating whether the last line of the target is empty.
        /// </summary>
        /// <returns>Value indicating whether the last line of the target is empty.</returns>
        protected abstract bool IsLastLineEmpty { get; }

        /// <summary>
        /// Truncates and trims a string and adds it to the collection.
        /// </summary>
        /// <param name="text">Text to evaluate.</param>
        /// <param name="maxLines">Maximum number of lines for the tooltip. Optional.</param>
        /// <param name="maxWidth">Maximum number of characters per line. Optional.</param>
        public void WrapAndTruncate(string text, uint? maxLines = null, uint? maxWidth = null)
        {
            this._maxLines = maxLines ?? DefaultMaxLines;
            this._maxWidth = maxWidth ?? DefaultMaxWidth;
            this._maxWidth -= _lineIndentSize;
            // Add indentation for the first line
            _currentLine.Clear();
            _currentWord.Clear();
            _currentLines = 0;
            var trimmedText = text.Trim();
            var inWhitespace = false;

            for (var i = 0; i < trimmedText.Length; i++)
            {
                var c = trimmedText[i];

                // Special case newlines so that we can respect current formatting
                if (IsNewlineCharacter(c))
                {
                    // First try using TryAppendLine to wrap the contents if needed
                    if (TryAppendLine(out var limitReached) && limitReached)
                    {
                        return;
                    }

                    // Write the remaining contents of the line
                    if (_currentLine.Length > 0)
                    {
                        AddNewLineIfNeeded();
                        AddLine(LineIndent + _currentLine.ToString());
                        _currentLine.Clear();
                        _currentLines++;
                    }

                    if (AppendEllipsisIfNeeded())
                    {
                        return;
                    }

                    // Move the counter forward in cases of \r\n to avoid appending 2 newlines
                    if (c == '\r' && i + 1 < trimmedText.Length && trimmedText[i + 1] == '\n')
                    {
                        i++;
                    }

                    // Continue so that the newline is not added to currentWord
                    continue;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (!inWhitespace)
                    {
                        if (TryAppendLine(out var limitReached) && limitReached)
                        {
                            return;
                        }
                    }

                    inWhitespace = true;
                }
                else
                {
                    inWhitespace = false;
                }

                _currentWord.Append(c);
            }

            if (_currentWord.Length > 0 || _currentLine.Length > 0)
            {
                // Process the last set of data
                TryAppendLine(out var limitReached);

                // Write the last line if needed
                if (!limitReached && _currentLine.Length > 0)
                {
                    if (_currentLines > 0)
                    {
                        AddNewline();
                    }

                    AddLine(LineIndent + _currentLine.ToString());
                }
            }
        }

        /// <summary>
        /// Adds a line of text to the target.
        /// </summary>
        /// <param name="line">Line of text to add.</param>
        protected abstract void AddLine(string line);

        /// <summary>
        /// Adds an ellipsis to the target.
        /// </summary>
        protected abstract void AddEllipsis();

        /// <summary>
        /// Adds a new line to the target.
        /// </summary>
        protected abstract void AddNewline();

        /// <summary>
        /// Appends a line to the elements if the <see name="currentWord"/> would force it to be outside the
        /// max width. Otherwise it adds the <see name="currentWord"/> to the <see name="currentLine"/>.
        /// </summary>
        /// <param name="limitReached">Value indicating whether the limit of lines has been reached.</param>
        /// <returns>Value indicating whether <see name="currentLine"/> was added to elements.</returns>
        private bool TryAppendLine(
            out bool limitReached)
        {
            limitReached = false;
            if (_currentLine.Length + _currentWord.Length >= _maxWidth)
            {
                if (_currentLine.Length == 0)
                {
                    // Special case for words bigger than maxWidth. Add the current
                    // word after trimming any whitespace
                    _currentLine.Append(_currentWord.ToString().Trim());
                    _currentWord.Clear();
                }

                AddNewLineIfNeeded();
                AddLine(LineIndent + _currentLine.ToString());
                _currentLine.Clear();

                _currentLine.Append(_currentWord.ToString().TrimStart());
                _currentLines++;
                limitReached = AppendEllipsisIfNeeded();

                _currentWord.Clear();
                return true;
            }
            else
            {
                _currentLine.Append(_currentWord);
                _currentWord.Clear();
                return false;
            }
        }

        /// <summary>
        /// Adds a new line if there are other lines in the collection and the previous line was not empty.
        /// </summary>
        private void AddNewLineIfNeeded()
        {
            if (_currentLines > 0 && !IsLastLineEmpty)
            {
                AddNewline();
            }
        }

        /// <summary>
        /// Adds an ellipsis if the the line limit has been reached.
        /// </summary>
        /// <returns>Value indicating whether the ellipsis was added.</returns>
        private bool AppendEllipsisIfNeeded()
        {
            if (_currentLines >= _maxLines)
            {
                // Reached the limit, add the ellipsis and stop processing
                AddNewline();
                AddEllipsis();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the character is a new line character.
        /// </summary>
        /// <param name="c">Character to evaluate.</param>
        /// <returns>Value indicating whether the character is a new line character.</returns>
        private static bool IsNewlineCharacter(char c)
        {
            switch (c)
            {
                case '\n': // Line Feed
                case '\r': // Carriage Return
                case '\u0085': // Next Line
                case '\u2028': // Line Separator
                case '\u2029': // Paragraph Separator
                    return true;
                default:
                    return false;
            }
        }
    }
}
