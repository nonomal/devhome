﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using HyperVExtension.HostGuestCommunication;

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class used to handle response for service state (RequestType = GetState).
/// </summary>
internal sealed class GetStateResponse : ResponseBase
{
    public GetStateResponse(IResponseMessage responseMessage, JsonNode jsonData)
        : base(responseMessage, jsonData)
    {
        StateData = GetRequiredValue<StateData>(nameof(StateData));
    }

    public StateData StateData { get; }
}
