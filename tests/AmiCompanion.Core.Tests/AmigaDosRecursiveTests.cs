using System.Buffers.Binary;
using System.Text;
using AmiCompanion.Core.FileSystems;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AmigaDosRecursiveTests
{
    [Fact]
    public void ListsDirectoryAndNestedFile()
    {
        var image = AmigaDosFormatter.FormatAdf("TEST");
        const int dirBlock = 100;
        const int fileBlock = 101;
        var root = image.AsSpan(880 * 512, 512);
        Write(root, 6, dirBlock);
        Fix(root, 5);

        var dir = image.AsSpan(dirBlock * 512, 512);
        Write(dir, 0, 2); Write(dir, 124, 0); Write(dir, 127, 2);
        dir[432] = 1; dir[433] = (byte)'C';
        Write(dir, 6, fileBlock); Fix(dir, 5);

        var file = image.AsSpan(fileBlock * 512, 512);
        Write(file, 0, 2); Write(file, 3, 42); Write(file, 124, 0); Write(file, 127, unchecked((uint)-3));
        file[432] = 3; Encoding.Latin1.GetBytes("foo").CopyTo(file[433..]);

        var entries = AmigaDosReader.ListAll(image);
        Assert.Contains(entries, x => x.Path == "C" && x.Entry.IsDirectory);
        Assert.Contains(entries, x => x.Path == "C/foo" && x.Entry.IsFile && x.Entry.ByteSize == 42);
    }

    static void Write(Span<byte> b,int n,uint v)=>BinaryPrimitives.WriteUInt32BigEndian(b.Slice(n*4,4),v);
    static void Fix(Span<byte> b,int n){Write(b,n,0);uint s=0;for(int i=0;i<128;i++)s=unchecked(s+BinaryPrimitives.ReadUInt32BigEndian(b.Slice(i*4,4)));Write(b,n,unchecked(0u-s));}
}
