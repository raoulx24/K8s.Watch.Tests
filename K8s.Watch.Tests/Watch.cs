using k8s;
using k8s.Autorest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace K8s.Watch.Tests
{
    internal static class Watch
    {
        internal static async Task Test01()
        {
            Console.WriteLine("=== App Enter ===");
            var kubernetesClientFactory = new KubernetesClientFactory();
            var k8sClient = kubernetesClientFactory.GetClient();

            string? lastKnownResourceVersion = "1";
            CancellationToken cancellationToken = new();
            var namespaceName = "develop";

            VulnerabilityReportCrd myCrd = new();

            while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("=== While !IsCancellationRequested Enter ===");
                try
                {
                    Console.WriteLine($"Last Resource Version from app - {lastKnownResourceVersion ?? "null"}");
                    if (string.IsNullOrEmpty(lastKnownResourceVersion))
                    {
                        var lastResourceVersion = await GetLastResourceVersion(k8sClient, namespaceName);
                        lastKnownResourceVersion = lastResourceVersion;
                        Console.WriteLine($"Last Resource Version from k8s - {lastResourceVersion ?? "null"}");
                    }

                    var vrListResp = GetKubernetesObjectWatchList(k8sClient, namespaceName, lastKnownResourceVersion, cancellationToken);
                    await foreach ((WatchEventType type, VulnerabilityReportCr item) in
                        vrListResp.WatchAsync<VulnerabilityReportCr, CustomResourceList<VulnerabilityReportCr>>(
                            ex => {
                                Console.WriteLine($"WatchAsync xxx - {ex.Message}");
                                Console.WriteLine(ex.GetType());
                                if (ex is k8s.KubernetesException && ex.Message.StartsWith("too old resource version:"))
                                {
                                    lastKnownResourceVersion = null;
                                }
                            },
                            cancellationToken))
                    {
                        Console.WriteLine($"{type} - {item.Metadata.ResourceVersion} - {item.Metadata.Name}");
                        if (type == WatchEventType.Bookmark)
                        {
                            lastKnownResourceVersion = item.Metadata.ResourceVersion;
                        }
                    }
                    if (vrListResp.Status == TaskStatus.RanToCompletion)
                    {
                        Console.WriteLine("=== TaskStatus.RanToCompletion ===");
                        //lastKnownResourceVersion = null;
                    }
                }
                catch (HttpOperationException hoe) when (hoe.Response.StatusCode is HttpStatusCode.Unauthorized
                                                             or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
                {
                    Console.WriteLine("Watcher Error - 401, 403, 404");
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Task canceled.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Watcher crashed - {ex.Message}");
                }
            }
        }

        public async static Task<string?> GetLastResourceVersion(Kubernetes client, string namespaceName = "default")
        {
            int? limit = 2;
            string? continueToken = null;
            string? lastResourceVersion;

            do
            {
                var customResourceList = await GetVRs(client, namespaceName, limit, continueToken);

                foreach (var item in customResourceList.Items)
                {
                    Console.WriteLine($"Initial - {item.Metadata.ResourceVersion} - {item.Metadata.Name}");
                }

                continueToken = customResourceList.Metadata.ContinueProperty;
                lastResourceVersion = customResourceList.Metadata.ResourceVersion;
            } while (!string.IsNullOrEmpty(continueToken));

            return lastResourceVersion;
        }

        public static Task<HttpOperationResponse<CustomResourceList<VulnerabilityReportCr>>> GetKubernetesObjectWatchList(
            Kubernetes k8sClient, string namespaceName = "default", string? lastKnownResourceVersion = null,
            CancellationToken cancellationToken = new())
        {
            VulnerabilityReportCrd myCrd = new();

            return k8sClient.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync<CustomResourceList<VulnerabilityReportCr>>(
                            myCrd.Group,
                            myCrd.Version,
                            namespaceName,
                            myCrd.PluralName,
                            watch: true,
                            resourceVersion: lastKnownResourceVersion,
                            allowWatchBookmarks: true,
                            timeoutSeconds: 30,
                            cancellationToken: cancellationToken);
        }

        public static async Task<CustomResourceList<VulnerabilityReportCr>> GetVRs(Kubernetes k8sClient, string namespaceName = "default", int? pageLimit = null, string? continueToken = null)
        {
            VulnerabilityReportCrd myCrd = new();
            var customResources = await k8sClient.CustomObjects.ListNamespacedCustomObjectAsync(myCrd.Group,
                    myCrd.Version,
                    namespaceName,
                    myCrd.PluralName,
                    limit: pageLimit,
                    continueParameter: continueToken);
            return JsonSerializer.Deserialize<CustomResourceList<VulnerabilityReportCr>>(customResources?.ToString() ?? string.Empty) ?? new();
        }
    }
}
