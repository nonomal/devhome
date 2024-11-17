﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WSLExtension.Models;

namespace WSLExtension.Contracts;

/// <summary>
/// Interface used to create processes throughout the WSL extension.
/// </summary>
public interface IProcessCreator
{
    /// <summary>
    /// Creates and starts a new process that opens in a new Window. Note:
    /// The process is started and the method does not wait until the process
    /// has exited before returning.
    /// </summary>
    /// <param name="fileName">The name of the executable to start</param>
    /// <param name="arguments">The arguments that will be passed to the executable at process startup</param>
    public void CreateProcessWithWindow(string fileName, string arguments);

    /// <summary>
    /// Creates and starts a new process without opening a window. Note: execution is blocked
    /// until the process exits.
    /// </summary>
    /// <param name="fileName">The name of the executable to start</param>
    /// <param name="arguments">The arguments that will be passed to the executable at process startup</param>
    /// <returns>The meta data associated with the exited process.
    /// E.g StdOutput, StdError and its exit code.
    /// </returns>
    public WslProcessData CreateProcessWithoutWindowAndWaitForExit(string fileName, string arguments);
}
