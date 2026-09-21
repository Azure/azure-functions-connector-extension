// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Connectors.Sdk.Office365.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Microsoft.Extensions.Logging;

namespace ConnectorPollSample;

public sealed class OnNewEmailPoll(ILogger<OnNewEmailPoll> logger)
{
    [Function("OnNewEmailPoll")]
    public void Run(
        [ConnectorTrigger(
            DeliveryMode = ConnectorTriggerDeliveryMode.Poll,
            Connection = "ConnectorNamespace",
            TriggerConfigName = "%ConnectorTriggerConfigName%",
            MaxBatchSize = 1,
            Concurrency = 4)]
        Office365OnNewEmailTriggerPayload payload)
    {
        logger.LogInformation(
            "Poll email trigger payload received: {Payload}",
            System.Text.Json.JsonSerializer.Serialize(payload));
    }
}
