﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;

namespace HyperVExtension.Models;

/// <summary>
/// Wrapper class for interacting with the PowerShell class in the System.Management.Automation
/// assembly.
/// </summary>
public class PowerShellSession : IPowerShellSession, IDisposable
{
    private readonly PowerShell _powerShellSession;
    private bool _disposedValue;

    public PowerShellSession()
    {
        _powerShellSession = PowerShell.Create();
    }

    /// <inheritdoc cref="IPowerShellSession.AddCommand(string)"/>
    public void AddCommand(string command)
    {
        _powerShellSession.AddCommand(command);
    }

    /// <inheritdoc cref="IPowerShellSession.AddParameters(IDictionary)"/>
    public void AddParameters(IDictionary parameters)
    {
        _powerShellSession.AddParameters(parameters);
    }

    /// <inheritdoc cref="IPowerShellSession.AddScript(string)"/>
    public void AddScript(string script, bool useLocalScope)
    {
        _powerShellSession.AddScript(script, useLocalScope);
    }

    /// <inheritdoc cref="IPowerShellSession.Invoke"/>
    public Collection<PSObject> Invoke()
    {
        return _powerShellSession.Invoke();
    }

    /// <inheritdoc cref="IPowerShellSession.ClearSession"/>
    public void ClearSession()
    {
        _powerShellSession.Commands.Clear();
        _powerShellSession.Streams.ClearStreams();
    }

    /// <inheritdoc cref="IPowerShellSession.GetErrorMessages"/>
    public string GetErrorMessages()
    {
        if (_powerShellSession.Streams.Error.Count > 0)
        {
            List<string> errors = new List<string>();
            for (int i = 0; i < _powerShellSession.Streams.Error.Count; i++)
            {
                var exception = _powerShellSession.Streams.Error[i].Exception;
                errors.Add($"{exception.Message} (0x{exception.HResult.ToString("X", CultureInfo.InvariantCulture)})");
            }

            return string.Join(Environment.NewLine, errors);
        }

        return string.Empty;
    }

    /// <inheritdoc cref="IPowerShellSession.GetErrorFirstHResult"/>
    public int GetErrorFirstHResult()
    {
        if (_powerShellSession.Streams.Error.Count > 0)
        {
            return _powerShellSession.Streams.Error[0].Exception.HResult;
        }

        return 0;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _powerShellSession.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
