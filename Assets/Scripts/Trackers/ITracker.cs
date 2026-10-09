/// <summary>
/// Base interface for all trackers. Defines the tracker type enum.
/// </summary>
public interface ITracker
{
    TrackerType Type { get; }
    string SaveKey { get; }
    object GetValueAsObject();
    void SetValueFromObject(object value);
}

/// <summary>
/// Enum defining all possible tracker value types.
/// </summary>
public enum TrackerType
{
    Int,
    Float,
    String,
    Bool
}
