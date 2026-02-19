namespace Flowxel.UI.Controls.Drawing.Scene;

public interface ISceneSerializer
{
    string Serialize(SceneDocument scene);

    SceneDocument Deserialize(string json);
}
