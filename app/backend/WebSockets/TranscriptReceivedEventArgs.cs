namespace Api.WebSockets;

public class TranscriptReceivedEventArgs : EventArgs
{
    private TranscriptionMetadata _metadata;
    private TranscriptionData _data;

    public TranscriptReceivedEventArgs(TranscriptionMetadata metadata, TranscriptionData data)
    {
        this._metadata = metadata;
        this._data = data;
    }

    public TranscriptionMetadata Metadata { get { return this._metadata; } }

    public TranscriptionData Data { get { return this._data; } }
}
