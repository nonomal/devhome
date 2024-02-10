﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle requests that have no request type.
/// It creates an error response JSON to send back to the client.
/// </summary>
internal sealed class ErrorNoTypeResponse : ResponseBase
{
    public ErrorNoTypeResponse(string requestId)
        : base(requestId)
    {
        Status = 0xFFFFFFFF;
        GenerateJsonData();
    }

    public string Error => "Missing Request type.";

    protected override void GenerateJsonData()
    {
        base.GenerateJsonData();
        JsonData![nameof(Error)] = Error;
    }
}