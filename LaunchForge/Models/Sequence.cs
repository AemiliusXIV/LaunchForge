using LaunchForge.Models.Steps;

namespace LaunchForge.Models;

public class Sequence
{
    public string          Name       { get; set; } = "Untitled Sequence";
    public List<StepBase>  Steps      { get; set; } = [];
    public DateTime        CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime        ModifiedAt { get; set; } = DateTime.UtcNow;
}
