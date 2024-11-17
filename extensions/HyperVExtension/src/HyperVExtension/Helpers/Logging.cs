﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Storage;

namespace HyperVExtension.Helpers;

public class Logging
{
    public static readonly string LogExtension = ".dhlog";

    public static readonly string LogFolderName = "Logs";

    public static readonly string DefaultLogFileName = "hyperv";

    private static readonly Lazy<string> _logFolderRoot = new(() => Path.Combine(ApplicationData.Current.TemporaryFolder.Path, LogFolderName));

    public static readonly string LogFolderRoot = _logFolderRoot.Value;
}
