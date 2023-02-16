// Licensed to the Blazor Docs Contributors under one or more agreements.
// The Blazor Docs Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace BlazorDocs.SourceGenerators
{
    /// <summary>
    /// The markdown source code generator.
    /// </summary>
    [Generator]
    public class MarkdownSourceGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// The markdown pipeline.
        /// </summary>
        private readonly MarkdownPipeline _markdownPipeline;

        /// <summary>
        /// The yaml deserializer.
        /// </summary>
        private readonly IDeserializer _yamlDeserializer;

        /// <summary>
        /// The json serializer.
        /// </summary>
        private readonly ISerializer _jsonSerializer;

        /// <summary>
        /// Creates a <see cref="MarkdownSourceGenerator"/> instance.
        /// </summary>
        public MarkdownSourceGenerator()
        {
            _markdownPipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .ConfigureNewLine(Environment.NewLine)
                .UseYamlFrontMatter()
                .Build();

            _yamlDeserializer = new DeserializerBuilder()
                .Build();

            _jsonSerializer = new SerializerBuilder()
                .JsonCompatible()
                .Build();
        }

        /// <summary>
        /// Initializes the source code generator.
        /// </summary>
        /// <param name="context">The generator initialization context.</param>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var analyzerConfigOptions = context.AnalyzerConfigOptionsProvider;

            var markdownSourceGeneratorOptions = analyzerConfigOptions
                .Select(ComputeMarkdownSourceGeneratorOptions);

            var configItems = context.AdditionalTextsProvider
                .Where(f => f.Path.EndsWith("Docs.yaml", StringComparison.OrdinalIgnoreCase))
                .Combine(analyzerConfigOptions)
                .Select(ComputeProjectItem);

            var markdownItems = context.AdditionalTextsProvider
                .Where(f => f.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                .Combine(analyzerConfigOptions)
                .Select(ComputeProjectItem);

            var generatedConfigOutput = configItems
                .Combine(markdownItems.Collect())
                .Combine(markdownSourceGeneratorOptions)
                .Select((pair, _) =>
                {
                    var ((configItem, markdownItems), markdownSourceGeneratorOptions) = pair;

                    var hintName = "BlazorDocsExtensions.g.cs";
                    var csharpDocument = GenerateConfigCode(markdownSourceGeneratorOptions, configItem, markdownItems);

                    return (hintName, csharpDocument);
                });

            var generatedMarkdownOutput = markdownItems
                .Combine(configItems.Collect())
                .Combine(markdownSourceGeneratorOptions)
                .Select((pair, _) =>
                {
                    var ((sourceItem, configItems), markdownSourceGeneratorOptions) = pair;

                    var hintName = GetIdentifierFromPath(sourceItem.FilePath) + ".g.cs";
                    var csharpDocument = GenerateMarkdownCode(markdownSourceGeneratorOptions, sourceItem, configItems.First());

                    return (hintName, csharpDocument);
                });

            context.RegisterSourceOutput(generatedConfigOutput, static (context, pair) =>
            {
                var (hintName, csharpDocument) = pair;

                context.AddSource(hintName, csharpDocument);
            });

            context.RegisterSourceOutput(generatedMarkdownOutput, static (context, pair) =>
            {
                var (hintName, csharpDocument) = pair;

                context.AddSource(hintName, csharpDocument);
            });
        }

        /// <summary>
        /// Computes a <see cref="MarkdownSourceGenerationOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="AnalyzerConfigOptionsProvider"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="MarkdownSourceGenerationOptions"/>.</returns>
        private static MarkdownSourceGenerationOptions ComputeMarkdownSourceGeneratorOptions(AnalyzerConfigOptionsProvider options, CancellationToken cancellationToken)
        {
            var globalOptions = options.GlobalOptions;

            globalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);

            return new MarkdownSourceGenerationOptions()
            {
                RootNamespace = rootNamespace ?? "BlazorDocs"
            };
        }

        /// <summary>
        /// Computes a <see cref="SourceGeneratorProjectItem"/>.
        /// </summary>
        /// <param name="pair">The <see cref="AdditionalText"/> and <see cref="AnalyzerConfigOptionsProvider"/> pair.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="SourceGeneratorProjectItem"/>.</returns>
        private static SourceGeneratorProjectItem ComputeProjectItem((AdditionalText, AnalyzerConfigOptionsProvider) pair, CancellationToken cancellationToken)
        {
            var (additionalText, globalOptions) = pair;
            var options = globalOptions.GetOptions(additionalText);

            options.TryGetValue("build_property.projectdir", out var projectDir);

            return new SourceGeneratorProjectItem(additionalText, projectDir ?? Path.DirectorySeparatorChar.ToString());
        }

        /// <summary>
        /// Generates code for a config file.
        /// </summary>
        /// <param name="options">The <see cref="MarkdownSourceGenerationOptions"/>.</param>
        /// <param name="item">The <see cref="SourceGeneratorProjectItem"/>.</param>
        /// <param name="markdownFiles">The markdown files.</param>
        /// <returns>The resulting code document.</returns>
        private string GenerateConfigCode(MarkdownSourceGenerationOptions options, SourceGeneratorProjectItem item, ImmutableArray<SourceGeneratorProjectItem> markdownFiles)
        {
            var builder = new StringBuilder();

            var configYamlStream = GetYamlStreamFromText(item.SourceText);
            var configYamlNode = (YamlMappingNode)configYamlStream.First().RootNode;

            var configTheme = configYamlNode.Where(c => c.Key.ToString() == "theme").Select(c => c.Value).First();

            var pages = markdownFiles.Select(m => (file: m, route: GetRouteFromPath(m.FilePath)));

            var (rootFile, rootRoute) = pages
                .First(p => string.Equals(p.file.FilePath, Path.Combine(Path.GetDirectoryName(item.FilePath)!, "Index.md"), StringComparison.OrdinalIgnoreCase));

            var rootPageYamlStream = GetYamlStreamFromMarkdown(rootFile.SourceText);
            var rootPageYamlNode = (YamlMappingNode)rootPageYamlStream.First().RootNode;

            rootPageYamlNode.Add("link", rootRoute);

            MapPageChildren(rootPageYamlNode, rootRoute, pages.Where(p => p.route != rootRoute));

            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine();
            builder.AppendLine("using BlazorDocs.Models;");
            builder.AppendLine("using BlazorDocs.Services;");
            builder.AppendLine();
            builder.AppendLine($"namespace {options.RootNamespace}");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// Extension methods for Blazor Docs.");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public static class BlazorDocsExtensions");
            builder.AppendLine("    {");
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// Adds Blazor Docs services to the service collection.");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        /// <param name=\"services\">The <see cref=\"IServiceCollection\"/> to add Blazor Docs to.</param>");
            builder.AppendLine("        /// <returns>The <see cref=\"IServiceCollection\"/>.</returns>");
            builder.AppendLine("        public static IServiceCollection UseBlazorDocs(this IServiceCollection services)");
            builder.AppendLine("        {");
            builder.AppendLine("            var data = new DocumentationData()");
            builder.AppendLine("            {");
            builder.AppendLine("                Config =");
            builder.AppendLine("\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"");
            builder.AppendLine($"{GetJsonFromYamlStream(configYamlStream).TrimEnd(Environment.NewLine.ToCharArray())}");
            builder.AppendLine("\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\",");
            builder.AppendLine("                RootPage =");
            builder.AppendLine("\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"");
            builder.AppendLine($"{GetJsonFromYamlStream(rootPageYamlStream).TrimEnd(Environment.NewLine.ToCharArray())}");
            builder.AppendLine("\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"");
            builder.AppendLine("            };");
            builder.AppendLine();
            builder.AppendLine("            services.AddSingleton(data);");
            builder.AppendLine($"            services.AddSingleton<DocumentationService<{configTheme}.Models.Config, {configTheme}.Models.Page>>();");
            builder.AppendLine();
            builder.AppendLine("            return services;");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        /// <summary>
        /// Generates code for a markdown file.
        /// </summary>
        /// <param name="options">The <see cref="MarkdownSourceGenerationOptions"/>.</param>
        /// <param name="item">The <see cref="SourceGeneratorProjectItem"/>.</param>
        /// <param name="configItem">The <see cref="SourceGeneratorProjectItem"/> for the config.</param>
        /// <returns>The resulting code document.</returns>
        private string GenerateMarkdownCode(MarkdownSourceGenerationOptions options, SourceGeneratorProjectItem item, SourceGeneratorProjectItem configItem)
        {
            var builder = new StringBuilder();

            var configYamlStream = GetYamlStreamFromText(configItem.SourceText);
            var configYamlNode = (YamlMappingNode)configYamlStream.First().RootNode;

            var configTitle = configYamlNode.Where(c => c.Key.ToString() == "title").Select(c => c.Value).First();
            var configTheme = configYamlNode.Where(c => c.Key.ToString() == "theme").Select(c => c.Value).First();

            var pageYamlStream = GetYamlStreamFromMarkdown(item.SourceText);
            var pageYamlNode = (YamlMappingNode)pageYamlStream.First().RootNode;

            var pageTitle = pageYamlNode.Where(c => c.Key.ToString() == "title").Select(c => c.Value).First();
            var pageLayout = pageYamlNode.Where(c => c.Key.ToString() == "layout").Select(c => c.Value).First();

            var fullTitle = configTitle.ToString() == pageTitle.ToString() ? configTitle.ToString() : $"{pageTitle} - {configTitle}";

            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine();
            builder.AppendLine("using Microsoft.AspNetCore.Components;");
            builder.AppendLine("using Microsoft.AspNetCore.Components.Rendering;");
            builder.AppendLine("using Microsoft.AspNetCore.Components.Web;");
            builder.AppendLine("using Microsoft.JSInterop;");
            builder.AppendLine();
            builder.AppendLine("#pragma warning disable 1591");
            builder.AppendLine($"namespace {GetNamespaceFromPath(options.RootNamespace, item.FilePath)}");
            builder.AppendLine("{");
            builder.AppendLine($"    [Route(\"{GetRouteFromPath(item.FilePath)}\")]");
            builder.AppendLine($"    [Layout(typeof({configTheme}.Shared.{pageLayout}))]");
            builder.AppendLine($"    public class {GetClassNameFromPath(item.FilePath)} : ComponentBase");
            builder.AppendLine("    {");
            builder.AppendLine("        [Inject]");
            builder.AppendLine("        private IJSRuntime js { get; set; }");
            builder.AppendLine();
            builder.AppendLine("        #pragma warning disable 1998");
            builder.AppendLine("        protected override void BuildRenderTree(RenderTreeBuilder builder)");
            builder.AppendLine("        {");
            builder.AppendLine("            builder.OpenComponent<PageTitle>(0);");
            builder.AppendLine("            builder.AddAttribute(1, \"ChildContent\", new RenderFragment(titleBuilder =>");
            builder.AppendLine("            {");
            builder.AppendLine($"                titleBuilder.AddContent(2,");
            builder.AppendLine("\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"");
            builder.AppendLine($"{fullTitle}");
            builder.AppendLine("\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"");
            builder.AppendLine($"                );");
            builder.AppendLine("            }));");
            builder.AppendLine("            builder.CloseComponent();");
            builder.AppendLine("            builder.AddMarkupContent(3,");
            builder.AppendLine("\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"");
            builder.AppendLine(Markdown.ToHtml(item.SourceText, _markdownPipeline).Trim(Environment.NewLine.ToArray()));
            builder.AppendLine("\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"");
            builder.AppendLine("            );");
            builder.AppendLine("        }");
            builder.AppendLine("        #pragma warning restore 1998");
            builder.AppendLine();
            builder.AppendLine("        protected override async Task OnAfterRenderAsync(bool firstR66ender)");
            builder.AppendLine("        {");
            builder.AppendLine("            await js.InvokeVoidAsync(\"Prism.highlightAll\");");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            builder.AppendLine("#pragma warning restore 1591");

            return builder.ToString();
        }

        /// <summary>
        /// Maps a pages children nodes.
        /// </summary>
        /// <param name="parent">The parent <see cref="YamlMappingNode"/>.</param>
        /// <param name="parentRoute">The parent route.</param>
        /// <param name="pages">The pages.</param>
        private void MapPageChildren(YamlMappingNode parent, string parentRoute, IEnumerable<(SourceGeneratorProjectItem file, string route)> pages)
        {
            var children = new YamlSequenceNode();

            var childPages = pages.Where(p => !p.route.Substring(parentRoute.Length, p.route.Length - parentRoute.Length).Trim('/').Contains('/'));

            foreach (var (file, route) in childPages)
            {
                var pageYamlStream = GetYamlStreamFromMarkdown(file.SourceText);

                var pageYamlNode = (YamlMappingNode)pageYamlStream.First().RootNode;
                pageYamlNode.Add("link", route);

                MapPageChildren(pageYamlNode, route, pages.Where(p => p.route.StartsWith(route) && p.route != route));

                children.Add(pageYamlNode);
            }

            parent.Add("children", children);
        }

        /// <summary>
        /// Gets the yaml front matter stream from a markdown document.
        /// </summary>
        /// <param name="markdownText">The markdown text.</param>
        /// <returns>The yaml stream.</returns>
        private YamlStream GetYamlStreamFromMarkdown(string markdownText)
        {
            var markdown = Markdown.Parse(markdownText, _markdownPipeline);
            var frontMatterBlock = markdown.Descendants<YamlFrontMatterBlock>().FirstOrDefault();

            return GetYamlStreamFromText(markdownText.Substring(frontMatterBlock.Span.Start, frontMatterBlock.Span.End - 3));
        }

        /// <summary>
        /// Gets the yaml front stream from yaml text.
        /// </summary>
        /// <param name="yamlText">The yaml text.</param>
        /// <returns>The yaml stream.</returns>
        private YamlStream GetYamlStreamFromText(string yamlText)
        {
            var yamlStream = new YamlStream();

            yamlStream.Load(new StringReader(yamlText));

            return yamlStream;
        }

        /// <summary>
        /// Gets a json document from a yaml stream.
        /// </summary>
        /// <param name="yamlStream">The yaml stream.</param>
        /// <returns>The json document.</returns>
        private string GetJsonFromYamlStream(YamlStream yamlStream)
        {
            var yamlBuilder = new StringBuilder();

            yamlStream.Save(new StringWriter(yamlBuilder));

            var yaml = _yamlDeserializer.Deserialize(new StringReader(yamlBuilder.ToString()));

            return _jsonSerializer.Serialize(yaml!);
        }

        /// <summary>
        /// Gets a file identifier by the file path.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>The identifier.</returns>
        private static string GetIdentifierFromPath(string filePath)
        {
            var builder = new StringBuilder();

            for (var i = 0; i < filePath.Length; i++)
            {
                switch (filePath[i])
                {
                    case '\\' or '/':
                    case char ch when !char.IsLetterOrDigit(ch):
                        builder.Append('_');
                        break;
                    default:
                        builder.Append(filePath[i]);
                        break;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gets a namespace by the root namespace and file path.
        /// </summary>
        /// <param name="rootNamespace">The root namespace.</param>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>The namespace.</returns>
        private static string GetNamespaceFromPath(string rootNamespace, string filePath)
        {
            var builder = new StringBuilder();

            var directory = Path.GetDirectoryName(filePath);

            for (var i = 0; i < directory.Length; i++)
            {
                switch (directory[i])
                {
                    case '\\' or '/':
                        builder.Append('.');
                        break;
                    case char ch when !char.IsLetterOrDigit(ch):
                        builder.Append('_');
                        break;
                    default:
                        builder.Append(directory[i]);
                        break;
                }
            }

            var fileNamespace = builder.ToString();

            if (string.IsNullOrEmpty(fileNamespace))
            {
                return rootNamespace;
            }
            else
            {
                return $"{rootNamespace}.{fileNamespace}";
            }
        }

        /// <summary>
        /// Gets a class name by the file path.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>The class name.</returns>
        private static string GetClassNameFromPath(string filePath)
        {
            var builder = new StringBuilder();

            var fileName = Path.GetFileNameWithoutExtension(filePath);

            for (var i = 0; i < fileName.Length; i++)
            {
                switch (fileName[i])
                {
                    case char ch when !char.IsLetterOrDigit(ch):
                        builder.Append('_');
                        break;
                    default:
                        builder.Append(fileName[i]);
                        break;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gets a route by the file path.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>The route.</returns>
        private static string GetRouteFromPath(string filePath)
        {
            var builder = new StringBuilder("/");

            var directory = Path.GetDirectoryName(filePath).ToLower();
            var fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();

            for (var i = 0; i < directory.Length; i++)
            {
                switch (directory[i])
                {
                    case '\\' or '/':
                        builder.Append('/');
                        break;
                    case char ch when !char.IsLetterOrDigit(ch):
                        builder.Append('_');
                        break;
                    default:
                        builder.Append(directory[i]);
                        break;
                }
            }

            if (fileName != "index")
            {
                builder.Append('/');

                for (var i = 0; i < fileName.Length; i++)
                {
                    switch (fileName[i])
                    {
                        case char ch when !char.IsLetterOrDigit(ch):
                            builder.Append('-');
                            break;
                        default:
                            builder.Append(fileName[i]);
                            break;
                    }
                }
            }

            return builder.ToString();
        }
    }
}
