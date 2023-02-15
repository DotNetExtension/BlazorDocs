// Licensed to the Blazor Docs Contributors under one or more agreements.
// The Blazor Docs Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BlazorDocs.Models;

namespace BlazorDocs.Primer.Models;

/// <summary>
/// The config.
/// </summary>
public sealed class Config : DocumentationConfig
{
    /// <summary>
    /// The path to the logo image.
    /// </summary>
    public string LogoPath { init; get; } = string.Empty;
}
