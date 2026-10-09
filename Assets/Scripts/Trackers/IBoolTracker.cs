/// <summary>
/// Interface for trackers that track boolean values.
/// </summary>
public interface IBoolTracker : ITracker
{
    bool Value { get; set; }
}
