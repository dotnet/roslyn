// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MSBuildWorkspaceTester.Services;

namespace MSBuildWorkspaceTester.ViewModels
{
    internal class LogViewModel : PageViewModel<UserControl>
    {
        private readonly OutputService _outputService;

        public LogViewModel(IServiceProvider serviceProvider)
            : base("LogView", serviceProvider)
        {
            _outputService = serviceProvider.GetRequiredService<OutputService>();
            _outputService.TextChanged += delegate { PropertyChanged("OutputText"); };

            Caption = "Log";
        }

        public string OutputText => _outputService.GetText();
    }
}
