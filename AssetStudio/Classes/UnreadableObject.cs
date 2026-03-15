using System;

namespace AssetStudio
{
    public class UnreadableObject : Object
    {
        public ClassIDType OriginalType { get; }
        public string ErrorMessage { get; }

        public UnreadableObject(ObjectReader reader, Exception exception) : base(reader)
        {
            OriginalType = reader.type;
            ErrorMessage = exception.ToString();

            // Force generic handling so GUI export routes to raw data.
            type = ClassIDType.UnknownType;
        }
    }
}