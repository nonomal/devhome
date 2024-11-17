﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow.Environments;

public enum EnvironmentsTelemetryStatus
{
    Started,
    Succeeded,
    Failed,
    Unknown,
    NoOperation, // Used when there is a no op kind of operation and no work was done.
}

[EventData]
public class EnvironmentCreationEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public string ProviderId { get; }

    public string Status { get; }

    public string? DisplayMessage { get; }

    public string? DiagnosticText { get; }

    public int HResult { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentCreationEvent"/> class.
    /// </summary>
    /// <param name="providerId">The Id of the compute system provider that initiated the creation operation</param>
    /// <param name="status">The status of the creation operation</param>
    /// <param name="result">Associated telemetry result for the operation</param>
    public EnvironmentCreationEvent(
        string providerId,
        EnvironmentsTelemetryStatus status,
        TelemetryResult result)
    {
        ProviderId = providerId;
        Status = status.ToString();
        HResult = result.HResult;
        DisplayMessage = result.DisplayMessage;
        DiagnosticText = result.DiagnosticText;
    }

    // Inherited but unused.
    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
    }
}
