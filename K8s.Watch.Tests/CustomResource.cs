using k8s.Models;
using k8s;
using System.Text.Json.Serialization;

namespace K8s.Watch.Tests
{
    public abstract class CustomResource : KubernetesObject, IMetadata<V1ObjectMeta>
    {
        [JsonPropertyName("metadata")]
        public V1ObjectMeta Metadata { get; set; } = new();
    }

    public abstract class CustomResourceDefinition
    {
        public abstract string Version { get; }
        public abstract string Group { get; }
        public abstract string PluralName { get; }
        public abstract string Kind { get; }
        public abstract string? Namespace { get; init; }
    }

    public class CustomResourceList<T> : KubernetesObject, IItems<T> where T : CustomResource
    {
        [JsonPropertyName("metadata")]
        public V1ListMeta Metadata { get; set; } = new();
        [JsonPropertyName("items")]
        public IList<T> Items { get; set; } = [];
    }
}
