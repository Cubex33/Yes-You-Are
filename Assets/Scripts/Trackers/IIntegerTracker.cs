/// <summary>
/// Interface for trackers that track integer values.
/// </summary>
public interface IIntegerTracker : ITracker
{
    int Value { get; set; }
}
