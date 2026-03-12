using System.Text;

namespace HackAssembler;

internal sealed class ConsoleLoader : IDisposable
{
    private static readonly string[] Frames = ["|", "/", "-", "\\"];

    private readonly CancellationTokenSource _cancellationSource = new();
    private readonly Task? _renderTask;
    private readonly object _sync = new();
    private string _message;
    private int _lineWidth;

    public ConsoleLoader(string message)
    {
        _message = message;

        if (Console.IsOutputRedirected)
        {
            return;
        }

        _renderTask = Task.Run(RenderAsync);
    }

    public void Update(string message)
    {
        lock (_sync)
        {
            _message = message;
        }
    }

    public void Dispose()
    {
        if (_renderTask is null)
        {
            return;
        }

        _cancellationSource.Cancel();

        try
        {
            _renderTask.Wait();
        }
        catch (AggregateException exception) when (exception.InnerExceptions.All(inner => inner is TaskCanceledException or OperationCanceledException))
        {
        }

        Console.Write("\r");
        Console.Write(new string(' ', _lineWidth));
        Console.Write("\r");
    }

    private async Task RenderAsync()
    {
        var frameIndex = 0;
        while (!_cancellationSource.Token.IsCancellationRequested)
        {
            string message;
            lock (_sync)
            {
                message = _message;
            }

            var output = new StringBuilder()
                .Append('\r')
                .Append(Frames[frameIndex % Frames.Length])
                .Append(' ')
                .Append(message)
                .ToString();

            _lineWidth = Math.Max(_lineWidth, output.Length - 1);
            Console.Write(output.PadRight(_lineWidth + 1));

            frameIndex++;

            try
            {
                await Task.Delay(100, _cancellationSource.Token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
