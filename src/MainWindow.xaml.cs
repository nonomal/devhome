// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Helpers;
using DevHome.Telemetry;
using DevHome.TelemetryEvents;
using Microsoft.UI.Xaml;

namespace DevHome;

public sealed partial class MainWindow : WindowEx
{
    private readonly DateTime mainWindowCreated;

    public MainWindow()
    {
        InitializeComponent();

#if CANARY_BUILD
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/DevHome_Canary.ico"));
#else
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/DevHome.ico"));
#endif

        Content = null;
        Title = Application.Current.GetService<IAppInfoService>().GetAppNameLocalized();
        mainWindowCreated = DateTime.UtcNow;
    }

    private void MainWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        Application.Current.GetService<IExtensionService>().SignalStopExtensionsAsync();
        TelemetryFactory.Get<ITelemetry>().Log("DevHome_MainWindow_Closed_Event", LogLevel.Critical, new DevHomeClosedEvent(mainWindowCreated));
    }
}
