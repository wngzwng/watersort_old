namespace WaterSort.Core.Solvers.TubeTopologies;

public interface ITubeTopologyFactory
{
    ITubeTopology Create(int tubeCount);
}