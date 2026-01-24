# Personal Journal App

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![MAUI](https://img.shields.io/badge/MAUI-Blazor%20Hybrid-512BD4)](https://dotnet.microsoft.com/apps/maui)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A cross-platform personal journaling application built with **.NET MAUI Blazor Hybrid**, featuring mood tracking, analytics, and secure local storage using SQLite.

---

## Overview

Personal Journal App is a privacy-focused journaling solution that runs natively on **Windows, Android, iOS, and macOS**.
It combines .NET MAUI with Blazor components to deliver a modern writing experience with rich text editing, mood tracking, and analytical insights. All data is stored locally on the device.

---

## Key Features

### Core Functionality

* Rich text editor with Quill.js integration (bold, italic, headers, lists, blockquotes)
* Mood tracking with 15 predefined moods
* Support for 1 primary and up to 2 secondary moods per entry
* Full CRUD operations for journal entries
* Advanced search and filtering by keyword, mood, category, tag, and date range
* Calendar view with mood-based indicators
* Timeline view with pagination (20 entries per page)

### Organization

* Tag system with custom tags and 30+ prebuilt tags
* Hierarchical categories with default groups (Personal, Work, Health, Travel, Goals)
* Entry metadata including title, timestamps, word count, and estimated reading time

### Analytics and Insights

* Dashboard with total entries, word counts, and writing streaks
* Mood distribution analytics and trend insights
* Streak tracking (current and longest)
* Tag usage analytics
* Time-based statistics (monthly, weekly, time of day)
* Period filtering (30, 60, 90, 365 days, or all time)

### Export and Security

* PDF export for single entries, filtered entries, and analytics reports (QuestPDF)
* PIN-based app lock with optional auto-lock on minimize
* Secure local storage using SQLite
* Authentication with ASP.NET Core Identity and PBKDF2 password hashing

### User Experience

* Light, Dark, and System theme modes
* Font size options (Small, Medium, Large)
* Responsive layouts for desktop and mobile
* Real-time word count
* Manual save with last-saved timestamp

---

## Architecture

### Technology Stack

| Component        | Technology            | Purpose                   |
| ---------------- | --------------------- | ------------------------- |
| Framework        | .NET 9.0 MAUI         | Cross-platform native app |
| UI               | Blazor Hybrid         | Component-based UI        |
| Database         | SQLite + EF Core 9.0  | Local persistence         |
| Authentication   | ASP.NET Core Identity | User management           |
| PDF Generation   | QuestPDF 2024.10.2    | PDF exports               |
| Rich Text Editor | Quill.js              | WYSIWYG editing           |
| Language         | C# 12                 | Application logic         |

---

### Project Structure

```text
PersonalJournalApp/
├── Auth/
│   └── CustomAuthStateProvider.cs
├── Common/
│   └── ServiceResult.cs
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   ├── NavMenu.razor
│   │   └── EmptyLayout.razor
│   ├── Pages/
│   │   ├── TodayPage.razor
│   │   ├── CalendarPage.razor
│   │   ├── TimelinePage.razor
│   │   ├── AnalyticsPage.razor
│   │   ├── SettingsPage.razor
│   │   ├── ViewEntryPage.razor
│   │   ├── LoginPage.razor
│   │   └── RegisterPage.razor
│   └── Shared/
│       ├── RichTextEditor.razor
│       └── LockScreen.razor
├── Data/
│   ├── AppDbContext.cs
│   ├── DatabaseInitializer.cs
│   └── TagSeeder.cs
├── Entities/
│   ├── User.cs
│   ├── JournalEntry.cs
│   ├── Tag.cs
│   ├── Category.cs
│   └── UserSettings.cs
├── Models/
│   ├── Input/
│   └── Display/
├── Services/
│   ├── JournalService.cs
│   ├── AuthService.cs
│   ├── AnalyticsService.cs
│   ├── PdfExportService.cs
│   ├── TagService.cs
│   ├── CategoryService.cs
│   ├── SettingsService.cs
│   ├── ThemeService.cs
│   └── AppLockService.cs
├── Platforms/
│   ├── Android/
│   ├── iOS/
│   ├── MacCatalyst/
│   ├── Windows/
│   └── Tizen/
└── wwwroot/
    ├── css/
    └── js/
        └── quillInterop.js
```

---

## Database Schema

### User (ASP.NET Identity)

* Id (PK)
* UserName (unique)
* Email (unique)
* PasswordHash
* PIN (nullable)
* CreatedAt

### JournalEntry

* Id (PK)
* Title
* Content (HTML)
* PrimaryMood
* SecondaryMood1
* SecondaryMood2
* CreatedDate
* ModifiedDate
* UserId (FK)
* CategoryId (FK)

### Tag

* Id (PK)
* Name
* IsPrebuilt
* CreatedAt
* UserId (FK)

### Category

* Id (PK)
* Name
* CreatedAt
* UserId (FK)

### UserSettings

* Id (PK)
* Theme
* FontSize
* BiometricUnlock
* AutoBackup
* CreatedAt
* ModifiedAt
* UserId (FK)

---

## Mood System

### Positive

Happy, Excited, Relaxed, Grateful, Confident

### Neutral

Calm, Thoughtful, Curious, Nostalgic, Bored

### Negative

Sad, Angry, Anxious, Stressed, Tired

**Rules**

* One primary mood is required
* Up to two secondary moods are optional
* Secondary moods must belong to the same category as the primary mood

---

## Getting Started

### Prerequisites

* .NET 9.0 SDK
* Visual Studio 2022 (v17.8+) with MAUI workload
* Android SDK (API 24+)
* Xcode 15+ for iOS/macOS
* Windows 10 version 1809 or later

### Installation

```bash
git clone https://github.com/yourusername/PersonalJournalApp.git
cd PersonalJournalApp
dotnet restore
dotnet build
```

#### Run

```bash
# Windows
dotnet build -t:Run -f net9.0-windows10.0.19041.0

# Android
dotnet build -t:Run -f net9.0-android

# iOS
dotnet build -t:Run -f net9.0-ios

# macOS
dotnet build -t:Run -f net9.0-maccatalyst
```

---

## Usage

### Creating an Entry

1. Open the Today page
2. Select a primary mood
3. Optionally select secondary moods
4. Choose category and tags
5. Write content
6. Save the entry

### Viewing Entries

* Calendar view with mood indicators
* Timeline view with search, filters, and pagination

### Analytics

* Mood distribution
* Writing streaks
* Tag usage
* Time-based trends
* PDF export support

---

## Configuration

### Database Location

* Windows: `%LOCALAPPDATA%\PersonalJournalApp\personaljournalapp.db`
* Android: `/data/data/com.companyname.personaljournalapp/files/`
* iOS/macOS: `~/Library/Application Support/`

---

## Development

### Add a New Feature

1. Create a service in `Services/`
2. Register it in `MauiProgram.cs`
3. Add a Razor page
4. Update navigation
5. Add styles if required

### Migrations

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

---

## Performance

* Startup time under 2 seconds (Windows)
* Entry load under 100 ms for 1,000 entries
* Search under 200 ms
* PDF generation 1–3 seconds per entry

---

## Security

* PBKDF2 password hashing
* Hashed PIN storage
* Local-only data storage
* EF Core parameterized queries
* HTML sanitization for rich text

---

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit changes
4. Push to your branch
5. Open a Pull Request

---

## License

This project is licensed under the MIT License. See the LICENSE file for details.

---

## Acknowledgments

* .NET MAUI
* Blazor
* QuestPDF
* Quill.js
* ASP.NET Core Identity
* Entity Framework Core

---