// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector.Models;

/// <summary>
/// Represents a GitHub webhook event payload.
/// </summary>
public sealed class GitHubEvent
{
    /// <summary>
    /// Gets or sets the action that triggered the webhook (e.g., "opened", "closed", "created").
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// Gets or sets the sender (user who triggered the event).
    /// </summary>
    [JsonPropertyName("sender")]
    public GitHubUser? Sender { get; set; }

    /// <summary>
    /// Gets or sets the repository information.
    /// </summary>
    [JsonPropertyName("repository")]
    public GitHubRepository? Repository { get; set; }

    /// <summary>
    /// Gets or sets the organization information (if applicable).
    /// </summary>
    [JsonPropertyName("organization")]
    public GitHubOrganization? Organization { get; set; }

    /// <summary>
    /// Gets or sets the push event details (for push events).
    /// </summary>
    [JsonPropertyName("pusher")]
    public GitHubPusher? Pusher { get; set; }

    /// <summary>
    /// Gets or sets the ref that was pushed (e.g., "refs/heads/main").
    /// </summary>
    [JsonPropertyName("ref")]
    public string? Ref { get; set; }

    /// <summary>
    /// Gets or sets the SHA before the push.
    /// </summary>
    [JsonPropertyName("before")]
    public string? Before { get; set; }

    /// <summary>
    /// Gets or sets the SHA after the push.
    /// </summary>
    [JsonPropertyName("after")]
    public string? After { get; set; }

    /// <summary>
    /// Gets or sets the list of commits (for push events).
    /// </summary>
    [JsonPropertyName("commits")]
    public IList<GitHubCommit>? Commits { get; set; }

    /// <summary>
    /// Gets or sets the head commit (for push events).
    /// </summary>
    [JsonPropertyName("head_commit")]
    public GitHubCommit? HeadCommit { get; set; }

    /// <summary>
    /// Gets or sets the pull request details (for PR events).
    /// </summary>
    [JsonPropertyName("pull_request")]
    public GitHubPullRequest? PullRequest { get; set; }

    /// <summary>
    /// Gets or sets the issue details (for issue events).
    /// </summary>
    [JsonPropertyName("issue")]
    public GitHubIssue? Issue { get; set; }

    /// <summary>
    /// Gets or sets the comment details (for comment events).
    /// </summary>
    [JsonPropertyName("comment")]
    public GitHubComment? Comment { get; set; }
}

/// <summary>
/// Represents a GitHub user.
/// </summary>
public sealed class GitHubUser
{
    /// <summary>
    /// Gets or sets the user's login name.
    /// </summary>
    [JsonPropertyName("login")]
    public string? Login { get; set; }

    /// <summary>
    /// Gets or sets the user's numeric ID.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the user's avatar URL.
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets the user's profile URL.
    /// </summary>
    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    /// <summary>
    /// Gets or sets the user type (User, Bot, Organization).
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

/// <summary>
/// Represents a GitHub repository.
/// </summary>
public sealed class GitHubRepository
{
    /// <summary>
    /// Gets or sets the repository ID.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the repository name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the full repository name (owner/repo).
    /// </summary>
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    /// <summary>
    /// Gets or sets the repository owner.
    /// </summary>
    [JsonPropertyName("owner")]
    public GitHubUser? Owner { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the repository is private.
    /// </summary>
    [JsonPropertyName("private")]
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Gets or sets the repository HTML URL.
    /// </summary>
    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    /// <summary>
    /// Gets or sets the repository description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the default branch name.
    /// </summary>
    [JsonPropertyName("default_branch")]
    public string? DefaultBranch { get; set; }
}

/// <summary>
/// Represents a GitHub organization.
/// </summary>
public sealed class GitHubOrganization
{
    /// <summary>
    /// Gets or sets the organization login name.
    /// </summary>
    [JsonPropertyName("login")]
    public string? Login { get; set; }

