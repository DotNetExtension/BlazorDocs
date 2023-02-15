// Licensed to the Blazor Docs Contributors under one or more agreements.
// The Blazor Docs Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace BlazorDocs.Models;

/// <summary>
/// Represents the documentation data.
/// </summary>
public sealed class DocumentationData
{
    /// <summary>
    /// The documentation config.
    /// </summary>
    public string Config { init; get; } = string.Empty;

    /// <summary>
    /// The documentation root page.
    /// </summary>
    public string RootPage { init; get; } = string.Empty;
}
