using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning disable IDE1006 // Naming Styles

namespace WDCMonInterface.DAP
{

    class DapHandler
    {
        private readonly Stream Outgoing;
        private readonly StreamReader reader;
        private readonly StreamWriter writer;

        public DapHandler(Stream Incoming, Stream Outgoing)
        {
            this.Outgoing = Outgoing;
            var encoding = new UTF8Encoding(false);
            reader = new StreamReader(Incoming, encoding);
            writer = new StreamWriter(Outgoing, encoding);
        }

        public bool ShouldRun { get; set; } = true;

        public void Run()
        {
            while (ShouldRun)
            {
                ReadMessage();
            }
        }

        void ReadMessage()
        {
            try
            {
                DapMessage message = new();
                while (true)
                {
                    string header_str = reader.ReadLine() ?? "";
                    if (string.IsNullOrEmpty(header_str)) break;

                    int idx = header_str.IndexOf(": ");
                    if (idx < 0) idx = header_str.Length;
                    string key = header_str[..idx];
                    string value = header_str[(idx + 2)..];
                    message.Header[key.Trim().ToLower()] = value.Trim();
                }

                var contentLength_s = message.Header.GetValueOrDefault("content-length");
                int contentLength = 0;
                if (int.TryParse(contentLength_s, out int i)) contentLength = i;

                StringBuilder sb = new();
                while (Encoding.UTF8.GetByteCount(sb.ToString()) < contentLength)
                {
                    sb.Append((char)reader.Read());
                }

                string content = sb.ToString();
                Logger.Log("received: " + content);

                message.Content = content;
                OnDapMessageReceived?.Invoke(this, message);
            }
            catch (Exception ex)
            {
                Logger.Log("Error receiving DAP Message");
                Logger.Log(ex);
            }
        }

        public delegate void OnDapMessageReceivedDelegate(DapHandler handler, DapMessage dm);
        public event OnDapMessageReceivedDelegate? OnDapMessageReceived;

        public void SendMessage(DapMessage message)
        {
            Logger.Log("> " + message.Content);
            foreach (var header in message.Header)
            {
                if (header.Key.ToLower() != "content-length")
                {
                    writer.WriteLine($"{header.Key}: {header.Value}");
                }
            }
            byte[] content_data = Encoding.UTF8.GetBytes(message.Content);
            writer.WriteLine($"Content-Length: {content_data.Length}");
            writer.WriteLine();
            writer.Flush();
            Outgoing.Write(content_data, 0, content_data.Length);
            writer.WriteLine();
            Outgoing.Flush();
        }
    }

    class DapMessage
    {
        public Dictionary<string, string> Header = new();
        public string Content
        {
            get
            {
                if (Message == null) return "{}";
                return JsonSerializer.Serialize(Message, Message.GetType(), new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            }
            set
            {
                var message = JsonSerializer.Deserialize<ProtocolMessage>(value);
                switch (message?.type)
                {
                    case "request":
                        Message = JsonSerializer.Deserialize<RequestMessage>(value);
                        break;
                    case "response":
                        Message = JsonSerializer.Deserialize<ResponseMessage>(value);
                        break;
                    case "event":
                        Message = JsonSerializer.Deserialize<EventMessage>(value);
                        break;
                    default:
                        Message = null;
                        break;
                }
            }
        }

        public ProtocolMessage? Message { get; set; }
    }

    public class ProtocolMessage
    {
        /// <summary>
        /// Sequence number of the message (also known as message ID). The `seq` for
        /// the first message sent by a client or debug adapter is 1, and for each
        /// subsequent message is 1 greater than the previous message sent by that
        /// actor. `seq` can be used to order requests, responses, and events, and to
        /// associate requests with their corresponding responses.For protocol
        /// messages of type `request` the sequence number can be used to cancel the
        /// request.
        /// </summary>
        public double? seq { get; set; }

        /// <summary>
        /// request | response | event
        /// </summary>
        public string? type { get; set; }
    }

    public class RequestMessage : ProtocolMessage
    {
        public string? command { get; set; }
        public Dictionary<string, JsonElement>? arguments { get; set; }
    }

    public class EventMessage : ProtocolMessage
    {
        [JsonPropertyName("event")]
        public string? Event { get; set; }
        public Dictionary<string, JsonElement>? body { get; set; }
    }

    public class ResponseMessage : ProtocolMessage
    {
        public double? request_seq { get; set; }
        public bool success { get; set; }
        public string? command { get; set; }
        /// <summary>
        /// 'cancelled' | 'notStopped'
        /// </summary>
        public string? message { get; set; }
        public Dictionary<string, JsonElement>? body { get; set; }
    }

}