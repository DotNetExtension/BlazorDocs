// Licensed to the Blazor Docs Contributors under one or more agreements.
// The Blazor Docs Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using BlazorDocs.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorDocs.Services;

/// <summary>
/// Handles all documentation oriented functions.
/// </summary>
public class DocumentationService<TConfig, TPage>
{
    /// <summary>
    /// The documentation config.
    /// </summary>
    public TConfig Config { get; }

    /// <summary>
    /// The documentation root page.
    /// </summary>
    public TPage RootPage { get; }

    /// <summary>
    /// The <see cref="NavigationManager"/> instance.
    /// </summary>
    private readonly NavigationManager _navigation;

    /// <summary>
    /// Creates a <see cref="DocumentationService{TConfig, TPage}"/> instance.
    /// </summary>
    /// <param name="data">The <see cref="DocumentationData"/> instance.</param>
    /// <param name="navigation">The <see cref="NavigationManager"/> instance.</param>
    public DocumentationService(DocumentationData data, NavigationManager navigation)
    {
        var options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        Config = JsonSerializer.Deserialize<TConfig>(data.Config, options)!;
        RootPage = JsonSerializer.Deserialize<TPage>(data.RootPage, options)!;
        _navigation = navigation;
    }

    /// <summary>
    /// Determines if the specified page is active.
    /// </summary>
    /// <param name="page">The <see cref="DocumentationPage{TPage}"/> to check.</param>
    /// <param name="allowParent">If true should be returned if its a parent of the page,</param>
    /// <returns>If the page is active.</returns>
    public bool IsPageActive(DocumentationPage<TPage> page, bool allowParent)
    {
        var route = _navigation.Uri.Replace(_navigation.BaseUri, "/");

        if (allowParent)
        {
            return route.StartsWith(page.Link);
        }
        else
        {
            return route.EndsWith(page.Link);
        }
    }

    /// <summary>
    /// Gets the breadcrumbs.
    /// </summary>
    /// <returns>The breadcrumbs list.</returns>
    public List<TPage> GetBreadcrumbs()
    {
        var result = new List<TPage>();

        var route = _navigation.Uri.Replace(_navigation.BaseUri, "/");
        var currentPage = RootPage as DocumentationPage<TPage>;

        result.Add(RootPage);

        while (currentPage!.Link != route)
        {
            foreach (var childPage in currentPage.Children)
            {
                var childPageItem = childPage as DocumentationPage<TPage>;

                if (route.StartsWith(childPageItem!.Link))
                {
                    result.Add(childPage);
                    currentPage = childPageItem;
                }
            }
        }

        return result;
    }
}
