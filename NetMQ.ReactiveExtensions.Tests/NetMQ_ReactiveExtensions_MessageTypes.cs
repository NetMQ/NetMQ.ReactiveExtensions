#region

using ProtoBuf;

// ReSharper disable InconsistentNaming

#endregion

// ReSharper disable ConvertClosureToMethodGroup

namespace NetMQ.ReactiveExtensions.Tests
{

    #region Message types.

    [ProtoContract]
    public class MyMessageClassType1
    {
        public MyMessageClassType1()
        {
        }

        public MyMessageClassType1(int num, string name)
        {
            Num = num;
            Name = name;
        }

        [ProtoMember(1)]
        public int Num { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }
    }

    [ProtoContract]
    public class MyMessageClassType2
    {
        public MyMessageClassType2()
        {
        }

        public MyMessageClassType2(int num, string name)
        {
            Num = num;
            Name = name;
        }

        [ProtoMember(1)]
        public int Num { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }
    }

    [ProtoContract]
    public struct MyMessageStructType1
    {
        public MyMessageStructType1(int num, string name)
        {
            Num = num;
            Name = name;
        }

        [ProtoMember(1)]
        public int Num { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }
    }

    [ProtoContract]
    public struct MyMessageStructType2
    {
        public MyMessageStructType2(int num, string name)
        {
            Num = num;
            Name = name;
        }

        [ProtoMember(1)]
        public int Num { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }
    }

    public class MessageNotSerializableByProtobuf
    {
        public int NotSerializable { get; set; }
    }

    [ProtoContract]
    public class ClassNameIsLongerThenThirtyTwoCharactersForAbsolutelySure
    {
        public ClassNameIsLongerThenThirtyTwoCharactersForAbsolutelySure()
        {
        }

        public ClassNameIsLongerThenThirtyTwoCharactersForAbsolutelySure(int num, string name)
        {
            Num = num;
            Name = name;
        }

        [ProtoMember(1)]
        public int Num { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }
    }

    #endregion
}