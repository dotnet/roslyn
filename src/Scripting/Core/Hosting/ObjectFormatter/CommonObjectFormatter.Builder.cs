// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Object pretty printer.
    /// </summary>
    public abstract partial class CommonObjectFormatter
    {
        private sealed class Builder
        {
            private readonly StringBuilder _sb;

            private readonly bool _insertEllipsis;

            private readonly BuilderOptions _options;

            private int _currentLimit;

            public Builder(BuilderOptions options, bool insertEllipsis)
            {
                _sb = new StringBuilder();
                _insertEllipsis = insertEllipsis;
                _options = insertEllipsis ? options.SubtractEllipsisLength() : options;

                _currentLimit = Math.Min(_options.LineLengthLimit, _options.TotalLengthLimit);
            }

            public bool LimitReached
            {
                get { return _sb.Length == _options.TotalLengthLimit; }
            }

            public int Remaining
            {
                get { return _options.TotalLengthLimit - _sb.Length; }
            }

            // can be negative (the min value is -Ellipsis.Length - 1)
            private int CurrentRemaining
            {
                get { return _currentLimit - _sb.Length; }
            }

            public void AppendLine()
            {
                // remove line length limit so that we can insert a new line even 
                // if the previous one hit maxed out the line limit:
                _currentLimit = _options.TotalLengthLimit;

                Append(_options.NewLine);

                // recalc limit for the next line:
                _currentLimit = (int)Math.Min((long)_sb.Length + _options.LineLengthLimit, _options.TotalLengthLimit);
            }

            private void AppendEllipsis()
            {
                if (_sb.Length > 0 && _sb[_sb.Length - 1] != ' ')
                {
                    _sb.Append(' ');
                }

                _sb.Append(_options.Ellipsis);
            }

            public void Append(char c, int count = 1)
            {
                if (CurrentRemaining < 0)
                {
                    return;
                }

                int length = Math.Min(count, CurrentRemaining);

                _sb.Append(c, length);

                if (_insertEllipsis && length < count)
                {
                    AppendEllipsis();
                }
            }

            public void Append(string str, int start = 0, int count = Int32.MaxValue)
            {
                if (str == null || CurrentRemaining < 0)
                {
                    return;
                }

                count = Math.Min(count, str.Length - start);
                int length = Math.Min(count, CurrentRemaining);
                _sb.Append(str, start, length);

                if (_insertEllipsis && length < count)
                {
                    AppendEllipsis();
                }
            }

            public void AppendFormat(string format, params object[] args)
            {
                Append(string.Format(format, args));
            }

            public void AppendGroupOpening()
            {
                Append('{');
            }

            public void AppendGroupClosing(bool inline)
            {
                if (inline)
                {
                    Append(" }");
                }
                else
                {
                    AppendLine();
                    Append('}');
                    AppendLine();
                }
            }

            public void AppendCollectionItemSeparator(bool isFirst, bool inline)
            {
                if (isFirst)
                {
                    if (inline)
                    {
                        Append(' ');
                    }
                    else
                    {
                        AppendLine();
                    }
                }
                else
                {
                    if (inline)
                    {
                        Append(", ");
                    }
                    else
                    {
                        Append(',');
                        AppendLine();
                    }
                }

                if (!inline)
                {
                    Append(_options.Indentation);
                }
            }

            /// <remarks>
            /// This is for conveying cyclic dependencies to the user, not for detecting them.
            /// </remarks>
            internal void AppendInfiniteRecursionMarker()
            {
                AppendGroupOpening();
                AppendCollectionItemSeparator(isFirst: true, inline: true);
                Append(_options.Ellipsis);
                AppendGroupClosing(inline: true);
            }

            public override string ToString()
            {
                return _sb.ToString();
            }
        }
    }
}