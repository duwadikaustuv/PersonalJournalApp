using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using PersonalJournalApp.Models.Display;
using System.Text.RegularExpressions;

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
            // Try Documents folder first
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // If Documents path is empty or doesn't exist, use fallback options
            if (string.IsNullOrEmpty(documentsPath) || !Directory.Exists(documentsPath))
            {
                // Try user profile folder on Windows
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

            // Final fallback to app data directory
            if (string.IsNullOrEmpty(documentsPath) || !Directory.Exists(documentsPath))
            {
                documentsPath = FileSystem.AppDataDirectory;
            }

            var filePath = Path.Combine(documentsPath, defaultFileName);

            // Ensure the directory exists
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
            // Ultimate fallback to app cache directory
            var cachePath = Path.Combine(FileSystem.CacheDirectory, defaultFileName);
            return Task.FromResult<string?>(cachePath);
        }
    }

    private void ComposeEntryHeader(QuestPDF.Infrastructure.IContainer container, JournalEntryDisplayModel entry)
    {
        container.Column(column =>
        {
            column.Spacing(8);

            // Title
            var title = string.IsNullOrEmpty(entry.Title) ? "Untitled Entry" : entry.Title;
            column.Item().Text(title).FontSize(24).Bold().FontColor(QColors.Grey.Darken4);

            // Date and time
            column.Item().Text($"{entry.CreatedDate.ToLocalTime():dddd, MMMM d, yyyy} at {entry.FormattedTime}")
                .FontSize(12).FontColor(QColors.Grey.Medium);

            // Divider line
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

                // Mood row
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

            // Content
            column.Item().PaddingTop(10).Text(StripHtml(entry.Content))
                .FontSize(12).LineHeight(1.6f).FontColor(QColors.Grey.Darken3);
        });
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

                            // Safer bar chart construction using weighted relative items
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

    private string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var text = Regex.Replace(html, "<.*?>", string.Empty);
        text = System.Net.WebUtility.HtmlDecode(text);
        return text.Trim();
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