namespace Flowxel.UI.Drawing.Scene;

public interface ISceneSerializer
{
    string Serialize(SceneDocument scene);

    SceneDocument Deserialize(string json);
}
