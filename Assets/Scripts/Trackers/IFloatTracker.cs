/// <summary>
/// Interface for trackers that track float values.
/// </summary>
public interface IFloatTracker : ITracker
{
    float Value { get; set; }
}
