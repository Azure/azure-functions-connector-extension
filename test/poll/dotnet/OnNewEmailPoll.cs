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
            PollingEndpoint = "%OnNewEmailEndpoint%",
            IsBatched = true,
            MaxBatchSize = 4,
            Concurrency = 4)]
        ConnectorEvent<Office365OnNewEmailTriggerPayload>[] emails)
    {
        foreach (ConnectorEvent<Office365OnNewEmailTriggerPayload> email in emails)
        {
            logger.LogInformation(
                "Poll email trigger message {MessageId} received: {Payload}",
                email.MessageId,
                System.Text.Json.JsonSerializer.Serialize(email.Data));
        }
    }
}
