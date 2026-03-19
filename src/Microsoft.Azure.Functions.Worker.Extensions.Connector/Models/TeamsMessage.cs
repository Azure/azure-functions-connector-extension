// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector.Models;

/// <summary>
/// Represents a Microsoft Teams message received via webhook.
/// </summary>
public sealed class TeamsMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for this message.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the message type (e.g., "message", "messageUpdate", "messageDelete").
    /// </summary>
    [JsonPropertyName("messageType")]
    public string? MessageType { get; set; }

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the content type (e.g., "text", "html").
    /// </summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the sender information.
    /// </summary>
    [JsonPropertyName("from")]
    public TeamsMember? From { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the message was created.
    /// </summary>
    [JsonPropertyName("createdDateTime")]
    public DateTimeOffset? CreatedDateTime { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the message was last modified.
    /// </summary>
    [JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime { get; set; }

    /// <summary>
    /// Gets or sets the ID of the team containing this message.
    /// </summary>
    [JsonPropertyName("teamId")]
    public string? TeamId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the channel containing this message.
    /// </summary>
    [JsonPropertyName("channelId")]
    public string? ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the chat (for 1:1 or group chats).
    /// </summary>
    [JsonPropertyName("chatId")]
    public string? ChatId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the parent message (for replies).
    /// </summary>
    [JsonPropertyName("replyToId")]
    public string? ReplyToId { get; set; }

    /// <summary>
    /// Gets or sets the subject of the message (for channel posts).
    /// </summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the importance level (normal, high, urgent).
    /// </summary>
    [JsonPropertyName("importance")]
    public string? Importance { get; set; }

    /// <summary>
    /// Gets or sets the list of mentioned users.
    /// </summary>
    [JsonPropertyName("mentions")]
    public IList<TeamsMention>? Mentions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the message has attachments.
    /// </summary>
    [JsonPropertyName("hasAttachments")]
    public bool HasAttachments { get; set; }

    /// <summary>
    /// Gets or sets the web URL to the message in Teams.
    /// </summary>
    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; set; }
}

/// <summary>
/// Represents a Teams user or member.
/// </summary>
public sealed class TeamsMember
{
    /// <summary>
    /// Gets or sets the user's unique identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the user's Azure AD object ID.
    /// </summary>
    [JsonPropertyName("aadObjectId")]
    public string? AadObjectId { get; set; }
}

/// <summary>
/// Represents a mention in a Teams message.
/// </summary>
public sealed class TeamsMention
{
    /// <summary>
    /// Gets or sets the mention ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the mentioned text as it appears in the message.
    /// </summary>
    [JsonPropertyName("mentionText")]
    public string? MentionText { get; set; }

    /// <summary>
    /// Gets or sets the mentioned user information.
    /// </summary>
    [JsonPropertyName("mentioned")]
    public TeamsMember? Mentioned { get; set; }
}
