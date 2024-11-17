﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Services.WindowsPackageManager.Models;
using DevHome.Services.WindowsPackageManager.Services;

namespace DevHome.Services.WindowsPackageManager.Contracts;

/// <summary>
/// Interface for interacting with the WinGet package manager.
/// More details: https://github.com/microsoft/winget-cli/blob/master/src/Microsoft.Management.Deployment/PackageManager.idl
/// </summary>
public interface IWinGet
{
    public const string AppInstallerProductId = WinGetDeployment.AppInstallerProductId;
    public const int AppInstallerErrorFacility = WinGetDeployment.AppInstallerErrorFacility;

    /// <summary>
    /// Initialize the winget package manager.
    /// </summary>
    public Task InitializeAsync();

    /// <inheritdoc cref="IWinGetOperations.InstallPackageAsync"/>
    public Task<IWinGetInstallPackageResult> InstallPackageAsync(WinGetPackageUri packageUri, Guid activityId);

    /// <inheritdoc cref="IWinGetOperations.GetPackagesAsync"/>
    public Task<IList<IWinGetPackage>> GetPackagesAsync(IList<WinGetPackageUri> packageUris);

    /// <inheritdoc cref="IWinGetOperations.SearchAsync"/>
    public Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit);

    /// <inheritdoc cref="IWinGetDeployment.IsUpdateAvailableAsync"/>
    public Task<bool> IsUpdateAvailableAsync();

    /// <inheritdoc cref="IWinGetDeployment.IsAvailableAsync"/>
    public Task<bool> IsAvailableAsync();

    /// <inheritdoc cref="IWinGetDeployment.RegisterAppInstallerAsync"/>
    public Task<bool> RegisterAppInstallerAsync();

    /// <inheritdoc cref="IWinGetCatalogConnector.IsMsStorePackage"/>
    public bool IsMsStorePackage(IWinGetPackage package);

    /// <inheritdoc cref="IWinGetCatalogConnector.IsWinGetPackage"/>
    public bool IsWinGetPackage(IWinGetPackage package);

    /// <inheritdoc cref="IWinGetProtocolParser.CreatePackageUri"/>
    public WinGetPackageUri CreatePackageUri(IWinGetPackage package);

    /// <inheritdoc cref="IWinGetProtocolParser.CreateWinGetCatalogPackageUri"/>
    public WinGetPackageUri CreateWinGetCatalogPackageUri(string packageId);

    /// <inheritdoc cref="IWinGetProtocolParser.CreateMsStoreCatalogPackageUri"/>
    public WinGetPackageUri CreateMsStoreCatalogPackageUri(string packageId);

    /// <inheritdoc cref="IWinGetProtocolParser.CreateCustomCatalogPackageUri"/>
    public WinGetPackageUri CreateCustomCatalogPackageUri(string packageId, string catalogName);
}
