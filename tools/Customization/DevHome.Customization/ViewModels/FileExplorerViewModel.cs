﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Controls;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Common.Windows.FileDialog;
using DevHome.Customization.Helpers;
using DevHome.Customization.Models;
using DevHome.Customization.TelemetryEvents;
using DevHome.FileExplorerSourceControlIntegration.Services;
using FileExplorerSourceControlIntegration;
using Microsoft.Internal.Windows.DevHome.Helpers;
using Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.Storage;

namespace DevHome.Customization.ViewModels;

public partial class FileExplorerViewModel : ObservableObject
{
    private readonly ShellSettings _shellSettings;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ObservableCollection<RepositoryInformation> TrackedRepositories { get; } = new();

    private RepositoryTracking RepoTracker { get; set; } = new(null);

    private readonly string _unassigned = "00000000-0000-0000-0000-000000000000";

    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(FileExplorerViewModel));

    public IExperimentationService ExperimentationService { get; }

    public IExtensionService ExtensionService { get; }

    public static ILocalSettingsService? LocalSettingsService { get; set; }

    public bool IsFeatureEnabled => ExperimentationService.IsFeatureEnabled("FileExplorerSourceControlIntegration") && ExtraFolderPropertiesWrapper.IsSupported();

    private readonly StringResource _stringResource = new("DevHome.Customization.pri", "DevHome.Customization/Resources");

    public FileExplorerViewModel(IExperimentationService experimentationService, IExtensionService extensionService, ILocalSettingsService localSettingsService)
    {
        _shellSettings = new ShellSettings();
        ExperimentationService = experimentationService;
        ExtensionService = extensionService;
        LocalSettingsService = localSettingsService;

        Breadcrumbs =
        [
            new(_stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!),
            new(_stringResource.GetLocalized("FileExplorer_Header"), typeof(FileExplorerViewModel).FullName!)
        ];
        RefreshTrackedRepositories();
    }

    public void RefreshTrackedRepositories()
    {
        if (ExperimentationService.IsFeatureEnabled("FileExplorerSourceControlIntegration"))
        {
            TrackedRepositories.Clear();
            var repoCollection = RepoTracker.GetAllTrackedRepositories();
            for (int position = 0; position < repoCollection.Count; position++)
            {
                var data = repoCollection.ElementAt(position);
                TrackedRepositories.Add(new RepositoryInformation(data.Key, data.Value, position + 1, repoCollection.Count));
            }
        }
    }

    public bool ShowFileExtensions
    {
        get => FileExplorerSettings.ShowFileExtensionsEnabled();
        set
        {
            SettingChangedEvent.Log("ShowFileExtensions", value.ToString());
            FileExplorerSettings.SetShowFileExtensionsEnabled(value);
        }
    }

    public bool ShowHiddenAndSystemFiles
    {
        get => FileExplorerSettings.ShowHiddenAndSystemFilesEnabled();
        set
        {
            SettingChangedEvent.Log("ShowHiddenAndSystemFiles", value.ToString());
            FileExplorerSettings.SetShowHiddenAndSystemFilesEnabled(value);
        }
    }

    public bool ShowFullPathInTitleBar
    {
        get => FileExplorerSettings.ShowFullPathInTitleBarEnabled();
        set
        {
            SettingChangedEvent.Log("ShowFullPathInTitleBar", value.ToString());
            FileExplorerSettings.SetShowFullPathInTitleBarEnabled(value);
        }
    }

    public bool ShowEmptyDrives
    {
        get => _shellSettings.ShowEmptyDrivesEnabled();
        set
        {
            SettingChangedEvent.Log("ShowEmptyDrives", value.ToString());
            _shellSettings.SetShowEmptyDrivesEnabled(value);
        }
    }

    public bool ShowFilesAfterExtraction
    {
        get => _shellSettings.ShowFilesAfterExtractionEnabled();
        set
        {
            SettingChangedEvent.Log("ShowFilesAfterExtraction", value.ToString());
            _shellSettings.SetShowFilesAfterExtractionEnabled(value);
        }
    }

    public bool EndTaskOnTaskBarEnabled
    {
        get => _shellSettings.EndTaskOnTaskBarEnabled();
        set
        {
            SettingChangedEvent.Log("EndTaskOnTaskBarEnabled", value.ToString());
            _shellSettings.SetEndTaskOnTaskBarEnabled(value);
        }
    }

    public bool IsVersionControlIntegrationEnabled
    {
        get => CalculateEnabled("VersionControlIntegration");
        set => OnToggledVersionControlIntegrationSettingAsync(value);
    }

    public bool ShowVersionControlInformation
    {
        get => CalculateEnabled("ShowVersionControlInformation");
        set => OnToggledVersionControlInformationSettingAsync(value);
    }

    public bool ShowRepositoryStatus
    {
        get => CalculateEnabled("ShowRepositoryStatus");
        set => OnToggledRepositoryStatusSettingAsync(value);
    }

    [RelayCommand]
    public async Task<string> AddFolderClick(object sender)
    {
        StorageFolder? repoRootFolder = null;
        if (IsFeatureEnabled)
        {
            await Task.Run(async () =>
            {
                using var folderDialog = new WindowOpenFolderDialog();

                try
                {
                    repoRootFolder = await folderDialog.ShowAsync(Application.Current.GetService<Window>());
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Error occurred when selecting a folder for adding a repository.");
                }

                if (repoRootFolder != null && repoRootFolder.Path.Length > 0)
                {
                    _log.Information($"Selected '{repoRootFolder.Path}' as location to register");
                    RepoTracker.AddRepositoryPath(_unassigned, repoRootFolder.Path);
                }
                else
                {
                    _log.Information("Didn't select a location to register");
                }
            });
            RefreshTrackedRepositories();
            AdjustFocus(sender);
        }

        return repoRootFolder == null ? string.Empty : repoRootFolder.Path;
    }

    public void AddRepositoryAlreadyOnMachine(string path)
    {
        RepoTracker.AddRepositoryPath(_unassigned, path);
        RefreshTrackedRepositories();
    }

    public void RemoveTrackedRepositoryFromDevHome(string rootPath)
    {
        ExtraFolderPropertiesWrapper.Unregister(rootPath);
        RepoTracker.RemoveRepositoryPath(rootPath);
        RefreshTrackedRepositories();
    }

    public async Task<SourceControlValidationResult> AssignSourceControlProviderToRepository(IExtensionWrapper? extension, string rootPath)
    {
        var result = await Task.Run(() =>
        {
            var extensionCLSID = extension?.ExtensionClassId ?? string.Empty;
            var result = SourceControlIntegration.ValidateSourceControlExtension(extensionCLSID, rootPath);
            if (result.Result == ResultType.Failure)
            {
                _log.Error("Failed to validate source control extension");
                return new SourceControlValidationResult(ResultType.Failure, result.Error, result.Exception, result.DisplayMessage, result.DiagnosticText);
            }

            try
            {
                var wrapperResult = ExtraFolderPropertiesWrapper.Register(rootPath, typeof(SourceControlProvider).GUID);
                if (!wrapperResult.Succeeded)
                {
                    _log.Error(wrapperResult.ExtendedError, "Failed to register folder for source control integration");
                    return new SourceControlValidationResult(ResultType.Failure, ErrorType.RegistrationWithFileExplorerFailed, wrapperResult.ExtendedError, _stringResource.GetLocalized("RegistrationErrorWithFileExplorer"), null);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "An exception occurred while registering folder for File Explorer source control integration");
                return new SourceControlValidationResult(ResultType.Failure, ErrorType.RegistrationWithFileExplorerFailed, ex, _stringResource.GetLocalized("RegistrationErrorWithFileExplorer"), null);
            }

            RepoTracker.ModifySourceControlProviderForTrackedRepository(extensionCLSID, rootPath);
            return new SourceControlValidationResult();
        });
        RefreshTrackedRepositories();
        return result;
  }

    public bool CalculateEnabled(string settingName)
    {
        if (LocalSettingsService!.HasSettingAsync(settingName).Result)
        {
            return LocalSettingsService.ReadSettingAsync<bool>(settingName).Result;
        }

        // Settings disabled by default
        return false;
    }

    public async void OnToggledVersionControlIntegrationSettingAsync(bool value)
    {
        await LocalSettingsService!.SaveSettingAsync("VersionControlIntegration", value);

        if (!value)
        {
            _log.Information("The user has disabled version control integration inside Dev Home");
            ExtraFolderPropertiesWrapper.UnregisterAllForCurrentApp();
            _log.Information("Unregistered all repositories in File Explorer as setting is disabled");
        }
        else
        {
            _log.Information("The user has enabled version control integration in Dev Home.");
            var repoCollection = RepoTracker.GetAllTrackedRepositories();
            foreach (var repo in repoCollection)
            {
                ExtraFolderPropertiesWrapper.Register(repo.Key, typeof(SourceControlProvider).GUID);
            }

            _log.Information("Dev Home has restored registration for enhanced repositories it is aware about");
        }
    }

    public async void OnToggledVersionControlInformationSettingAsync(bool value)
    {
        if (!value)
        {
            _log.Information("The user has disabled display of version control information in File Explorer");
        }

        await LocalSettingsService!.SaveSettingAsync("ShowVersionControlInformation", value);
    }

    public async void OnToggledRepositoryStatusSettingAsync(bool value)
    {
        if (!value)
        {
            _log.Information("The user has disabled display or repository status in File Explorer");
        }

        await LocalSettingsService!.SaveSettingAsync("ShowRepositoryStatus", value);
    }

    private void AdjustFocus(object sender)
    {
        var addRepositoryCard = sender as SettingsCard;
        if (addRepositoryCard != null)
        {
            addRepositoryCard.IsTabStop = true;
            _log.Debug($"AddRepositoryCard IsEnabled: {addRepositoryCard.IsEnabled}");
            _log.Debug($"AddRepositoryCard Visibility: {addRepositoryCard.Visibility}");
            bool isFocusSet = addRepositoryCard.Focus(FocusState.Keyboard);
            _log.Information($"Set focus to add reposiotry card result: {isFocusSet}");
        }
    }

    public void UnassignSourceControlProviderFromRepository(string repositoryRootPath)
    {
        ExtraFolderPropertiesWrapper.Unregister(repositoryRootPath);
        RepoTracker.ModifySourceControlProviderForTrackedRepository(_unassigned, repositoryRootPath);
        RefreshTrackedRepositories();
    }
}
