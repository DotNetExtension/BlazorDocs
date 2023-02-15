// Licensed to the Blazor Docs Contributors under one or more agreements.
// The Blazor Docs Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace BlazorDocs.Models;

/// <summary>
/// Represents a documentation page.
/// </summary>
public abstract class DocumentationPage<TPage>
{
    /// <summary>
    /// The page title.
    /// </summary>
    public string Title { init; get; } = string.Empty;

    /// <summary>
    /// The page link.
    /// </summary>
    public string Link { init; get; } = string.Empty;

    /// <summary>
    /// The page layout.
    /// </summary>
    public string Layout { init; get; } = string.Empty;

    /// <summary>
    /// The children pages.
    /// </summary>
    public List<TPage> Children { init; get; } = new();
}
