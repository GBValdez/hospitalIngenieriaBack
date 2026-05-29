using System.Globalization;
using System.Text;

namespace project.utils.services
{
    public class simplePdfService
    {
        public byte[] CreateDocument(string title, IEnumerable<string> lines)
        {
            List<string> contentLines = new List<string> { title };
            contentLines.AddRange(lines.Where(x => !string.IsNullOrWhiteSpace(x)));

            StringBuilder stream = new StringBuilder();
            stream.AppendLine("BT");
            stream.AppendLine("/F1 18 Tf");
            stream.AppendLine("50 780 Td");
            stream.AppendLine($"({Escape(title)}) Tj");
            stream.AppendLine("/F1 11 Tf");
            stream.AppendLine("0 -30 Td");

            foreach (string line in contentLines.Skip(1))
            {
                stream.AppendLine($"({Escape(line)}) Tj");
                stream.AppendLine("0 -18 Td");
            }
            stream.AppendLine("ET");

            string content = stream.ToString();
            List<string> objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream"
            };

            MemoryStream memory = new MemoryStream();
            Write(memory, "%PDF-1.4\n");
            List<long> offsets = new List<long> { 0 };
            for (int i = 0; i < objects.Count; i++)
            {
                offsets.Add(memory.Position);
                Write(memory, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            long xrefPosition = memory.Position;
            Write(memory, $"xref\n0 {objects.Count + 1}\n");
            Write(memory, "0000000000 65535 f \n");
            foreach (long offset in offsets.Skip(1))
                Write(memory, $"{offset.ToString("0000000000", CultureInfo.InvariantCulture)} 00000 n \n");

            Write(memory, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF");
            return memory.ToArray();
        }

        private static void Write(Stream stream, string value)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static string Escape(string value)
        {
            string normalized = value.Normalize(NormalizationForm.FormD);
            StringBuilder builder = new StringBuilder();
            foreach (char character in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;

                char safeChar = character > 127 ? '?' : character;
                if (safeChar == '(' || safeChar == ')' || safeChar == '\\')
                    builder.Append('\\');
                builder.Append(safeChar);
            }

            return builder.ToString();
        }
    }
}
