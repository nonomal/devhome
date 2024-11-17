﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Extensions;

public static class StackedNotificationsBehaviorExtensions
{
    public static void ShowWithWindowExtension(
        this StackedNotificationsBehavior behavior,
        string title,
        string message,
        InfoBarSeverity severity,
        IRelayCommand? command = null,
        string? buttonContent = null)
    {
        var dispatcherQueue = Application.Current.GetService<DispatcherQueue>();

        dispatcherQueue?.EnqueueAsync(() =>
        {
            var notificationToShow = new Notification
            {
                Title = title,
                Severity = severity,
            };

            // Create a stack panel to hold the message and button
            // A custom control is needed to allow text selection in the message
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, -15, 0, 20),
                Spacing = 10,
            };

            stackPanel.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.WrapWholeWords,
                IsTextSelectionEnabled = true,
            });

            if (command != null)
            {
                // Make the command parameter the notification so RelayCommands can reference the notification in case they need
                // to close it within the command.
                stackPanel.Children.Add(new Button
                {
                    Content = buttonContent,
                    Command = command,
                    CommandParameter = notificationToShow,
                });
            }

            notificationToShow.Content = stackPanel;
            behavior.Show(notificationToShow);
        });
    }

    public static void RemoveWithWindowExtension(this StackedNotificationsBehavior behavior, Notification notification)
    {
        var dispatcherQueue = Application.Current.GetService<DispatcherQueue>();

        dispatcherQueue?.EnqueueAsync(() =>
        {
            behavior.Remove(notification);
        });
    }

    public static void ClearWithWindowExtension(this StackedNotificationsBehavior behavior)
    {
        var dispatcherQueue = Application.Current.GetService<Window>().DispatcherQueue;

        dispatcherQueue.EnqueueAsync(() =>
        {
            behavior.Clear();
        });
    }
}
