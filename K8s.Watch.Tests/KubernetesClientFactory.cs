using k8s;

namespace K8s.Watch.Tests
{
    internal class KubernetesClientFactory
    {
        private readonly Kubernetes kubernetesClient;

        public KubernetesClientFactory()
        {
            KubernetesClientConfiguration? defaultConfig = KubernetesClientConfiguration.IsInCluster()
                ? KubernetesClientConfiguration.InClusterConfig()
                : KubernetesClientConfiguration.BuildConfigFromConfigFile();
            kubernetesClient = new Kubernetes(defaultConfig);
        }

        public Kubernetes GetClient() => kubernetesClient;
    }
}
