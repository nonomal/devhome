﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle requests that have no request type. It creates an error response to send back to the client.
/// </summary>
internal sealed class ErrorNoTypeRequest : ErrorRequest
{
    public ErrorNoTypeRequest(IRequestMessage requestMessage)
        : base(requestMessage)
    {
    }

    public override string RequestType => "ErrorNoType";

    public override IHostResponse Execute(IProgressHandler progressHandler, CancellationToken stoppingToken)
    {
        return new ErrorNoTypeResponse(RequestId);
    }
}
