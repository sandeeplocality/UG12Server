using System;
using System.Collections.Generic;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace SiteWelfareChecking
{
    public class Report
    {
        public static string companyName;
        public static string fileName;
        public static string logoPath;
        public static List<string[]> visitsHistory;
    }

    public class PDFReport
    {
        public string CompanyName
        {
            set { Report.companyName = value; }
        }

        public string FileName
        {
            set { Report.fileName = value; }
            get { return Report.fileName; }
        }

        public string LogoPath
        {
            set { Report.logoPath = value; }
        }

        public List<string[]> VisitsHistory
        {
            set { Report.visitsHistory = value; }
        }

        public void Save()
        {
            // Create a MigraDoc document
            Document document = Documents.CreateDocument();

            // Write to Data Definition Language
            MigraDoc.DocumentObjectModel.IO.DdlWriter.WriteToFile(document, "MigraDoc.mdddl");

            // Render the document
            PdfDocumentRenderer render = new PdfDocumentRenderer(true, PdfSharp.Pdf.PdfFontEmbedding.Always);
            render.Document = document;
            render.RenderDocument();

            // Save the document
            render.PdfDocument.Save(Report.fileName);
        }

        public void Open()
        {
            System.Diagnostics.Process.Start(Report.fileName);
        }
    }

    class Documents
    {
        public static Document CreateDocument()
        {
            // Create a new document
            Document document = new Document();
            document.Info.Title   = "Welfare Check Breach Report";
            document.Info.Subject = "Reports on staff movements prior to the breach of welfare check time allowance.";
            document.Info.Author  = "UniGuard 12: Online Management System";

            // Add content to document
            Styles.DefineStyles(document);
            DefineContentSection(document);
            Tables.RenderDataTable(document);

            return document;
        }

        /// <summary>
        /// Defines page setup, headers, and footers
        /// </summary>
        /// <param name="document">MigraDoc document created via Documents.CreateDocument()</param>
        public static void DefineContentSection(Document document)
        {
            Section section = document.AddSection();
            section.PageSetup.TopMargin = "5cm";
            section.PageSetup.StartingNumber = 1;
            section.PageSetup.Orientation = Orientation.Landscape;

            // Create header
            HeaderFooter header = section.Headers.Primary;

            // Add logo
            Image image = header.AddImage(Report.logoPath);
            image.Width = "8cm";
            image.WrapFormat.Style = WrapStyle.Through;

            // Add company name
            Paragraph h1 = new Paragraph();
            h1.AddText(Report.companyName);
            h1.Format.Font.Color = Colors.SlateGray;
            h1.Format.Font.Size = 24;
            h1.Format.Font.Bold = true;
            header.Add(h1);

            // Add report name
            Paragraph h2 = new Paragraph();
            h2.Format.Font.Color = Colors.SlateGray;
            h2.AddText("Welfare Check Breach Report");
            h2.Format.Font.Size = 18;
            h2.Format.Borders.Width = 0;
            h2.Format.Borders.Bottom.Width = 1;
            h2.Format.Borders.Color = Colors.LightSlateGray;
            h2.Format.Borders.Distance = 4;
            header.Add(h2);

            // Add current date
            Paragraph h3 = new Paragraph();
            h3.Format.Font.Size = 12;
            h3.Format.Font.Italic = true;
            h3.Format.Borders.Distance = 6;
            h3.AddDateField();
            header.Add(h3);

            // Create a paragraph with centered page number. See definition of style "Footer".
            Paragraph footer = new Paragraph();
            footer.Format.Borders.Width = 0;
            footer.Format.Borders.Top.Width = 1;
            footer.Format.Borders.Color = Colors.LightSlateGray;
            footer.Format.Borders.Distance = 10;
            footer.AddPageField();

            // Add paragraph to footer
            section.Footers.Primary.Add(footer);
        }
    }

    class Styles
    {
        /// <summary>
        /// Defines the styles used in the document
        /// </summary>
        /// <param name="document">MigraDoc document created via Documents.CreateDocument()</param>
        public static void DefineStyles(Document document)
        {
            // Get the predefined style Normal.
            Style style = document.Styles["Normal"];
            // Because all styles are derived from Normal, the next line changes the font of the whole
            // document. Or, more exactly, it changes the font of all styles and paragraphs that do not
            // redefine the font.
            style.Font.Name = "Verdana";

            // Heading1 to Heading9 are predefined styles with an outline level. An outline level other
            // than OutlineLevel.BodyText automatically creates the outline (or bookmarks) in PDF.

            style = document.Styles["Heading1"];
            style.Font.Size = 14;
            style.Font.Bold = true;
            style.Font.Color = Colors.DodgerBlue;
            style.ParagraphFormat.PageBreakBefore = true;
            style.ParagraphFormat.SpaceAfter = 6;

            style = document.Styles["Heading2"];
            style.Font.Size = 12;
            style.Font.Bold = true;
            style.Font.Color = Colors.DodgerBlue;
            style.ParagraphFormat.PageBreakBefore = false;
            style.ParagraphFormat.SpaceBefore = 6;
            style.ParagraphFormat.SpaceAfter = 6;

            style = document.Styles["Heading3"];
            style.Font.Size = 10;
            style.Font.Bold = true;
            style.Font.Italic = true;
            style.ParagraphFormat.SpaceBefore = 6;
            style.ParagraphFormat.SpaceAfter = 3;

            style = document.Styles[StyleNames.Header];
            style.Font.Name = "Century Gothic";
            style.ParagraphFormat.Alignment = ParagraphAlignment.Right;

            style = document.Styles[StyleNames.Footer];
            style.ParagraphFormat.Alignment = ParagraphAlignment.Center;
        }
    }

    class Tables
    {
        public static void RenderDataTable(Document document)
        {
            int count = Report.visitsHistory.Count / 7;

            // Add text above table
            Paragraph p = document.LastSection.AddParagraph("Displaying last " + count + " recorded patrol hits within the last 24 hours.\n\n");
            p.Format.Font.Bold = true;
            p.Format.Font.Italic = true;

            Table table = document.LastSection.AddTable();
            table.Borders.Visible = true;
            table.Borders.Color = Colors.SlateGray;
            table.TopPadding = 5;
            table.BottomPadding = 5;

            // Add all columns
            Column column = table.AddColumn(Unit.FromCentimeter(5));
            table.AddColumn(Unit.FromCentimeter(6));
            table.AddColumn(Unit.FromCentimeter(6));
            table.AddColumn();
            table.AddColumn();
            table.AddColumn(Unit.FromCentimeter(4));

            // Add header columns
            Row row = table.AddRow();
            row.Shading.Color = Colors.DodgerBlue;
            row.Format.Font.Bold = true;
            row.Format.Font.Color = Colors.White;
            row.Format.Font.Size = 9;
            Cell cell = row.Cells[0];
            cell.AddParagraph("Region");
            cell = row.Cells[1];
            cell.AddParagraph("Site");
            cell = row.Cells[2];
            cell.AddParagraph("Checkpoint");
            cell = row.Cells[3];
            cell.AddParagraph("Date");
            cell = row.Cells[4];
            cell.AddParagraph("Time");
            cell = row.Cells[5];
            cell.AddParagraph("Recorder");

            // Create the history object
            string[][] history = Report.visitsHistory.ToArray();

            // Add rows
            for (int x = 0; x < count; x++)
            {
                row = table.AddRow();
                row.Shading.Color = (x % 2 == 0) ? Colors.LightCyan : Colors.LightBlue;
                row.Format.Font.Size = 8;
                cell = row.Cells[0];
                cell.AddParagraph(history[x][5].ToString()); // region
                cell = row.Cells[1];
                cell.AddParagraph(history[x][4].ToString()); // site
                cell = row.Cells[2];
                cell.AddParagraph(history[x][3].ToString()); // checkpoint
                cell = row.Cells[3];
                cell.AddParagraph(history[x][0].ToString()); // date
                cell = row.Cells[4];
                cell.AddParagraph(history[x][1].ToString()); // time
                cell = row.Cells[5];
                cell.AddParagraph(history[x][6].ToString()); // recorder name
            }
        }
    }
}
