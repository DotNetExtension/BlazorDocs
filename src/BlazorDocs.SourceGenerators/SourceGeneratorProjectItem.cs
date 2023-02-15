// Licensed to the Blazor Docs Contributors under one or more agreements.
// The Blazor Docs Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CodeAnalysis;

namespace BlazorDocs.SourceGenerators
{
    /// <summary>
    /// A source generator project item.
    /// </summary>
    internal class SourceGeneratorProjectItem
    {
        /// <summary>
        /// The base path to the project item.
        /// </summary>
        public string BasePath { get; }

        /// <summary>
        /// The file path to the project item.
        /// </summary>
        public string FilePath => _additionalText.Path.Substring(BasePath.Length).TrimStart(Path.DirectorySeparatorChar);

        /// <summary>
        /// The full path to the project item.
        /// </summary>
        public string FullPath => _additionalText.Path;

        /// <summary>
        /// The source text.
        /// </summary>
        public string SourceText { get; } = string.Empty;

        /// <summary>
        /// The <see cref="AdditionalText"/> representing the project item.
        /// </summary>
        private readonly AdditionalText _additionalText;

        /// <summary>
        /// Creates a <see cref="SourceGeneratorProjectItem"/>.
        /// </summary>
        /// <param name="additionalText">The <see cref="AdditionalText"/> representing the project item.</param>
        /// <param name="projectPath">The project path.</param>
        public SourceGeneratorProjectItem(AdditionalText additionalText, string projectPath)
        {
            _additionalText = additionalText;
            BasePath = projectPath;

            var text = additionalText.GetText();
            if (text is not null)
            {
                SourceText = text.ToString();
            }
        }
    }
}
