// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.ViewManagement;

namespace DevHome.SetupFlow.Views;

public sealed partial class SummaryView : UserControl
{
    private readonly UISettings _uiSettings = new();

    private const double BaseWidth = 550;

    public SummaryView()
    {
        this.InitializeComponent();

        // Setting MinWidth on the UniformGrid prevents text clipping in "Next Steps".
        // Setting MinWidth on the "Left Side" stack panel keeps the MinWidth but still clips
        // when the window becomes too small.
        var textScale = _uiSettings.TextScaleFactor;
        _uiSettings.TextScaleFactorChanged += HandleTextScaleFactorChanged;
        ParentUniformGrid.MinWidth = BaseWidth * textScale;
    }

    public SummaryViewModel ViewModel => (SummaryViewModel)this.DataContext;

    private void HandleTextScaleFactorChanged(UISettings sender, object args)
    {
        Application.Current.GetService<DispatcherQueue>().EnqueueAsync(() =>
        {
            var textScale = sender.TextScaleFactor;
            ParentUniformGrid.MinWidth = BaseWidth * textScale;
            InvalidateMeasure();
        });
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _uiSettings.TextScaleFactorChanged -= HandleTextScaleFactorChanged;
    }
}
