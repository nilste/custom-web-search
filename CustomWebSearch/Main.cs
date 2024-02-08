using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Wox.Plugin;

namespace Community.Powertoys.Run.Plugin.CustomWebSearch
{
    public class Service
    {
        public string? Alias { get; set; }
        public string? Name { get; set; }
        public string? Url { get; set; }
    }

    public class Main : IPlugin
    {
        public static string PluginID => "9A65303AC054498D883D49C4A3D7B578";
        public string Name => "Custom Web Search";
        public string Description => "Search on available sites.";

        private List<Service>? _services;
        private PluginInitContext? _context;

        public void Init(PluginInitContext context)
        {
            this._context = context;

            try
            {
                var servicesFilePath = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, "services.json");
                _services = JsonSerializer.Deserialize<List<Service>>(File.ReadAllText(servicesFilePath));
            }
            catch (JsonException ex)
            {
                this._context.API.ShowMsg("Failed to parse services.json", ex.ToString(), string.Empty);
                _services = [];
            }
        }

        public List<Result> Query(Query query)
        {
            string serviceAlias = string.Empty;
            string searchPhrase = string.Empty;

            if (query.Terms != null && query.Terms.Any())
            {
                serviceAlias = query.Terms[0];
                searchPhrase = string.Join(" ", query.Terms.Skip(1));
            }

            // Filter suggested results based on the search phrase
            List<Service> filteredServices = _services ?? [];
            if (!string.IsNullOrEmpty(serviceAlias))
            {
                filteredServices = filteredServices
                    .Where(s => s.Alias != null && s.Alias.StartsWith(serviceAlias, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            List<Result> results = [];

            foreach (Service filteredService in filteredServices)
            {
                string serviceName = filteredService.Name ?? string.Empty;

                string url = filteredService.Url != null
                    ? filteredService.Url.Replace("{query}", Uri.EscapeDataString(searchPhrase))
                    : string.Empty;

                string formattedSearchPhrase = string.IsNullOrEmpty(searchPhrase)
                    ? "for..." : $"for '{searchPhrase}'";

                // Fall back to default icon if the service name is not found
                string iconFolder = "Images/Favicons/";
                string iconPath = string.IsNullOrEmpty(serviceName)
                    ? $"{iconFolder}Default.png"
                    : $"{iconFolder}{serviceName.Replace(" ", string.Empty)}.png";

                Result result = new Result
                {
                    Title = $"Search {serviceName} {formattedSearchPhrase}",
                    SubTitle = $"Command: {query.ActionKeyword} {filteredService.Alias} <query>",
                    QueryTextDisplay = filteredService.Alias,
                    IcoPath = iconPath,
                    Action = _ =>
                    {
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                        return true;
                    },
                };
                results.Add(result);
            }

            return results;
        }
    }
}
