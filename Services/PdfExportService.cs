using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using PersonalJournalApp.Models.Display;
using System.Text.RegularExpressions;
using System.Web;

// Create type aliases to avoid ambiguity with MAUI types
using QColors = QuestPDF.Helpers.Colors;
using QPageSizes = QuestPDF.Helpers.PageSizes;

namespace PersonalJournalApp.Services;

public class PdfExportService
{
    public PdfExportService()
    {
        // Set QuestPDF license 
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string?> ExportSingleEntryAsync(JournalEntryDisplayModel entry)
    {
        try
        {
            var fileName = $"journal_entry_{entry.CreatedDate.ToLocalTime():yyyy-MM-dd_HHmmss}.pdf";
            var filePath = await GetSavePathAsync(fileName);

            if (string.IsNullOrEmpty(filePath)) return null;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QPageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(QColors.Grey.Darken3));

                    page.Header().Element(header => ComposeEntryHeader(header, entry));
                    page.Content().Element(content => ComposeEntryContent(content, entry));
                    page.Footer().Element(footer => ComposeFooter(footer));
                });
            });

            document.GeneratePdf(filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDF Export Error: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> ExportMultipleEntriesAsync(List<JournalEntryDisplayModel> entries)
    {
        try
        {
            var fileName = $"journal_entries_{DateTime.Now:yyyy-MM-dd_HHmmss}.pdf";
            var filePath = await GetSavePathAsync(fileName);

            if (string.IsNullOrEmpty(filePath)) return null;

            var document = Document.Create(container =>
            {
                // Cover page
                container.Page(page =>
                {
                    page.Size(QPageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(QColors.Grey.Darken3));

                    page.Content().Column(column =>
                    {
                        column.Spacing(20);

                        column.Item().PaddingTop(100).AlignCenter().Text("Personal Journal")
                            .FontSize(32).Bold().FontColor(QColors.Indigo.Medium);

                        column.Item().AlignCenter().Text("Exported Entries")
                            .FontSize(18).FontColor(QColors.Grey.Darken1);

                        column.Item().PaddingTop(30).AlignCenter().Text($"{entries.Count} entries")
                            .FontSize(14).FontColor(QColors.Grey.Medium);

                        var dateRange = entries.Any()
                            ? $"{entries.Min(e => e.CreatedDate).ToLocalTime():MMM dd, yyyy} - {entries.Max(e => e.CreatedDate).ToLocalTime():MMM dd, yyyy}"
                            : "No entries";
                        column.Item().AlignCenter().Text(dateRange)
                            .FontSize(12).FontColor(QColors.Grey.Medium);

                        column.Item().PaddingTop(30).AlignCenter().Text($"Generated: {DateTime.Now:MMMM dd, yyyy}")
                            .FontSize(10).FontColor(QColors.Grey.Lighten1);
                    });

                    page.Footer().Element(footer => ComposeFooter(footer));
                });

                // Each entry on its own page(s)
                foreach (var entry in entries.OrderByDescending(e => e.CreatedDate))
                {
                    container.Page(page =>
                    {
                        page.Size(QPageSizes.A4);
                        page.Margin(40);
                        page.DefaultTextStyle(x => x.FontSize(11).FontColor(QColors.Grey.Darken3));

                        page.Header().Element(header => ComposeEntryHeader(header, entry));
                        page.Content().Element(content => ComposeEntryContent(content, entry));
                        page.Footer().Element(footer => ComposeFooter(footer));
                    });
                }
            });

            document.GeneratePdf(filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDF Export Error: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> ExportEntriesByDateRangeAsync(List<JournalEntryDisplayModel> entries, DateTime startDate, DateTime endDate)
    {
        try
        {
            var fileName = $"journal_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}.pdf";
            var filePath = await GetSavePathAsync(fileName);

            if (string.IsNullOrEmpty(filePath)) return null;

            var document = Document.Create(container =>
            {
                // Cover page
                container.Page(page =>
                {
                    page.Size(QPageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(QColors.Grey.Darken3));

                    page.Content().Column(column =>
                    {
                        column.Spacing(20);

                        column.Item().PaddingTop(100).AlignCenter().Text("Personal Journal")
                            .FontSize(32).Bold().FontColor(QColors.Indigo.Medium);

                        column.Item().AlignCenter().Text("Date Range Export")
                            .FontSize(18).FontColor(QColors.Grey.Darken1);

                        column.Item().PaddingTop(20).AlignCenter().Text($"{startDate:MMMM dd, yyyy} - {endDate:MMMM dd, yyyy}")
                            .FontSize(14).FontColor(QColors.Indigo.Lighten2);

                        column.Item().PaddingTop(10).AlignCenter().Text($"{entries.Count} entries")
                            .FontSize(14).FontColor(QColors.Grey.Medium);

                        column.Item().PaddingTop(30).AlignCenter().Text($"Generated: {DateTime.Now:MMMM dd, yyyy}")
                            .FontSize(10).FontColor(QColors.Grey.Lighten1);
                    });

                    page.Footer().Element(footer => ComposeFooter(footer));
                });

                // Each entry on its own page(s)
                foreach (var entry in entries.OrderByDescending(e => e.CreatedDate))
                {
                    container.Page(page =>
                    {
                        page.Size(QPageSizes.A4);
                        page.Margin(40);
                        page.DefaultTextStyle(x => x.FontSize(11).FontColor(QColors.Grey.Darken3));

                        page.Header().Element(header => ComposeEntryHeader(header, entry));
                        page.Content().Element(content => ComposeEntryContent(content, entry));
                        page.Footer().Element(footer => ComposeFooter(footer));
                    });
                }
            });

            document.GeneratePdf(filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDF Export Error: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> ExportAnalyticsReportAsync(AnalyticsReportData reportData)
    {
        try
        {
            var fileName = $"journal_analytics_{DateTime.Now:yyyy-MM-dd_HHmmss}.pdf";
            var filePath = await GetSavePathAsync(fileName);

            if (string.IsNullOrEmpty(filePath)) return null;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QPageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(QColors.Grey.Darken3));

                    page.Header().Element(header => ComposeAnalyticsHeader(header, reportData));
                    page.Content().Element(content => ComposeAnalyticsContent(content, reportData));
                    page.Footer().Element(footer => ComposeFooter(footer));
                });
            });

            document.GeneratePdf(filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDF Export Error: {ex.Message} \nStack: {ex.StackTrace}");
            return null;
        }
    }

    private Task<string?> GetSavePathAsync(string defaultFileName)
    {
        try
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (string.IsNullOrEmpty(documentsPath) || !Directory.Exists(documentsPath))
            {
                documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                if (!string.IsNullOrEmpty(documentsPath))
                {
                    documentsPath = Path.Combine(documentsPath, "Documents");
                    if (!Directory.Exists(documentsPath))
                    {
                        Directory.CreateDirectory(documentsPath);
                    }
                }
            }

            if (string.IsNullOrEmpty(documentsPath) || !Directory.Exists(documentsPath))
            {
                documentsPath = FileSystem.AppDataDirectory;
            }

            var filePath = Path.Combine(documentsPath, defaultFileName);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return Task.FromResult<string?>(filePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetSavePathAsync Error: {ex.Message}");
            var cachePath = Path.Combine(FileSystem.CacheDirectory, defaultFileName);
            return Task.FromResult<string?>(cachePath);
        }
    }

    private void ComposeEntryHeader(QuestPDF.Infrastructure.IContainer container, JournalEntryDisplayModel entry)
    {
        container.Column(column =>
        {
            column.Spacing(8);

            var title = string.IsNullOrEmpty(entry.Title) ? "Untitled Entry" : entry.Title;
            column.Item().Text(title).FontSize(24).Bold().FontColor(QColors.Grey.Darken4);

            column.Item().Text($"{entry.CreatedDate.ToLocalTime():dddd, MMMM d, yyyy} at {entry.FormattedTime}")
                .FontSize(12).FontColor(QColors.Grey.Medium);

            column.Item().PaddingTop(8).LineHorizontal(2).LineColor(QColors.Indigo.Medium);
        });
    }

    private void ComposeEntryContent(QuestPDF.Infrastructure.IContainer container, JournalEntryDisplayModel entry)
    {
        container.PaddingTop(20).Column(column =>
        {
            column.Spacing(15);

            // Info grid
            column.Item().Background(QColors.Grey.Lighten4).Padding(15).Column(infoColumn =>
            {
                infoColumn.Spacing(10);

                var moods = FormatMoodName(entry.PrimaryMood);
                if (!string.IsNullOrEmpty(entry.SecondaryMood1))
                    moods += ", " + FormatMoodName(entry.SecondaryMood1);
                if (!string.IsNullOrEmpty(entry.SecondaryMood2))
                    moods += ", " + FormatMoodName(entry.SecondaryMood2);

                infoColumn.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Mood").FontSize(10).FontColor(QColors.Grey.Medium).Bold();
                        c.Item().Text(moods).FontSize(12).FontColor(QColors.Grey.Darken3);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Word Count").FontSize(10).FontColor(QColors.Grey.Medium).Bold();
                        c.Item().Text($"{entry.WordCount} words").FontSize(12).FontColor(QColors.Grey.Darken3);
                    });
                });

                infoColumn.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Category").FontSize(10).FontColor(QColors.Grey.Medium).Bold();
                        c.Item().Text(string.IsNullOrEmpty(entry.CategoryName) ? "None" : entry.CategoryName)
                            .FontSize(12).FontColor(QColors.Grey.Darken3);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Tags").FontSize(10).FontColor(QColors.Grey.Medium).Bold();
                        c.Item().Text(entry.TagNames.Any() ? string.Join(", ", entry.TagNames) : "None")
                            .FontSize(12).FontColor(QColors.Grey.Darken3);
                    });
                });
            });

            // Rich text content
            column.Item().PaddingTop(10).Element(c => RenderRichTextContent(c, entry.Content));
        });
    }

    // Renders HTML content with rich text formatting (bold, italic, headings, lists, etc.)
    private void RenderRichTextContent(QuestPDF.Infrastructure.IContainer container, string htmlContent)
    {
        if (string.IsNullOrEmpty(htmlContent))
        {
            container.Text("No content").FontSize(12).FontColor(QColors.Grey.Medium);
            return;
        }

        container.Column(column =>
        {
            column.Spacing(8);

            // Parse HTML into blocks
            var blocks = ParseHtmlBlocks(htmlContent);

            foreach (var block in blocks)
            {
                switch (block.Type)
                {
                    case BlockType.Heading1:
                        column.Item().Text(text => RenderInlineStyles(text, block.Content, 20, true));
                        break;

                    case BlockType.Heading2:
                        column.Item().Text(text => RenderInlineStyles(text, block.Content, 16, true));
                        break;

                    case BlockType.Heading3:
                        column.Item().Text(text => RenderInlineStyles(text, block.Content, 14, true));
                        break;

                    case BlockType.OrderedListItem:
                        column.Item().Row(row =>
                        {
                            row.ConstantItem(25).Text($"{block.ListIndex}.").FontSize(12);
                            row.RelativeItem().Text(text => RenderInlineStyles(text, block.Content, 12, false));
                        });
                        break;

                    case BlockType.UnorderedListItem:
                        column.Item().Row(row =>
                        {
                            row.ConstantItem(25).Text("•").FontSize(12);
                            row.RelativeItem().Text(text => RenderInlineStyles(text, block.Content, 12, false));
                        });
                        break;

                    case BlockType.Blockquote:
                        column.Item().BorderLeft(3).BorderColor(QColors.Grey.Lighten1)
                            .PaddingLeft(10).Background(QColors.Grey.Lighten4).Padding(8)
                            .Text(text =>
                            {
                                text.DefaultTextStyle(style => style.FontColor(QColors.Grey.Darken1).Italic());
                                RenderInlineStyles(text, block.Content, 12, false);
                            });
                        break;

                    case BlockType.CodeBlock:
                        column.Item().Background(QColors.Grey.Lighten3).Padding(10)
                            .Text(block.Content).FontSize(10).FontColor(QColors.Grey.Darken3);
                        break;

                    case BlockType.Paragraph:
                    default:
                        if (!string.IsNullOrWhiteSpace(block.Content))
                        {
                            column.Item().Text(text =>
                            {
                                text.DefaultTextStyle(style => style.LineHeight(1.6f));
                                RenderInlineStyles(text, block.Content, 12, false);
                            });
                        }
                        break;
                }
            }
        });
    }

    // Renders inline styles (bold, italic, underline, colors) within a text block
    private void RenderInlineStyles(TextDescriptor text, string htmlContent, int fontSize, bool isBold)
    {
        // Parse inline HTML elements
        var segments = ParseInlineElements(htmlContent);

        foreach (var segment in segments)
        {
            var span = text.Span(segment.Text);
            span.FontSize(fontSize);

            // Apply base bold if heading
            if (isBold) span.Bold();

            // Apply inline styles
            if (segment.IsBold) span.Bold();
            if (segment.IsItalic) span.Italic();
            if (segment.IsUnderline) span.Underline();
            if (segment.IsStrikethrough) span.Strikethrough();

            // Apply text color
            if (!string.IsNullOrEmpty(segment.Color))
            {
                var color = ParseColor(segment.Color);
                if (color.HasValue) span.FontColor(color.Value);
            }

            // Apply background color
            if (!string.IsNullOrEmpty(segment.BackgroundColor))
            {
                var bgColor = ParseColor(segment.BackgroundColor);
                if (bgColor.HasValue) span.BackgroundColor(bgColor.Value);
            }
        }
    }

    // Parse HTML into block-level elements
    private List<ContentBlock> ParseHtmlBlocks(string html)
    {
        var blocks = new List<ContentBlock>();

        // Clean up the HTML
        html = html.Replace("\r\n", "\n").Replace("\r", "\n");

        // Split by block-level tags
        var blockPattern = @"<(h1|h2|h3|p|ol|ul|blockquote|pre)[^>]*>(.*?)</\1>|<(li)[^>]*>(.*?)</li>|<br\s*/?>|([^<]+)";
        var matches = Regex.Matches(html, blockPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        var currentListType = ListType.None;
        var listIndex = 0;

        foreach (Match match in matches)
        {
            var tagName = match.Groups[1].Value.ToLower();
            var content = match.Groups[2].Value;

            if (string.IsNullOrEmpty(tagName))
            {
                tagName = match.Groups[3].Value.ToLower();
                content = match.Groups[4].Value;
            }

            // Handle plain text between tags
            if (string.IsNullOrEmpty(tagName) && !string.IsNullOrEmpty(match.Groups[5].Value))
            {
                var plainText = match.Groups[5].Value.Trim();
                if (!string.IsNullOrWhiteSpace(plainText))
                {
                    blocks.Add(new ContentBlock { Type = BlockType.Paragraph, Content = plainText });
                }
                continue;
            }

            switch (tagName)
            {
                case "h1":
                    blocks.Add(new ContentBlock { Type = BlockType.Heading1, Content = content });
                    break;
                case "h2":
                    blocks.Add(new ContentBlock { Type = BlockType.Heading2, Content = content });
                    break;
                case "h3":
                    blocks.Add(new ContentBlock { Type = BlockType.Heading3, Content = content });
                    break;
                case "p":
                    blocks.Add(new ContentBlock { Type = BlockType.Paragraph, Content = content });
                    break;
                case "ol":
                    currentListType = ListType.Ordered;
                    listIndex = 0;
                    // Parse list items within
                    var olItems = Regex.Matches(content, @"<li[^>]*>(.*?)</li>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    foreach (Match item in olItems)
                    {
                        listIndex++;
                        blocks.Add(new ContentBlock { Type = BlockType.OrderedListItem, Content = item.Groups[1].Value, ListIndex = listIndex });
                    }
                    currentListType = ListType.None;
                    break;
                case "ul":
                    currentListType = ListType.Unordered;
                    var ulItems = Regex.Matches(content, @"<li[^>]*>(.*?)</li>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    foreach (Match item in ulItems)
                    {
                        blocks.Add(new ContentBlock { Type = BlockType.UnorderedListItem, Content = item.Groups[1].Value });
                    }
                    currentListType = ListType.None;
                    break;
                case "li":
                    if (currentListType == ListType.Ordered)
                    {
                        listIndex++;
                        blocks.Add(new ContentBlock { Type = BlockType.OrderedListItem, Content = content, ListIndex = listIndex });
                    }
                    else
                    {
                        blocks.Add(new ContentBlock { Type = BlockType.UnorderedListItem, Content = content });
                    }
                    break;
                case "blockquote":
                    blocks.Add(new ContentBlock { Type = BlockType.Blockquote, Content = content });
                    break;
                case "pre":
                    blocks.Add(new ContentBlock { Type = BlockType.CodeBlock, Content = StripAllHtml(content) });
                    break;
            }
        }

        // If no blocks were parsed, treat the entire content as a single block
        if (blocks.Count == 0 && !string.IsNullOrWhiteSpace(html))
        {
            // Try to split by <p> tags or line breaks
            var paragraphs = Regex.Split(html, @"<p[^>]*>|</p>|<br\s*/?>", RegexOptions.IgnoreCase);
            foreach (var para in paragraphs)
            {
                var trimmed = para.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    blocks.Add(new ContentBlock { Type = BlockType.Paragraph, Content = trimmed });
                }
            }
        }

        return blocks;
    }

    // Parse inline HTML elements for styling
    private List<TextSegment> ParseInlineElements(string html)
    {
        var segments = new List<TextSegment>();

        if (string.IsNullOrEmpty(html))
        {
            return segments;
        }

        // Pattern to match styled spans and inline elements
        var pattern = @"<(strong|b|em|i|u|s|strike|span)[^>]*(?:style=""([^""]*)""|class=""([^""]*)"")?[^>]*>(.*?)</\1>|([^<]+)|<[^>]+>";
        var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            var tagName = match.Groups[1].Value.ToLower();
            var style = match.Groups[2].Value;
            var className = match.Groups[3].Value;
            var innerContent = match.Groups[4].Value;
            var plainText = match.Groups[5].Value;

            if (!string.IsNullOrEmpty(plainText))
            {
                // Plain text without tags
                var decoded = HttpUtility.HtmlDecode(plainText);
                if (!string.IsNullOrEmpty(decoded))
                {
                    segments.Add(new TextSegment { Text = decoded });
                }
            }
            else if (!string.IsNullOrEmpty(tagName))
            {
                // Parse nested content recursively
                var nestedSegments = ParseInlineElements(innerContent);

                foreach (var seg in nestedSegments)
                {
                    // Apply current tag's styling
                    switch (tagName)
                    {
                        case "strong":
                        case "b":
                            seg.IsBold = true;
                            break;
                        case "em":
                        case "i":
                            seg.IsItalic = true;
                            break;
                        case "u":
                            seg.IsUnderline = true;
                            break;
                        case "s":
                        case "strike":
                            seg.IsStrikethrough = true;
                            break;
                        case "span":
                            // Parse style attribute for colors
                            if (!string.IsNullOrEmpty(style))
                            {
                                var colorMatch = Regex.Match(style, @"color:\s*([^;]+)");
                                if (colorMatch.Success) seg.Color = colorMatch.Groups[1].Value.Trim();

                                var bgMatch = Regex.Match(style, @"background-color:\s*([^;]+)");
                                if (bgMatch.Success) seg.BackgroundColor = bgMatch.Groups[1].Value.Trim();
                            }
                            // Parse Quill-specific classes
                            if (!string.IsNullOrEmpty(className))
                            {
                                if (className.Contains("ql-bg-"))
                                {
                                    var bgClass = Regex.Match(className, @"ql-bg-(\w+)");
                                    if (bgClass.Success) seg.BackgroundColor = bgClass.Groups[1].Value;
                                }
                                if (className.Contains("ql-color-"))
                                {
                                    var colorClass = Regex.Match(className, @"ql-color-(\w+)");
                                    if (colorClass.Success) seg.Color = colorClass.Groups[1].Value;
                                }
                            }
                            break;
                    }

                    segments.Add(seg);
                }

                // If no nested content was found, add empty segment
                if (nestedSegments.Count == 0 && !string.IsNullOrEmpty(innerContent))
                {
                    var decoded = HttpUtility.HtmlDecode(StripAllHtml(innerContent));
                    if (!string.IsNullOrEmpty(decoded))
                    {
                        var seg = new TextSegment { Text = decoded };
                        switch (tagName)
                        {
                            case "strong":
                            case "b":
                                seg.IsBold = true;
                                break;
                            case "em":
                            case "i":
                                seg.IsItalic = true;
                                break;
                            case "u":
                                seg.IsUnderline = true;
                                break;
                            case "s":
                            case "strike":
                                seg.IsStrikethrough = true;
                                break;
                        }
                        segments.Add(seg);
                    }
                }
            }
        }

        // If no segments parsed, return the stripped text
        if (segments.Count == 0)
        {
            var text = HttpUtility.HtmlDecode(StripAllHtml(html));
            if (!string.IsNullOrWhiteSpace(text))
            {
                segments.Add(new TextSegment { Text = text });
            }
        }

        return segments;
    }


    // Parse color string to QuestPDF color
    private QuestPDF.Infrastructure.Color? ParseColor(string colorStr)
    {
        if (string.IsNullOrEmpty(colorStr)) return null;

        colorStr = colorStr.Trim().ToLower();

        // Handle hex colors
        if (colorStr.StartsWith("#"))
        {
            try
            {
                return QuestPDF.Infrastructure.Color.FromHex(colorStr);
            }
            catch { }
        }

        // Handle rgb/rgba
        var rgbMatch = Regex.Match(colorStr, @"rgba?\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)");
        if (rgbMatch.Success)
        {
            var r = byte.Parse(rgbMatch.Groups[1].Value);
            var g = byte.Parse(rgbMatch.Groups[2].Value);
            var b = byte.Parse(rgbMatch.Groups[3].Value);
            return QuestPDF.Infrastructure.Color.FromRGB(r, g, b);
        }

        // Handle named colors
        return colorStr switch
        {
            "red" => QColors.Red.Medium,
            "blue" => QColors.Blue.Medium,
            "green" => QColors.Green.Medium,
            "yellow" => QColors.Yellow.Medium,
            "orange" => QColors.Orange.Medium,
            "purple" => QColors.Purple.Medium,
            "pink" => QColors.Pink.Medium,
            "black" => QColors.Black,
            "white" => QColors.White,
            "gray" or "grey" => QColors.Grey.Medium,
            _ => null
        };
    }

    private string StripAllHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var text = Regex.Replace(html, "<.*?>", string.Empty);
        text = HttpUtility.HtmlDecode(text);
        return text.Trim();
    }

    private void ComposeAnalyticsHeader(QuestPDF.Infrastructure.IContainer container, AnalyticsReportData data)
    {
        container.Column(column =>
        {
            column.Spacing(8);

            column.Item().Text("Journal Analytics Report").FontSize(24).Bold().FontColor(QColors.Grey.Darken4);
            column.Item().Text($"Period: {data.PeriodLabel}").FontSize(12).FontColor(QColors.Grey.Medium);
            column.Item().Text($"Generated: {DateTime.Now:MMMM dd, yyyy}").FontSize(10).FontColor(QColors.Grey.Lighten1);

            column.Item().PaddingTop(8).LineHorizontal(2).LineColor(QColors.Indigo.Medium);
        });
    }

    private void ComposeAnalyticsContent(QuestPDF.Infrastructure.IContainer container, AnalyticsReportData data)
    {
        container.PaddingTop(20).Column(column =>
        {
            column.Spacing(20);

            // Overview Section
            column.Item().Text("Overview").FontSize(16).Bold().FontColor(QColors.Grey.Darken3);
            column.Item().Background(QColors.Grey.Lighten4).Padding(15).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell().Element(CellStyle).Text("Total Entries").FontSize(10).FontColor(QColors.Grey.Medium);
                table.Cell().Element(CellStyle).Text("Current Streak").FontSize(10).FontColor(QColors.Grey.Medium);
                table.Cell().Element(CellStyle).Text("Longest Streak").FontSize(10).FontColor(QColors.Grey.Medium);
                table.Cell().Element(CellStyle).Text("Avg Words/Entry").FontSize(10).FontColor(QColors.Grey.Medium);

                table.Cell().Element(CellStyle).Text(data.TotalEntries.ToString()).FontSize(18).Bold().FontColor(QColors.Indigo.Medium);
                table.Cell().Element(CellStyle).Text($"{data.CurrentStreak} days").FontSize(18).Bold().FontColor(QColors.Indigo.Medium);
                table.Cell().Element(CellStyle).Text($"{data.LongestStreak} days").FontSize(18).Bold().FontColor(QColors.Indigo.Medium);
                table.Cell().Element(CellStyle).Text(data.AverageWordsPerEntry.ToString("F0")).FontSize(18).Bold().FontColor(QColors.Indigo.Medium);
            });

            // Mood Distribution Section
            if (data.MoodCounts != null && data.MoodCounts.Any())
            {
                column.Item().Text("Mood Distribution").FontSize(16).Bold().FontColor(QColors.Grey.Darken3);
                column.Item().Background(QColors.Grey.Lighten4).Padding(15).Column(moodColumn =>
                {
                    moodColumn.Spacing(8);
                    var totalMoods = data.MoodCounts.Values.Sum();
                    foreach (var mood in data.MoodCounts.OrderByDescending(m => m.Value).Take(10))
                    {
                        var percentage = totalMoods > 0 ? (mood.Value * 100.0 / totalMoods) : 0;
                        if (percentage < 0) percentage = 0;
                        if (percentage > 100) percentage = 100;

                        moodColumn.Item().Row(row =>
                        {
                            var moodName = CapitalizeFirst(mood.Key);
                            row.ConstantItem(100).Text(moodName).FontSize(11);

                            row.RelativeItem().Height(16).Background(QColors.Grey.Lighten2).Row(bar =>
                            {
                                if (percentage > 0)
                                    bar.RelativeItem((float)percentage).Background(GetMoodColor(mood.Key));

                                if (percentage < 100)
                                    bar.RelativeItem((float)(100 - percentage));
                            });

                            row.ConstantItem(60).AlignRight().Text($"{mood.Value} ({percentage:F0}%)").FontSize(10);
                        });
                    }
                });
            }

            // Top Tags Section
            if (data.TopTags != null && data.TopTags.Any())
            {
                column.Item().Text("Most Used Tags").FontSize(16).Bold().FontColor(QColors.Grey.Darken3);
                column.Item().Background(QColors.Grey.Lighten4).Padding(15).Column(tagColumn =>
                {
                    tagColumn.Spacing(6);
                    foreach (var tag in data.TopTags.Take(10))
                    {
                        tagColumn.Item().Row(row =>
                        {
                            row.RelativeItem().Text(tag.TagName ?? "Unknown").FontSize(11);
                            row.ConstantItem(80).AlignRight().Text($"{tag.UsageCount} uses").FontSize(10).FontColor(QColors.Grey.Medium);
                        });
                    }
                });
            }

            // Achievements Section
            column.Item().Text("Achievements").FontSize(16).Bold().FontColor(QColors.Grey.Darken3);
            column.Item().Background(QColors.Grey.Lighten4).Padding(15).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Total Words").FontSize(10).FontColor(QColors.Grey.Medium);
                    c.Item().Text(data.TotalWords.ToString("N0")).FontSize(16).Bold().FontColor(QColors.Indigo.Medium);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Days Journaling").FontSize(10).FontColor(QColors.Grey.Medium);
                    c.Item().Text(data.DaysJournaling.ToString()).FontSize(16).Bold().FontColor(QColors.Indigo.Medium);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Unique Tags").FontSize(10).FontColor(QColors.Grey.Medium);
                    c.Item().Text(data.UniqueTags.ToString()).FontSize(16).Bold().FontColor(QColors.Indigo.Medium);
                });
            });
        });
    }

    private QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container)
    {
        return container.Padding(5);
    }

    private void ComposeFooter(QuestPDF.Infrastructure.IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(9).FontColor(QColors.Grey.Medium));
            text.Span("Exported from Personal Journal App | Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
        });
    }

    private string FormatMoodName(string mood)
    {
        if (string.IsNullOrEmpty(mood)) return "";
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(mood.ToLower());
    }

    private string CapitalizeFirst(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return char.ToUpper(text[0]) + text.Substring(1).ToLower();
    }

    private QuestPDF.Infrastructure.Color GetMoodColor(string mood)
    {
        if (string.IsNullOrEmpty(mood)) return QColors.Grey.Lighten1;

        return mood.ToLower() switch
        {
            "happy" or "excited" or "relaxed" or "grateful" or "confident" => QColors.Green.Lighten1,
            "calm" or "thoughtful" or "curious" or "nostalgic" or "bored" => QColors.Blue.Lighten1,
            _ => QColors.Orange.Lighten1
        };
    }

    // Helper classes
    private enum BlockType
    {
        Paragraph,
        Heading1,
        Heading2,
        Heading3,
        OrderedListItem,
        UnorderedListItem,
        Blockquote,
        CodeBlock
    }

    private enum ListType
    {
        None,
        Ordered,
        Unordered
    }

    private class ContentBlock
    {
        public BlockType Type { get; set; }
        public string Content { get; set; } = "";
        public int ListIndex { get; set; }
    }

    private class TextSegment
    {
        public string Text { get; set; } = "";
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsUnderline { get; set; }
        public bool IsStrikethrough { get; set; }
        public string? Color { get; set; }
        public string? BackgroundColor { get; set; }
    }
}

// Data model for analytics export
public class AnalyticsReportData
{
    public string PeriodLabel { get; set; } = "";
    public int TotalEntries { get; set; }
    public int TotalWords { get; set; }
    public double AverageWordsPerEntry { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int DaysJournaling { get; set; }
    public int UniqueTags { get; set; }
    public Dictionary<string, int> MoodCounts { get; set; } = new();
    public List<TagUsageDisplayModel> TopTags { get; set; } = new();
}