    /// <summary>
    /// Gets or sets the organization ID.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the organization description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Represents the pusher in a GitHub push event.
/// </summary>
public sealed class GitHubPusher
{
    /// <summary>
    /// Gets or sets the pusher's name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the pusher's email.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

/// <summary>
/// Represents a GitHub commit.
/// </summary>
public sealed class GitHubCommit
{
    /// <summary>
    /// Gets or sets the commit SHA.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the commit message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the commit timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the commit URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the commit author.
    /// </summary>
    [JsonPropertyName("author")]
    public GitHubCommitAuthor? Author { get; set; }

    /// <summary>
    /// Gets or sets the list of added files.
    /// </summary>
    [JsonPropertyName("added")]
    public IList<string>? Added { get; set; }

    /// <summary>
    /// Gets or sets the list of removed files.
    /// </summary>
    [JsonPropertyName("removed")]
    public IList<string>? Removed { get; set; }

    /// <summary>
    /// Gets or sets the list of modified files.
    /// </summary>
    [JsonPropertyName("modified")]
    public IList<string>? Modified { get; set; }
}

/// <summary>
/// Represents a commit author.
/// </summary>
public sealed class GitHubCommitAuthor
{
    /// <summary>
    /// Gets or sets the author's name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the author's email.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the author's GitHub username.
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }
}

/// <summary>
/// Represents a GitHub pull request.
/// </summary>
public sealed class GitHubPullRequest
{
    /// <summary>
    /// Gets or sets the pull request number.
    /// </summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>
    /// Gets or sets the pull request title.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the pull request body/description.
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>
    /// Gets or sets the pull request state (open, closed).
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the pull request HTML URL.
    /// </summary>
    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    /// <summary>
    /// Gets or sets the user who opened the pull request.
    /// </summary>
    [JsonPropertyName("user")]
    public GitHubUser? User { get; set; }

    /// <summary>
    /// Gets or sets the head branch reference.
    /// </summary>
    [JsonPropertyName("head")]
    public GitHubPullRequestRef? Head { get; set; }

    /// <summary>
    /// Gets or sets the base branch reference.
    /// </summary>
    [JsonPropertyName("base")]
    public GitHubPullRequestRef? Base { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the PR is merged.
    /// </summary>
    [JsonPropertyName("merged")]
    public bool Merged { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the PR was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the PR was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Represents a pull request branch reference.
/// </summary>
public sealed class GitHubPullRequestRef
{
    /// <summary>
    /// Gets or sets the branch name.
    /// </summary>
    [JsonPropertyName("ref")]
    public string? Ref { get; set; }

    /// <summary>
    /// Gets or sets the commit SHA.
    /// </summary>
    [JsonPropertyName("sha")]
    public string? Sha { get; set; }

    /// <summary>
    /// Gets or sets the repository information.
    /// </summary>
    [JsonPropertyName("repo")]
    public GitHubRepository? Repo { get; set; }
}

/// <summary>
/// Represents a GitHub issue.
/// </summary>
public sealed class GitHubIssue
{
    /// <summary>
    /// Gets or sets the issue number.
    /// </summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>
    /// Gets or sets the issue title.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the issue body/description.
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>
    /// Gets or sets the issue state (open, closed).
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the issue HTML URL.
    /// </summary>
    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    /// <summary>
    /// Gets or sets the user who opened the issue.
    /// </summary>
    [JsonPropertyName("user")]
    public GitHubUser? User { get; set; }

    /// <summary>
    /// Gets or sets the list of labels.
    /// </summary>
    [JsonPropertyName("labels")]
    public IList<GitHubLabel>? Labels { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the issue was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the issue was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Represents a GitHub label.
/// </summary>
public sealed class GitHubLabel
{
    /// <summary>
    /// Gets or sets the label name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the label color (hex code without #).
    /// </summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the label description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Represents a GitHub comment.
/// </summary>
public sealed class GitHubComment
{
    /// <summary>
    /// Gets or sets the comment ID.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the comment body.
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>
    /// Gets or sets the comment HTML URL.
    /// </summary>
    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    /// <summary>
    /// Gets or sets the user who wrote the comment.
    /// </summary>
    [JsonPropertyName("user")]
    public GitHubUser? User { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the comment was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the comment was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}
