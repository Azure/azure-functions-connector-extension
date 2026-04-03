// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Connector;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(ConnectorStartup))]

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// WebJobs startup class that registers the Connector extension.
/// The [WebJobsStartup] assembly attribute ensures this is called during host initialization.
/// </summary>
internal sealed class ConnectorStartup : IWebJobsStartup
{
    public void Configure(IWebJobsBuilder builder)
    {
        builder.AddConnector();
    }
}
