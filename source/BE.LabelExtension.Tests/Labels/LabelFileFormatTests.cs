using System;
using System.IO;
using System.Linq;
using System.Text;
using BE.LabelExtension.Core.Labels;
using Xunit;

namespace BE.LabelExtension.Tests.Labels
{
    public class LabelFileFormatTests
    {
        private static readonly byte[] Bom = { 0xEF, 0xBB, 0xBF };

        /// <summary>TC01: a file in the write format, read and written again, stays the same byte for byte.</summary>
        [Fact]
        public void ReadAndWrite_FileInWriteFormat_IsUnchangedByteForByte()
        {
            byte[] original = Utf8WithBom(
                "BDM110000001=Kunde\r\n" +
                "BDM110000003=Lieferadresse\r\n" +
                " ;Adresse, an die geliefert wird\r\n" +
                "BDM110000021= \r\n" +
                " ;Text folgt\r\n" +
                "L3F2A9C15B8047DE1=Währung: %1 (für Kunden)\r\n" +
                "@DMO1001=Lieferadresse\r\n" +
                "@DMO1002= \r\n");

            byte[] written = RoundTrip(original);

            Assert.Equal(original, written);
        }

        /// <summary>
        /// Second test for the write format (F6, no own TC number): reading takes the format of
        /// the existing tool apart, writing puts it together in the write format.
        /// </summary>
        [Fact]
        public void ReadAndWrite_FileInOtherForm_IsWrittenInWriteFormat()
        {
            byte[] original = Encoding.UTF8.GetBytes(
                "; comment before the first label\n" +
                "\n" +
                "A=First\n" +
                "   # comment with hash\n" +
                " ;further comment line\n" +
                "\n" +
                "B=\n" +
                "A=Duplicate\n" +
                " ;comment of the duplicate\n" +
                "C=Last");

            byte[] written = RoundTrip(original);

            Assert.Equal(Encoding.UTF8.GetBytes("A=First\r\n ; comment with hash\r\nB= \r\nC=Last\r\n"), written);
        }

        /// <summary>
        /// F12: a file without byte order mark keeps it that way. The existing tool writes one
        /// into every file.
        /// </summary>
        [Fact]
        public void ReadAndWrite_FileWithoutByteOrderMark_IsUnchangedByteForByte()
        {
            byte[] original = Encoding.UTF8.GetBytes(
                "BDM110000001=Kunde\r\n" +
                "BDM110000003=Lieferadresse\r\n" +
                " ;Adresse, an die geliefert wird\r\n" +
                "L3F2A9C15B8047DE1=Währung: %1 (für Kunden)\r\n" +
                "@DMO1002= \r\n");

            byte[] written = RoundTrip(original);

            Assert.Equal(original, written);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Read_ByteOrderMark_IsRecordedAndTheTextIsTheSame(bool withByteOrderMark)
        {
            byte[] text = Encoding.UTF8.GetBytes("A=Währung\r\n");
            using var stream = new MemoryStream(withByteOrderMark ? Bom.Concat(text).ToArray() : text);

            LabelFileContent content = LabelFileFormat.Read(stream);

            Assert.Equal(withByteOrderMark, content.HasByteOrderMark);
            Assert.Equal("Währung", content.Entries.Single().Text);
        }

        [Fact]
        public void Read_ShortFile_HasNoByteOrderMark()
        {
            using var stream = new MemoryStream(new byte[] { 0xEF, 0xBB });

            Assert.False(LabelFileFormat.Read(stream).HasByteOrderMark);
        }

        [Fact]
        public void Read_FileInOtherForm_ReportsWhatTheNextSaveDrops()
        {
            LabelFileContent content = Read("A=First\n ;one\n ;two\nA=Duplicate\nB=Second\n");

            Assert.False(content.IsDamaged);
            Assert.True(content.NeedsCleanup);
            Assert.Collection(
                content.Issues,
                i => { Assert.Equal(LabelFileIssueKind.ExtraCommentLine, i.Kind); Assert.Equal(3, i.Line); Assert.Equal("A", i.Key); },
                i => { Assert.Equal(LabelFileIssueKind.DuplicateId, i.Kind); Assert.Equal(4, i.Line); Assert.Equal("A", i.Key); });
            Assert.Equal(new[] { "A", "B" }, content.Entries.Select(e => e.Key));
            Assert.Equal("First", content.Entries[0].Text);
            Assert.Equal("one", content.Entries[0].Comment);
        }

        [Theory]
        [InlineData("A=First\nno equals sign\nB=Second")]
        [InlineData("A=First\n=no key\n")]
        public void Read_LineThatIsNoLabel_MarksTheFileAsDamaged(string text)
        {
            LabelFileContent content = Read(text);

            Assert.True(content.IsDamaged);
            Assert.Equal(2, content.Issues.Single().Line);
        }

        [Fact]
        public void Read_TextUpToFirstEqualsSign_KeepsFurtherEqualsSigns()
        {
            LabelEntry entry = Read("A=x = y\n").Entries.Single();

            Assert.Equal("A", entry.Key);
            Assert.Equal("x = y", entry.Text);
        }

        [Fact]
        public void Read_OneSpace_IsAnEmptyText()
        {
            Assert.Equal(string.Empty, Read("A= \r\n").Entries.Single().Text);
        }

        [Fact]
        public void Read_WithoutByteOrderMark_IsUtf8()
        {
            Assert.Equal("Währung", Read("A=Währung\r\n").Entries.Single().Text);
        }

        [Fact]
        public void WriteFile_ExistingFile_IsReplacedWithoutTemporaryFileLeft()
        {
            string folder = Path.Combine(Path.GetTempPath(), "BE.LabelExtension.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                string path = Path.Combine(folder, "BDM1.de.label.txt");
                File.WriteAllText(path, "OLD=Old\r\n");

                LabelFileFormat.WriteFile(path, new[] { new LabelEntry("NEW", "Neu", "Kommentar") });

                Assert.Equal(Utf8WithBom("NEW=Neu\r\n ;Kommentar\r\n"), File.ReadAllBytes(path));
                Assert.Equal(new[] { path }, Directory.GetFiles(folder));
            }
            finally
            {
                Directory.Delete(folder, recursive: true);
            }
        }

        [Theory]
        [InlineData("A\r\nB", "Text", null)]
        [InlineData("A=B", "Text", null)]
        [InlineData("A", "Line\nbreak", null)]
        [InlineData("A", "Text", "Line\rbreak")]
        public void Write_EntryThatWouldBreakTheFormat_IsRejected(string key, string text, string? comment)
        {
            using var stream = new MemoryStream();
            Assert.Throws<ArgumentException>(() => LabelFileFormat.Write(stream, new[] { new LabelEntry(key, text, comment) }));
        }

        private static LabelFileContent Read(string text)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            return LabelFileFormat.Read(stream);
        }

        private static byte[] RoundTrip(byte[] original)
        {
            LabelFileContent content;
            using (var input = new MemoryStream(original))
            {
                content = LabelFileFormat.Read(input);
            }

            using var output = new MemoryStream();
            LabelFileFormat.Write(output, content.Entries, content.HasByteOrderMark);
            return output.ToArray();
        }

        private static byte[] Utf8WithBom(string text) => Bom.Concat(Encoding.UTF8.GetBytes(text)).ToArray();
    }
}
