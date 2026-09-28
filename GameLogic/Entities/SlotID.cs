namespace GameLogic.Entities;

public class SlotID : IEquatable<SlotID>
{
    public int Id { get; set; }

    public bool Equals(SlotID? other)
    {
        return other is not null && Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is SlotID other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(SlotID? left, SlotID? right)
    {
        return ReferenceEquals(left, right) || (left is not null && left.Equals(right));
    }

    public static bool operator !=(SlotID? left, SlotID? right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return Id.ToString();
    }
}
