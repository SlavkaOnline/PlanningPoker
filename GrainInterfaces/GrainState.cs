namespace GrainInterfaces
{
	public class GrainState<TId, TState>
	{
		public TId Id { get; }
		public int Version { get; }
		public TState State { get; }
		public GrainState(int version, TId id, TState state)
		{
			Id = id;
			Version = version;
			State = state;
		}
	}
}
