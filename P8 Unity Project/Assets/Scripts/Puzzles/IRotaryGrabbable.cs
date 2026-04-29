/// <summary>
/// Marker interface for transform-owning rotary puzzle components (LeverGrab, ValveGrab, etc.).
/// HOMER guards check for this interface to skip position teleport and delta-movement.
/// HomerScaleMultiplier lets each component tune how much physical hand movement translates
/// to virtual hand movement while HOMER is grabbing it.
/// </summary>
public interface IRotaryGrabbable
{
    /// <summary>
    /// Fraction of HOMER's normal scale factor to apply when this object is grabbed.
    /// 1.0 = full HOMER scaling; 0.2 = 20% (much slower, closer to 1:1 physical feel).
    /// </summary>
    float HomerScaleMultiplier { get; }
}
