/// <summary>
/// Interface for trackers that track string values.
/// </summary>
public interface IStringTracker : ITracker
{
    string Value { get; set; }
}
