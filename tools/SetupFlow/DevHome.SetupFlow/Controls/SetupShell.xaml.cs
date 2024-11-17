// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace DevHome.SetupFlow.Controls;

/// <summary>
/// Setup shell class used by the setup flow pages to ensure a consistent
/// end-to-end page layout.
/// </summary>
[ContentProperty(Name = nameof(SetupShellContent))]
public sealed partial class SetupShell : UserControl
{
    public string Title
    {
        get
        {
            var title = (string)GetValue(TitleProperty);
            return string.IsNullOrEmpty(title) ? Orchestrator.FlowTitle : title;
        }
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public object Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object SetupShellContent
    {
        get => (object)GetValue(SetupShellContentProperty);
        set => SetValue(SetupShellContentProperty, value);
    }

    public object SetupShellNotification
    {
        get => (object)GetValue(SetupShellNotificationProperty);
        set => SetValue(SetupShellNotificationProperty, value);
    }

    public SetupFlowOrchestrator Orchestrator
    {
        get => (SetupFlowOrchestrator)GetValue(OrchestratorProperty);
        set => SetValue(OrchestratorProperty, value);
    }

    public Visibility HeaderVisibility
    {
        get => (Visibility)GetValue(HeaderVisibilityProperty);
        set => SetValue(HeaderVisibilityProperty, value);
    }

    public Visibility ContentVisibility
    {
        get => (Visibility)GetValue(ContentVisibilityProperty);
        set => SetValue(ContentVisibilityProperty, value);
    }

    public Visibility SetupShellNotificationVisibility
    {
        get => (Visibility)GetValue(SetupShellNotificationVisibilityProperty);
        set => SetValue(SetupShellNotificationVisibilityProperty, value);
    }

    public SetupShell()
    {
        this.InitializeComponent();
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.RegisterAttached(nameof(Title), typeof(string), typeof(SetupShell), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.RegisterAttached(nameof(Description), typeof(string), typeof(SetupShell), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty SetupShellContentProperty = DependencyProperty.RegisterAttached(nameof(SetupShellContent), typeof(object), typeof(SetupShell), new PropertyMetadata(null));
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.RegisterAttached(nameof(Header), typeof(object), typeof(SetupShell), new PropertyMetadata(null));
    public static readonly DependencyProperty OrchestratorProperty = DependencyProperty.RegisterAttached(nameof(Orchestrator), typeof(SetupFlowOrchestrator), typeof(SetupShell), new PropertyMetadata(null));
    public static readonly DependencyProperty HeaderVisibilityProperty = DependencyProperty.RegisterAttached(nameof(HeaderVisibility), typeof(Visibility), typeof(SetupShell), new PropertyMetadata(Visibility.Visible));
    public static readonly DependencyProperty ContentVisibilityProperty = DependencyProperty.RegisterAttached(nameof(ContentVisibility), typeof(Visibility), typeof(SetupShell), new PropertyMetadata(Visibility.Visible));

    public static readonly DependencyProperty SetupShellNotificationProperty = DependencyProperty.RegisterAttached(nameof(SetupShellNotification), typeof(object), typeof(SetupShell), new PropertyMetadata(null));

    public static readonly DependencyProperty SetupShellNotificationVisibilityProperty = DependencyProperty.RegisterAttached(nameof(SetupShellNotificationVisibility), typeof(Visibility), typeof(SetupShell), new PropertyMetadata(Visibility.Collapsed));
}
