﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using DevHome.Common.Services;
using DevHome.Customization.Helpers;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Win32;
using WinRT;

namespace DevHome.Customization.Models;

public class SourceControlIntegration
{
    private static readonly Serilog.ILogger _log = Serilog.Log.ForContext("SourceContext", nameof(Models.SourceControlIntegration));
    private static readonly StringResource _stringResource = new("DevHome.Customization.pri", "DevHome.Customization/Resources");

    public static SourceControlValidationResult ValidateSourceControlExtension(string extensionCLSID, string rootPath)
    {
        var providerPtr = IntPtr.Zero;
        try
        {
            _log.Information("Validating source control extension with arguments: extensionCLSID = {extensionCLSID}, rootPath = {rootPath}", extensionCLSID, rootPath);

            var hr = PInvoke.CoCreateInstance(Guid.Parse(extensionCLSID), null, Windows.Win32.System.Com.CLSCTX.CLSCTX_LOCAL_SERVER, typeof(ILocalRepositoryProvider).GUID, out var extensionObj);
            providerPtr = Marshal.GetIUnknownForObject(extensionObj);
            if (hr < 0)
            {
                _log.Error(hr.ToString(), "Failure occurred while creating instance of repository provider");
                return new SourceControlValidationResult(ResultType.Failure, ErrorType.RepositoryProviderCreationFailed, null, _stringResource.GetLocalized("ValidateSourceControlErrorOnRepositoryProviderInstanceCreation"), null);
            }

            ILocalRepositoryProvider provider = MarshalInterface<ILocalRepositoryProvider>.FromAbi(providerPtr);
            GetLocalRepositoryResult result = provider.GetRepository(rootPath);

            if (result.Result.Status == ProviderOperationStatus.Failure)
            {
                Log.Error("Could not open local repository.");
                Log.Error(result.Result.DisplayMessage);
                return new SourceControlValidationResult(ResultType.Failure, ErrorType.OpenRepositoryFailed, result.Result.ExtendedError, result.Result.DisplayMessage, result.Result.DiagnosticText);
            }
            else
            {
                _log.Information("Local repository opened successfully.");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "An exception occurred while validating source control extension.");
            return new SourceControlValidationResult(ResultType.Failure, ErrorType.SourceControlExtensionValidationFailed, ex, _stringResource.GetLocalized("ValidateSourceControlErrorOnGetRepository", ex.Message), null);
        }
        finally
        {
            if (providerPtr != IntPtr.Zero)
            {
                Marshal.Release(providerPtr);
            }
        }

        return new SourceControlValidationResult();
    }
}
