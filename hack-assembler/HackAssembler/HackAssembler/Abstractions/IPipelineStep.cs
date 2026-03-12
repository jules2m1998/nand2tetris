namespace HackAssembler.Abstractions;

public interface IPipelineStep<T>
{
    int Order { get; }
    T  Process(T input);
}
