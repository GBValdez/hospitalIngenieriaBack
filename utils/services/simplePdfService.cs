using System.Globalization;
using System.Text;

namespace project.utils.services
{
    public class simplePdfService
    {
        public byte[] CreateDocument(string title, IEnumerable<string> lines)
        {
            StringBuilder stream = new StringBuilder();
            decimal y = 748;

            stream.AppendLine("q");
            stream.AppendLine("0.10 0.20 0.36 rg");
            stream.AppendLine("40 724 532 48 re f");
            stream.AppendLine("Q");
            AppendText(stream, title, 56, 748, 20, true, "1 1 1 rg");
            AppendText(stream, $"Generado: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC", 56, 730, 9, false, "0.86 0.91 0.98 rg");

            y = 698;
            foreach (string rawLine in lines.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                string line = rawLine.Trim();
                if (line.StartsWith("# "))
                {
                    y -= 12;
                    AppendSection(stream, line.Substring(2), y);
                    y -= 24;
                    continue;
                }

                if (y < 68)
                {
                    AppendText(stream, "El documento continua en el sistema.", 56, y, 10, false, "0.35 0.40 0.48 rg");
                    break;
                }

                string label = string.Empty;
                string value = line;
                int separatorIndex = line.IndexOf(':');
                if (separatorIndex > 0)
                {
                    label = line.Substring(0, separatorIndex).Trim();
                    value = line.Substring(separatorIndex + 1).Trim();
                }

                List<string> wrappedValue = Wrap(value, label.Length > 0 ? 58 : 82);
                int rowHeight = Math.Max(22, 14 + (wrappedValue.Count * 12));
                AppendRowBackground(stream, y - rowHeight + 7, rowHeight);
                if (!string.IsNullOrWhiteSpace(label))
                    AppendText(stream, label, 56, y - 8, 9, true, "0.20 0.28 0.38 rg");

                decimal valueX = string.IsNullOrWhiteSpace(label) ? 56 : 192;
                for (int i = 0; i < wrappedValue.Count; i++)
                    AppendText(stream, wrappedValue[i], valueX, y - 8 - (i * 12), 9, false, "0.16 0.20 0.27 rg");

                y -= rowHeight + 4;
            }

            AppendText(stream, "Hospital Codigo", 40, 28, 8, false, "0.45 0.50 0.58 rg");
            AppendText(stream, "Documento generado automaticamente", 392, 28, 8, false, "0.45 0.50 0.58 rg");

            string content = stream.ToString();
            List<string> objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
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

        private static void AppendSection(StringBuilder stream, string text, decimal y)
        {
            stream.AppendLine("q");
            stream.AppendLine("0.89 0.93 0.98 rg");
            stream.AppendLine($"40 {ToPdf(y - 8)} 532 22 re f");
            stream.AppendLine("0.72 0.78 0.86 RG");
            stream.AppendLine($"40 {ToPdf(y - 8)} 532 22 re S");
            stream.AppendLine("Q");
            AppendText(stream, text, 52, y - 2, 11, true, "0.10 0.20 0.36 rg");
        }

        private static void AppendRowBackground(StringBuilder stream, decimal y, int height)
        {
            stream.AppendLine("q");
            stream.AppendLine("0.98 0.99 1 rg");
            stream.AppendLine($"40 {ToPdf(y)} 532 {height} re f");
            stream.AppendLine("0.86 0.89 0.94 RG");
            stream.AppendLine($"40 {ToPdf(y)} 532 {height} re S");
            stream.AppendLine("Q");
        }

        private static void AppendText(
            StringBuilder stream,
            string text,
            decimal x,
            decimal y,
            int fontSize,
            bool bold,
            string color)
        {
            stream.AppendLine("BT");
            stream.AppendLine(color);
            stream.AppendLine($"/{(bold ? "F2" : "F1")} {fontSize} Tf");
            stream.AppendLine($"{ToPdf(x)} {ToPdf(y)} Td");
            stream.AppendLine($"({Escape(text)}) Tj");
            stream.AppendLine("ET");
        }

        private static List<string> Wrap(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new List<string> { "-" };

            List<string> lines = new List<string>();
            string[] words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            StringBuilder line = new StringBuilder();
            foreach (string word in words)
            {
                if (line.Length > 0 && line.Length + word.Length + 1 > maxLength)
                {
                    lines.Add(line.ToString());
                    line.Clear();
                }

                if (line.Length > 0)
                    line.Append(' ');
                line.Append(word);
            }

            if (line.Length > 0)
                lines.Add(line.ToString());

            return lines.Count == 0 ? new List<string> { value } : lines;
        }

        private static string ToPdf(decimal value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
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
