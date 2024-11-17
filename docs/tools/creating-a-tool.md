# Creating a tool

1. Create a new directory with your tool's name under `tools`.
1. Create a new `WinUI 3 Class Library` project in the directory you just created.
1. In your project file, remove `TargetFramework` and `TargetPlatformMinVersion`. Add the following line to the top:
    ```xml
    <Import Project="$(SolutionDir)ToolingVersions.props" />
    ```
1. Remove the PackageReference to WindowsAppSDK, since it will be added via the Common project in a few steps.
1. Create the `Strings\en-us` directories under your project directory. Add `Resources.resw` and include the following code:
    ```xml
    <data name="NavigationPane.Content" xml:space="preserve">
      <value>[Name of your tool that will appear in navigation menu]</value>
      <comment>[Extra information about the name of your tool that may help translation]</comment>
    </data>
    ```
1. Add a project reference from `DevHome.csproj` to your project
1. Add a project reference from your project to `DevHome.Common.csproj` project under [/common/](/common)
1. Create your XAML View and ViewModel. Your View class must inherit from `ToolPage` and implement [tool interface requirements](#tool-requirements).
1. Update [NavConfig.jsonc](/src/NavConfig.jsonc) with your tool. Specifications for the [NavConfig.json schema](./navconfig.md).

## Tool requirements

Each tool must define a custom page view extending from the [`ToolPage`](../../common/Views/ToolPage.cs) abstract class, and implement it like in this example:

```cs
public class SampleToolPage : ToolPage
{
    public SampleToolPage()
    {
        ViewModel = Application.Current.GetService<SampleToolViewModel>();
        InitializeComponent();
    }
}
```

If a page is not part of a tool, it should extend from [`DevHomePage.cs`](../../common/Views/DevHomePage.cs).

<!-- ### Method definition

This section contains a more detailed description of each of the interface methods.

-->

## Code organization

[`ToolPage.cs`](../../common/Views/ToolPage.cs)
Contains the interface definition for Dev Home tools.

[`DevHomePage.cs`](../../common/Views/DevHomePage.cs)
Contains the interface definition for all Dev Home pages.
