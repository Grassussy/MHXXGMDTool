using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MHXXGMDTool
{
    public enum Versions : uint
    {
        Version1 = 0x00010201,
        Version2 = 0x00010302
    }

    public enum Language : int
    {
        JAPANESE,
        ENGLISH,
        FRENCH,
        SPANISH,
        GERMAN,
        ITALIAN
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Header
    {
        public string Magic;
        public Versions Version;
        public Language Language;
        public ulong Unknown;
        public uint LabelCount;
        public uint SectionCount;
        public uint LabelSize;
        public uint SectionSize;
        public uint NameSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct EntryV1
    {
        public uint ID;
        public uint Unknown;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct EntryV2
    {
        public uint ID;
        public uint Unknown1;
        public uint Unknown2;
        public uint LabelOffset;
        public uint Unknown3;
    }

    public class Label
    {
        public string Name = string.Empty;
        public string Text = string.Empty;
        public int TextID = 0;
    }

    public sealed class Gmd
    {
        public List<Label> Labels = new List<Label>();

        private Header Header;
        private int HeaderLength = 0x28;
        private EntryV1 EntryV1;
        private EntryV2 EntryV2;
        private string Name;

        private byte[] UnknownSetV2;
        private List<EntryV1> EntriesV1 = new List<EntryV1>();
        private List<EntryV2> EntriesV2 = new List<EntryV2>();
        private List<String> Names = new List<String>();

        public Gmd(Stream input)
        {
            using (var br = new BinaryReader(input))
            {
                if (DataReader.PeekString(br) != "GMD\0")
                    return;

                Header.Magic = Encoding.UTF8.GetString(br.ReadBytes(4));
                Header.Version = (Versions)br.ReadUInt32();
                Header.Language = (Language)br.ReadInt32();
                Header.Unknown = br.ReadUInt64();
                Header.LabelCount = br.ReadUInt32();
                Header.SectionCount = br.ReadUInt32();
                Header.LabelSize = br.ReadUInt32();
                Header.SectionSize = br.ReadUInt32();
                Header.NameSize = br.ReadUInt32();

                Name = Encoding.UTF8.GetString(br.ReadBytes((int)Header.NameSize));

                br.BaseStream.Position++;

                if (Header.Version == Versions.Version1)
                {
                    for (var i = 0; i < (int)Header.LabelCount; i++)
                    {
                        EntryV1.ID = br.ReadUInt32();
                        EntryV1.Unknown = br.ReadUInt32();
                        EntriesV1.Add(EntryV1);
                    }
                }
                else if (Header.Version == Versions.Version2)
                {
                    for (var i = 0; i < (int)Header.LabelCount; i++)
                    {
                        EntryV2.ID = br.ReadUInt32();
                        EntryV2.Unknown1 = br.ReadUInt32();
                        EntryV2.Unknown2 = br.ReadUInt32();
                        EntryV2.LabelOffset = br.ReadUInt32();
                        EntryV2.Unknown3 = br.ReadUInt32();
                        EntriesV2.Add(EntryV2);
                    }

                    var posTemp = br.BaseStream.Position;
                    var temp = br.ReadUInt32();
                    while (temp < 0x100000 || temp == 0xffffffff) temp = br.ReadUInt32();
                    br.BaseStream.Position -= 4;

                    temp = br.ReadByte();
                    while (temp == 0) temp = br.ReadByte();
                    br.BaseStream.Position--;

                    var unknownSetSize = br.BaseStream.Position - posTemp;
                    br.BaseStream.Position = posTemp;

                    UnknownSetV2 = br.ReadBytes((int)unknownSetSize);
                }

                var counter = 0;
                for (var i = 0; i < Header.LabelCount; i++)
                {
                    if (Header.LabelSize > 0)
                        Names.Add(DataReader.ReadStringUntilNull(br));
                    else
                        Names.Add("unnamed_" + i.ToString("00000"));
                    counter = i;
                }

                for (var i = 0; i < Header.SectionCount; i++)
                {
                    Labels.Add(new Label
                    {
                        Name = i < Header.LabelCount ? Names[i] : "",
                        Text = DataReader.ReadStringUntilNull(br),
                        TextID = i
                    }
                    );

                    if (i >= Header.LabelCount)
                        counter++;
                }
            }
        }

        public void Save(Stream input)
        {
            using (var bw = new BinaryWriter(input))
            {
                bw.BaseStream.Position = HeaderLength + Header.NameSize + 1;

                if (Header.Version == Versions.Version1)
                    foreach (var entry in EntriesV1)
                    {
                        bw.Write(entry.ID);
                        bw.Write(entry.Unknown);
                    }
                else if (Header.Version == Versions.Version2)
                {
                    foreach (var entry in EntriesV2)
                    {
                        bw.Write(entry.ID);
                        bw.Write(entry.Unknown1);
                        bw.Write(entry.Unknown2);
                        bw.Write(entry.LabelOffset);
                        bw.Write(entry.Unknown3);
                    }
                    bw.Write(UnknownSetV2);
                }

                uint labelSize = 0;
                for (var i = 0; i < Header.LabelCount; i++)
                {
                    bw.Write(Encoding.UTF8.GetBytes(Names[i]));
                    bw.Write((byte)0);
                    labelSize += (uint)Names[i].Length + 1;
                }
                Header.LabelSize = labelSize;

                var textStartPos = bw.BaseStream.Position;
                foreach (var section in Labels)
                {
                    bw.Write(Encoding.UTF8.GetBytes(section.Text));
                    bw.Write((byte)0);
                }
                Header.SectionSize = (uint)(bw.BaseStream.Position - textStartPos);

                bw.BaseStream.Position = 0;

                bw.Write(Encoding.UTF8.GetBytes(Header.Magic));
                bw.Write((uint)Header.Version);
                bw.Write((int)Header.Language);
                bw.Write(Header.Unknown);
                bw.Write(Header.LabelCount);
                bw.Write(Header.SectionCount);
                bw.Write(Header.LabelSize);
                bw.Write(Header.SectionSize);
                bw.Write(Header.NameSize);

                bw.Write(Encoding.UTF8.GetBytes(Name));

                bw.Write((byte)0);
            }
        }

        public uint GetRealLabelCount()
        {
            return Header.LabelCount;
        }
    }

    internal static class DataReader
    {
        public static string PeekString(BinaryReader br, int length = 4)
        {
            var bytes = new List<byte>();
            var startOffset = br.BaseStream.Position;

            if (br.BaseStream.Position != br.BaseStream.Length)
                for (var i = 0; i < length; i++)
                    bytes.Add(br.ReadByte());

            br.BaseStream.Seek(startOffset, SeekOrigin.Begin);

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public static string ReadStringUntilNull(BinaryReader br)
        {
            var tempList = new List<byte>();
            do
            {
                if (br.BaseStream.Position != br.BaseStream.Length)
                {
                    var temp = (byte)br.ReadByte();
                    if (temp != 0)
                        tempList.Add(temp);
                    else
                        break;
                }
                else
                    break;
            } while (true);
            return Encoding.UTF8.GetString(tempList.ToArray());
        }
    }
}