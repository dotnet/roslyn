// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Roslyn.Hosting.Diagnostics.PerfMargin;

/// <summary>
/// Interaction logic for StatusIndicator.xaml
/// </summary>
public partial class StatusIndicator : UserControl
{
    private readonly ActivityLevel _activityLevel;
    private bool _changedSinceLastUpdate;

    internal StatusIndicator(ActivityLevel activityLevel)
    {
        InitializeComponent();

        _activityLevel = activityLevel;
        _changedSinceLastUpdate = activityLevel.IsActive;
    }

    // Don't use WPF one way binding since it allocates too much memory for this high-frequency event
    internal void Subscribe()
    {
        _activityLevel.IsActiveChanged += this.ActivityLevel_IsActiveChanged;
    }

    internal void Unsubscribe()
    {
        _activityLevel.IsActiveChanged -= this.ActivityLevel_IsActiveChanged;
    }

    private void ActivityLevel_IsActiveChanged(object sender, EventArgs e)
    {
        _changedSinceLastUpdate = true;
    }

    private const double MinimumScale = 0.2;
    private static readonly DoubleAnimation s_growAnimation = new(1.0, new Duration(TimeSpan.FromSeconds(1.0)), FillBehavior.HoldEnd);
    private static readonly DoubleAnimation s_shrinkAnimation = new(0.0, new Duration(TimeSpan.FromSeconds(0.33333)), FillBehavior.HoldEnd);

    public void UpdateOnUIThread()
    {
        if (!_changedSinceLastUpdate)
        {
            return;
        }

        _changedSinceLastUpdate = false;

        // Remove existing animation
        this.clipScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);

        // For very short durations, the growth animation hasn't even begun yet, so make
        // sure something is visible.
        this.clipScale.ScaleX = Math.Max(this.clipScale.ScaleX, MinimumScale);

        var anim = _activityLevel.IsActive ? s_growAnimation : s_shrinkAnimation;
        this.clipScale.BeginAnimation(ScaleTransform.ScaleXProperty, anim, HandoffBehavior.SnapshotAndReplace);
    }
}
