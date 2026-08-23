using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OneNoteMomtion.Server.Commands;

namespace OneNoteMomtion.Server.Ipc
{
    internal sealed class CommandPipeServer : IDisposable
    {
        private readonly string _pipeName;
        private readonly Func<string, string?, CommandResult> _handler;
        private readonly SynchronizationContext _uiContext;
        private readonly CancellationTokenSource _cancellation = new();
        private bool _disposed;

        public CommandPipeServer(string pipeName, Func<string, string?, CommandResult> handler, SynchronizationContext uiContext)
        {
            _pipeName = pipeName;
            _handler = handler;
            _uiContext = uiContext;
        }

        public event EventHandler? ShutdownRequested;

        public void Start()
        {
            Task.Run(() => AcceptLoopAsync(_cancellation.Token));
            EngineLog.Info("Listening for commands on pipe " + _pipeName + ".");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cancellation.Cancel();
            _cancellation.Dispose();
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using NamedPipeServerStream pipe = new(_pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                    await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                    await HandleConnectionAsync(pipe, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    EngineLog.Error("Error handling a pipe connection.", ex);

                    await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        private async Task HandleConnectionAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
        {
            UTF8Encoding encoding = new(false);
            StreamReader reader = new(pipe, encoding, false, 1024, leaveOpen: true);
            StreamWriter writer = new(pipe, encoding, 1024, leaveOpen: true) { AutoFlush = false };

            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            string[] fields = PipeProtocol.Decode(line);

            if (fields.Length == 0)
            {
                return;
            }

            string command = fields[0];
            string? argument = fields.Length > 1 && fields[1].Length > 0 ? fields[1] : null;

            string response = command == CommandIds.EngineShutdown ? PipeProtocol.Encode(PipeProtocol.StatusOk, "Shutting down.") : Encode(ExecuteOnUiThread(command, argument));

            await writer.WriteLineAsync(response).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

            if (command == CommandIds.EngineShutdown)
            {
                _uiContext.Post(_ => ShutdownRequested?.Invoke(this, EventArgs.Empty), null);
            }
        }

        private CommandResult ExecuteOnUiThread(string command, string? argument)
        {
            CommandResult? result = null;

            _uiContext.Send(_ => result = _handler(command, argument), null);

            return result ?? CommandResult.Failed("The command returned no result.");
        }

        private static string Encode(CommandResult result)
        {
            if (!result.Success)
            {
                return PipeProtocol.Encode(PipeProtocol.StatusError, result.Message ?? "Nieznany blad.");
            }

            List<string?> fields = [PipeProtocol.StatusOk];

            switch (result.Data)
            {
                case IReadOnlyList<string> items:
                    fields.AddRange(items);
                    break;
                case string text:
                    fields.Add(text);
                    break;
                default:
                    fields.Add(result.Message);
                    break;
            }

            return PipeProtocol.Encode([.. fields]);
        }
    }
}
