namespace VoxBotanica.Types
{
public struct TurtleState<TVector>
{
	public TurtleState(TVector pos, TVector dir)
	{
		Position = pos;
		Direction = dir;
	}

	public readonly TVector Position;
	public readonly TVector Direction;
}
}
