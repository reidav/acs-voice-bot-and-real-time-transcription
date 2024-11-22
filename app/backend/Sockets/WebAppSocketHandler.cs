using System.Net.WebSockets;
using System.Text;

namespace Api.Sockets;

public class WebAppSocketHandler
{
    private WebSocket _webSocket;
    private CancellationTokenSource _cancellationTokenSource;

    public WebAppSocketHandler(WebSocket webSocket)
    {
        this._webSocket = webSocket;
        this._cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task ProcessWebSocketAsync()
    {
        if (_webSocket == null)
        {
            return;
        }

        try
        {
            if (this._webSocket == null)
                return;
            
            try
            {
                while (this._webSocket.State == WebSocketState.Open || this._webSocket.State == WebSocketState.Closed)
                {
                    byte[] receiveBuffer = new byte[2048];
                    WebSocketReceiveResult receiveResult = await this._webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), this._cancellationTokenSource.Token);

                    if (receiveResult.MessageType != WebSocketMessageType.Close)
                    {
                        string data = Encoding.UTF8.GetString(receiveBuffer).TrimEnd('\0');
                        Console.WriteLine("-----------: " + data);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception -> {ex}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception -> {ex}");
        }
        finally
        {
            this.Close();
        }
    }

    public void Close()
    {
        this._cancellationTokenSource.Cancel();
        this._cancellationTokenSource.Dispose();
    }

    public async Task SendMessageAsync(string message)
    {
        if (_webSocket?.State == WebSocketState.Open)
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, endOfMessage: true, this._cancellationTokenSource.Token);
        }
    }
}