// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector.Models;

/// <summary>
/// Represents an Office 365 email message received via webhook.
/// </summary>
public sealed class O365Email
{
    /// <summary>
    /// Gets or sets the unique identifier for this email.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the email subject line.
    /// </summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the sender's email address.
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; set; }

    /// <summary>
    /// Gets or sets the sender's display name.
    /// </summary>
    [JsonPropertyName("fromDisplayName")]
    public string? FromDisplayName { get; set; }

    /// <summary>
    /// Gets or sets the list of recipient email addresses.
    /// </summary>
    [JsonPropertyName("to")]
    public IList<string>? To { get; set; }

    /// <summary>
    /// Gets or sets the list of CC recipient email addresses.
    /// </summary>
    [JsonPropertyName("cc")]
    public IList<string>? Cc { get; set; }

    /// <summary>
    /// Gets or sets the email body content.
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>
    /// Gets or sets the body content type (e.g., "text", "html").
    /// </summary>
    [JsonPropertyName("bodyContentType")]
    public string? BodyContentType { get; set; }

    /// <summary>
    /// Gets or sets the body preview (first 255 characters).
    /// </summary>
    [JsonPropertyName("bodyPreview")]
    public string? BodyPreview { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the email was received.
    /// </summary>
    [JsonPropertyName("receivedDateTime")]
    public DateTimeOffset? ReceivedDateTime { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the email was sent.
    /// </summary>
    [JsonPropertyName("sentDateTime")]
    public DateTimeOffset? SentDateTime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the email has attachments.
    /// </summary>
    [JsonPropertyName("hasAttachments")]
    public bool HasAttachments { get; set; }

    /// <summary>
    /// Gets or sets the importance level (low, normal, high).
    /// </summary>
    [JsonPropertyName("importance")]
    public string? Importance { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the email has been read.
    /// </summary>
    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets the conversation ID for threading.
    /// </summary>
    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the web link to view the email in Outlook.
    /// </summary>
    [JsonPropertyName("webLink")]
    public string? WebLink { get; set; }
}
