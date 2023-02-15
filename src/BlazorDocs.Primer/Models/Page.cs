// Licensed to the Blazor Docs Contributors under one or more agreements.
// The Blazor Docs Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BlazorDocs.Models;

namespace BlazorDocs.Primer.Models;

/// <summary>
/// A page.
/// </summary>
public sealed class Page : DocumentationPage<Page>
{
    /// <summary>
    /// The short title.
    /// </summary>
    public string ShortTitle { init; get; } = string.Empty;
}
