// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel;

internal sealed partial class MockTextManagerAdapter
{
    internal sealed class TextPoint : EnvDTE.TextPoint
    {
        private readonly VirtualTreePoint _point;

        public TextPoint(VirtualTreePoint point)
        {
            _point = point;
        }

        public int AbsoluteCharOffset
        {
            get { return _point.Position; }
        }

        public bool AtEndOfDocument
        {
            get { return _point.Position == _point.Text.Length; }
        }

        public bool AtEndOfLine
        {
            get { return _point.Position == _point.GetContainingLine().End; }
        }

        public bool AtStartOfDocument
        {
            get { return _point.Position == 0; }
        }

        public bool AtStartOfLine
        {
            get { return _point.Position == _point.GetContainingLine().Start; }
        }

        public EnvDTE.EditPoint CreateEditPoint()
        {
            throw new NotImplementedException();
        }

        public EnvDTE.DTE DTE
        {
            get { throw new NotImplementedException(); }
        }

        public int DisplayColumn
        {
            get { throw new NotImplementedException(); }
        }

        public bool EqualTo(EnvDTE.TextPoint point)
        {
            return AbsoluteCharOffset == point.AbsoluteCharOffset;
        }

        public bool GreaterThan(EnvDTE.TextPoint point)
        {
            return AbsoluteCharOffset > point.AbsoluteCharOffset;
        }

        public bool LessThan(EnvDTE.TextPoint point)
        {
            return AbsoluteCharOffset < point.AbsoluteCharOffset;
        }

        public int Line
        {
            get
            {
                // These line numbers start at 1!
                return _point.GetContainingLine().LineNumber + 1;
            }
        }

        public int LineCharOffset
        {
            get
            {
                var result = _point.Position - _point.GetContainingLine().Start + 1;
                if (_point.IsInVirtualSpace)
                {
                    result += _point.VirtualSpaces;
                }

                return result;
            }
        }

        public int LineLength
        {
            get
            {
                var line = _point.GetContainingLine();
                return line.End - line.Start;
            }
        }

        public EnvDTE.TextDocument Parent
        {
            get { return CreateEditPoint().Parent; }
        }

        public bool TryToShow(EnvDTE.vsPaneShowHow how, object pointOrCount)
        {
            throw new NotImplementedException();
        }

        public EnvDTE.CodeElement get_CodeElement(EnvDTE.vsCMElement scope)
        {
            throw new NotImplementedException();
        }
    }
}
