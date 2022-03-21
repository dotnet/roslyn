﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Microsoft.VisualStudio.LanguageServices.Utilities
{
    internal class DelegateCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        private readonly Func<object, bool> _canExecute;
        private readonly Action<object> _action;

        private bool _lastCanExecute;

        public DelegateCommand(Action<object> action)
            : this(action, (_) => true)
        {
        }

        public DelegateCommand(Action<object> action, Func<object, bool> canExecute)
        {
            _canExecute = canExecute;
            _action = action;
        }

        public bool CanExecute(object parameter)
        {
            var canExecute = _canExecute(parameter);
            if (canExecute != _lastCanExecute)
            {
                _lastCanExecute = canExecute;
                CanExecuteChanged?.Invoke(this, new EventArgs());
            }

            return canExecute;
        }

        public void Execute(object parameter) => _action(parameter);
    }
}
