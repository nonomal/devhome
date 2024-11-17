﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle request for service version (RequestType = GetVersion).
/// </summary>
internal sealed class AckRequest : RequestBase
{
    public const string RequestTypeId = "Ack";

    public AckRequest(IRequestContext requestContext)
        : base(requestContext)
    {
        AckRequestId = GetRequiredStringValue(nameof(AckRequestId));
    }

    private string AckRequestId { get; }

    public override bool IsStatusRequest => true;

    public override IHostResponse Execute(IProgressHandler progressHandler, CancellationToken stoppingToken)
    {
        Task.Run(
            () =>
            {
                RequestContext.HostChannel.DeleteResponseMessageAsync(AckRequestId, stoppingToken);
            },
            stoppingToken);

        return new AckResponse(RequestMessage.CommunicationId!);
    }
}
