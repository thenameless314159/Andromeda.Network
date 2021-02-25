namespace Andromeda.Framing
{
    /// <summary>
    /// Contains logic of both <see cref="IMetadataDecoder"/> and <see cref="IMetadataEncoder"/>.
    /// </summary>
    /// <inheritdoc cref="IMetadataEncoder"/>
    /// <inheritdoc cref="IMetadataDecoder"/>
    public interface IMetadataParser : IMetadataDecoder, IMetadataEncoder
    {
    }
}
