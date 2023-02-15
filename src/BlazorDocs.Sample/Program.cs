// Licensed to the Blazor Docs Contributors under one or more agreements.
// The Blazor Docs Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BlazorDocs.Sample;
using BlazorDocs.Sample.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.UseBlazorDocs();

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
