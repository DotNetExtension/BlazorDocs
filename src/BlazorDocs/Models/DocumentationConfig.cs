// Licensed to the Blazor Docs Contributors under one or more agreements.
// The Blazor Docs Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace BlazorDocs.Models;

/// <summary>
/// Represents a documentation config.
/// </summary>
public abstract class DocumentationConfig
{
    /// <summary>
    /// The documentation title.
    /// </summary>
    public string Title { init; get; } = string.Empty;

    /// <summary>
    /// The documentation theme.
    /// </summary>
    public string Theme { init; get; } = string.Empty;
}
