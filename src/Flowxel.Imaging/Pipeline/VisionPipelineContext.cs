using System.Collections.Concurrent;

namespace Flowxel.Imaging.Pipeline;

public class VisionPipelineContext
{
    private static readonly AsyncLocal<VisionPipelineContext> _current = new();
    
    public static VisionPipelineContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    // The output pool - nodes publish here after execution
    public ConcurrentDictionary<Guid, IVisionPort> Outputs { get; } = new();
    
    // Optional: keep node references for debugging
    public ConcurrentDictionary<Guid, VisionNode> Results { get; } = new();
}