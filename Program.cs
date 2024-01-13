using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

class DubnaServer
{
    private static ConcurrentDictionary<int, NamedPipeServerStream> clients = new ConcurrentDictionary<int, NamedPipeServerStream>();

    static async Task Main(string[] args)
    {
        Console.WriteLine("Server started...");

        int clientId = 0;
        while (true)
        {
            var server = new NamedPipeServerStream("Dubna", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            Console.WriteLine("Waiting for connection...");
            await server.WaitForConnectionAsync();
            clientId++;
            Console.WriteLine($"Client {clientId} connected.");
            // Сохраняем коннекшены к клиентам в словарь
            clients[clientId] = server;

            HandleClient(clientId, server);
        }
    }

    // Функция приёма сообщений от клиента
    private static async void HandleClient(int clientId, NamedPipeServerStream clientStream)
    {
        var reader = new StreamReader(clientStream);
        try
        {
            while (true)
            {
                string message = await reader.ReadLineAsync();
                // Обрабатываем не пустые сообщения и отправляем их
                if (!string.IsNullOrEmpty(message))
                {
                    Console.WriteLine($"Received from client {clientId}: {message}");
                    await BroadcastMessage(clientId, message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error received message from client {clientId}: {ex.Message}");
        }
        finally
        {
            clients.TryRemove(clientId, out var _);
            clientStream.Dispose();
            Console.WriteLine($"Client {clientId} disconnected.");
        }
    }

    // Функция отправки сообщения в чат
    private static async Task BroadcastMessage(int senderId, string message)
    {
        // Проходим по всем подключенным клиентам и отправляем им сообщение
        foreach (var client in clients)
        {
            if (client.Value.IsConnected)
            {
                try
                {
                    var writer = new StreamWriter(client.Value);
                    await writer.WriteLineAsync($"Client {senderId}: {message}");
                    await writer.FlushAsync();
                }
                catch
                {
                    Console.WriteLine($"Error sending message to client {client.Key}.");
                }
            }
        }
    }
}
