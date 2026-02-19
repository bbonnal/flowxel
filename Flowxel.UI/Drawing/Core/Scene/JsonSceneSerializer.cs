using System.Text.Json;

namespace Flowxel.UI.Drawing.Scene;

public sealed class JsonSceneSerializer : ISceneSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public string Serialize(SceneDocument scene)
        => JsonSerializer.Serialize(scene, SerializerOptions);

    public SceneDocument Deserialize(string json)
        => JsonSerializer.Deserialize<SceneDocument>(json, SerializerOptions)
           ?? throw new InvalidOperationException("Could not deserialize scene document.");
}
