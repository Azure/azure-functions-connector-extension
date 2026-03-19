// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector.Models;

/// <summary>
/// Represents a SharePoint list item or document received via webhook.
/// </summary>
public sealed class SharePointItem
{
    /// <summary>
    /// Gets or sets the unique identifier for this item.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the item's display name or title.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the item title (for list items).
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the type of change that triggered the webhook.
    /// </summary>
    [JsonPropertyName("changeType")]
    public string? ChangeType { get; set; }

    /// <summary>
    /// Gets or sets the site ID where the item is located.
    /// </summary>
    [JsonPropertyName("siteId")]
    public string? SiteId { get; set; }

    /// <summary>
    /// Gets or sets the site URL.
    /// </summary>
    [JsonPropertyName("siteUrl")]
    public string? SiteUrl { get; set; }

    /// <summary>
    /// Gets or sets the web ID within the site.
    /// </summary>
    [JsonPropertyName("webId")]
    public string? WebId { get; set; }

    /// <summary>
    /// Gets or sets the list ID containing the item.
    /// </summary>
    [JsonPropertyName("listId")]
    public string? ListId { get; set; }

    /// <summary>
    /// Gets or sets the list title.
    /// </summary>
    [JsonPropertyName("listTitle")]
    public string? ListTitle { get; set; }

    /// <summary>
    /// Gets or sets the document library ID (for files).
    /// </summary>
    [JsonPropertyName("libraryId")]
    public string? LibraryId { get; set; }

    /// <summary>
    /// Gets or sets the file path relative to the site.
    /// </summary>
    [JsonPropertyName("filePath")]
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets the file name with extension.
    /// </summary>
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    [JsonPropertyName("fileSize")]
    public long? FileSize { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the file.
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    /// <summary>
    /// Gets or sets the ETag for concurrency control.
    /// </summary>
    [JsonPropertyName("eTag")]
    public string? ETag { get; set; }

    /// <summary>
    /// Gets or sets the user who created the item.
    /// </summary>
    [JsonPropertyName("createdBy")]
    public SharePointUser? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the item was created.
    /// </summary>
    [JsonPropertyName("createdDateTime")]
    public DateTimeOffset? CreatedDateTime { get; set; }

    /// <summary>
    /// Gets or sets the user who last modified the item.
    /// </summary>
    [JsonPropertyName("lastModifiedBy")]
    public SharePointUser? LastModifiedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the item was last modified.
    /// </summary>
    [JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime { get; set; }

    /// <summary>
    /// Gets or sets the web URL to view the item in SharePoint.
    /// </summary>
    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; set; }

    /// <summary>
    /// Gets or sets additional field values for list items.
    /// </summary>
    [JsonPropertyName("fields")]
    public IDictionary<string, object>? Fields { get; set; }
}

/// <summary>
/// Represents a SharePoint user.
/// </summary>
public sealed class SharePointUser
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
    /// Gets or sets the user's login name.
    /// </summary>
    [JsonPropertyName("loginName")]
    public string? LoginName { get; set; }
}
