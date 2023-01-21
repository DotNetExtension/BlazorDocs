// Licensed to the Blazor Docs Contributors under one or more agreements.
// The Blazor Docs Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace BlazorDocs.SourceGenerators
{
    /// <summary>
    /// Source generation options for markdown files.
    /// </summary>
    internal class MarkdownSourceGenerationOptions
    {
        /// <summary>
        /// The root namespace to be used.
        /// </summary>
        public string RootNamespace { get; set; } = "BlazorDocs";
    }
}
