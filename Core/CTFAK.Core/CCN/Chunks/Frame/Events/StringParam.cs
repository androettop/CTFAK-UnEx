using CTFAK.Memory;
using CTFAK.Utils;
using Newtonsoft.Json.Linq;

namespace CTFAK.MMFParser.EXE.Loaders.Events.Parameters
{
    public class StringParam:ParameterCommon
    {
        public string Value;

        public override void Read(ByteReader reader)
        {
            Value = reader.ReadYuniversal();
        }

        public override void Write(ByteWriter Writer)
        {
            Writer.WriteUnicode(Value);
        }

        public override string ToString()
        {
            return $"String: {Value} ({Value.Length})";
        }
    }
}