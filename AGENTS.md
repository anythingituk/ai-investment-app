# AGENTS.md - Private AI Investment Research App

## Purpose

This repository contains a private Windows investment research application for personal use. The app is not intended to place trades automatically, provide financial advice to third parties, or operate as a public investment service.

The application should help the user scan markets, shortlist lower-priced shares, perform AI-assisted research on selected companies, monitor watchlists, and present dashboard suggestions for human review.

The human user remains the final decision-maker.

## Product Summary

Build a private Windows installable app that runs from the system tray and opens a local browser dashboard at `localhost`.

The app should:

- Run quietly in the Windows system tray.
- Provide a tray menu with commands such as `Open Dashboard`, `Run Scan Now`, `Pause AI`, `Resume AI`, `Settings`, and `Exit`.
- Open a local web dashboard, preferably bound to `127.0.0.1` only.
- Maintain a local database of companies, watchlists, research reports, alerts, portfolio/cash data, and settings.
- Use AI APIs to analyse selected companies and summarise opportunities.
- Use structured market/financial data where possible; AI should interpret data, not invent data.
- Support broad scans, human shortlisting, deep research, scheduled monitoring, and dashboard suggestions.
- Include cost controls for AI/API usage.
- Include warning labels and risk controls for shorting or other high-risk ideas.

## Core Workflow

```text
Broad market scan
    -> rules-based filters
    -> AI shortlist
    -> human selection / override
    -> AI deep research on selected companies
    -> scheduled monitoring
    -> dashboard suggestions
    -> human decision
```

The app should never automatically buy, sell, short, or place orders.

## Target Investment Universe

Initial focus:

- Shares priced approximately between GBP 1 and GBP 10, or equivalent in other currencies.
- UK and US markets initially.
- Filters to be added for FTSE 100, FTSE 250, AIM, Dow Jones, S&P 500, Nasdaq, and later custom baskets.
- Sector/industry dashboards should help show broader market strength/weakness.

Important principle:

> A low share price does not mean the stock is cheap. The app must evaluate liquidity, spread, valuation, debt, profitability, dilution risk, news, catalysts, and technical setup.

## Recommended Technology Direction

Preferred stack:

```text
.NET 8 or later
Windows tray app
ASP.NET Core local web server
React, Blazor, or similar local dashboard UI
SQLite database
Quartz.NET, Hangfire, or equivalent background job scheduler
OpenAI API or compatible AI provider abstraction
Market data provider abstraction
```

Alternative stack:

```text
Tauri tray app
React dashboard
Rust or .NET/Node backend
SQLite database
```

For a Windows-first private tool, prefer the .NET stack unless there is a strong reason otherwise.

## Localhost Dashboard

The `Open Dashboard` tray command should open a local browser URL, for example:

```text
http://127.0.0.1:48720
```

Security requirements:

- Bind the local dashboard to `127.0.0.1` by default, not `0.0.0.0`.
- Store API keys encrypted where practical.
- Do not expose the dashboard to the LAN or internet by default.
- Add a local access token if the dashboard/API could be reached by another process.
- Shut down the local server cleanly when the app exits.
- Log actions but avoid logging full secret values.

## Windows App Updates

The Windows app should support update awareness from GitHub releases.

MVP update behaviour:

- Add an `Check for Updates` command in settings or the tray menu.
- Automatically check GitHub releases periodically, for example once per day.
- Compare the installed app version with the latest GitHub release version.
- Notify the user when an update is available.
- Open the GitHub release page or installer download for manual installation.
- Do not silently install updates in the MVP.

Later update behaviour:

- Support signed installers.
- Verify installer checksums or signatures before running an update.
- Offer one-click update install after user confirmation.
- Show release notes before updating.
- Allow update checks to be disabled.

Update checks should be cheap, rate-limit friendly, and should not require a GitHub token for public release metadata where possible.

## Multi-Computer Data Strategy

The app may need to run on up to three private computers in different locations.

MVP data behaviour:

- Use local SQLite first for speed, privacy, offline access, and simpler development.
- Keep the data access layer provider-based so a cloud database or sync layer can be added later.
- Store machine-specific settings separately from shared investment research data where practical.
- Avoid putting API keys or local secrets into shared cloud tables.
- Prefer record-level sync between local SQLite and cloud storage, not raw SQLite file syncing.
- Support explicit sync triggers on app startup, manual app launch, and tray exit once cloud sync is implemented.

Viable low-cost options for multi-computer use:

| Option | Role | Notes |
|---|---|---|
| Local SQLite with manual backup/export | Simplest MVP | Cheapest and safest to build first, but no automatic cross-device sync. |
| SQLite plus cloud file sync | Simple sync | Possible with OneDrive/Dropbox, but risky if multiple devices write at the same time. Better for backup than live sync. |
| Supabase Postgres | Cloud database | Low-cost managed Postgres with authentication, APIs, backups, and a generous free/low-cost tier. Good candidate for later shared data. |
| Neon Postgres | Cloud database | Low-cost serverless Postgres. Good for structured data, but app must handle cold starts and connection management. |
| Azure SQL Database | Microsoft cloud database | Strong Windows/Microsoft fit, but usually more expensive and heavier than needed for a private MVP. |
| LiteFS/Turso/libSQL | SQLite-style cloud sync | Attractive if keeping SQLite semantics matters, but check Windows/.NET support carefully before committing. |
| Self-hosted Postgres on a VPS | Cheapest long-term control | More admin work: backups, security patches, firewalling, TLS, monitoring. |

Recommended direction:

- Build Phase 1-3 on local SQLite.
- Add backup/export early.
- Design repositories/services so storage can later move to Postgres without rewriting dashboard or AI logic.
- If cross-device sync becomes necessary, prefer Supabase Postgres or Neon Postgres before attempting cloud-file SQLite sync.
- If self-hosting is preferred, PostgreSQL on an existing Linode server is a strong low-cost option, provided backups, firewalling, TLS/VPN access, and patching are handled properly.

Sync behaviour, once enabled:

- Sync on application startup, including Windows startup tray launch.
- Sync when the app is manually opened.
- Sync periodically in the background using a configurable interval.
- Sync after meaningful task completion when local data has changed, such as after AI research, broad scans, alert generation, settings edits, watchlist edits, or portfolio/cash edits.
- Attempt a final sync when the user exits from the system tray.
- Show clear sync status: idle, syncing, success, warning, error, offline.
- Do not block app shutdown indefinitely if final sync fails; log the failure and show a concise warning if appropriate.
- Keep sync pause/control separate from `Pause AI`. Pausing AI should stop market/AI/background research activity, but should not automatically prevent data sync unless the user explicitly pauses sync too.
- Use per-device IDs and UTC timestamps for sync conflict handling.
- Prefer append-only or versioned records for AI runs, job logs, usage logs, market snapshots, and research reports.
- Do not sync after every individual row insert; mark changed records as dirty and batch them.
- Use incremental sync based on dirty flags, row versions, or `UpdatedAtUtc > LastSyncedAtUtc`.

Recommended default sync timing:

| Trigger | Timing |
|---|---|
| App startup | Immediately. |
| Manual user edits | Debounced by 30-60 seconds. |
| AI/deep research completion | Immediately after the task completes. |
| Broad scan completion | Immediately after the scan completes. |
| Alert generation | Within 1-2 minutes. |
| Market price/candle updates | Batched every 15-30 minutes. |
| API usage and job logs | Batched every 10-15 minutes. |
| Normal periodic sync | Every 15 minutes by default. |
| Tray exit | Final sync attempt, capped at 10-20 seconds. |
| Sync failure or offline mode | Retry with exponential backoff, for example 1 minute, 5 minutes, 15 minutes, then 1 hour. |

Initial defaults:

```text
Sync interval: 15 minutes
Debounce delay: 45 seconds
Exit sync timeout: 15 seconds
Failure retry: 1 min, 5 min, 15 min, 1 hour
```

## Tray Menu MVP

Implement this first:

```text
Open Dashboard
Run Scan Now
Pause AI
Resume AI
Settings
Exit
```

Later additions:

```text
Check for Updates
Run Quick Scan
Run Deep Research
Update Prices Now
View Latest Alerts
Pause for 1 Hour
Pause Until Tomorrow
Open Logs
Export Report
```

## Pause / Resume Behaviour

`Pause AI` should stop external automated activity, including:

- Scheduled AI research.
- Broad market scans.
- News scans.
- Price/data refreshes if configured as part of active monitoring.
- AI-generated dashboard suggestion refresh.
- Notifications generated by background jobs.

Paused mode should still allow:

- Opening the dashboard.
- Viewing stored data.
- Editing settings.
- Reviewing previous research.
- Manual actions if explicitly triggered by the user.

The UI should clearly show whether AI/background activity is active or paused.

## Dashboard MVP

The first dashboard should include:

- AI status card: Active, Paused, Running, Error.
- Portfolio/cash summary placeholder.
- Top ranked long opportunities.
- Top ranked short-watch opportunities with warning labels.
- GBP 1-10 share candidates table.
- Research queue.
- Sector/index summary.
- Recent alerts.
- AI usage/cost summary.
- Settings page.

## Main Dashboard Sections

### Dashboard

- Portfolio value.
- Cash available.
- Today's P/L, if data is available.
- AI activity status.
- Last scan time.
- Alerts requiring human review.
- Top suggestions.

### Opportunities

Ranked long and short-watch candidates with filters:

- Market.
- Index.
- Sector.
- Price range.
- Share price GBP 1-10.
- AI score.
- Risk level.
- Long/short status.
- Liquidity.
- Volume.
- Dividend.
- Momentum.

### Research Queue

Statuses:

```text
Pending
Running
Complete
Needs Human Review
Rejected
Monitoring
Paused
```

### Company Research Page

For each company, include:

- Company overview.
- Price chart.
- Candlestick/technical section.
- Fundamentals.
- News/catalysts.
- Bull case.
- Bear case.
- Short case, where applicable.
- Risk flags.
- AI score.
- Human notes.
- Decision history.

### Sectors & Indexes

Track broad market context:

- FTSE 100.
- FTSE 250.
- AIM.
- Dow Jones.
- S&P 500.
- Nasdaq.
- Sector strength/weakness.
- Top movers.
- Worst movers.

### Shorting

Shorting is an advanced high-risk feature. It should be visibly separated from ordinary long-share ideas.

Required fields for short-watch ideas:

- Short thesis.
- Entry price.
- Target cover price.
- Stop-loss or max loss level.
- Time horizon.
- Borrow/financing cost, where available.
- Short interest/squeeze risk, where available.
- Upcoming earnings date.
- Maximum position size.
- Warning label.

Do not allow a short candidate to appear as actionable without a warning and a defined risk/exit rule.

## AI Agents / Services

Implement as services or jobs, not necessarily separate autonomous agents at first.

Planned agents/services:

| Agent | Role |
|---|---|
| Universe Scanner | Scans markets using structured filters. |
| Low-Price Share Agent | Focuses on GBP 1-10 share candidates. |
| Fundamentals Agent | Reviews revenue, earnings, debt, margins, cash flow, dilution. |
| News Agent | Checks company and sector news. |
| Catalyst Agent | Finds earnings, dividends, contract wins, regulation, M&A, sector events. |
| Technical Agent | Reviews trend, moving averages, RSI, volume, candlesticks. |
| Shorting Agent | Identifies bearish setups and squeeze risk. |
| Risk Agent | Checks position size, exposure, drawdown, cost drag. |
| Ranking Agent | Combines scores into ranked suggestions. |
| Human Notes Agent | Applies the user's notes, preferences, overrides, and previous decisions. |

## AI Design Principles

- AI should interpret and summarise structured data.
- AI should not be treated as the source of truth for prices, fundamentals, or corporate events.
- Store the input data snapshot used for each AI analysis where practical.
- Store AI output, model name, timestamp, estimated token usage, and estimated cost.
- Every AI suggestion should include reasons and warnings.
- The app should avoid language such as `Buy this now`.
- Prefer language such as `Candidate meets long-entry criteria; review for possible entry`.
- Human approval is always required.

## AI Usage Cost Controls

Include settings for:

```text
Monthly AI budget
Daily soft limit
Daily hard limit
Maximum deep research companies per day
Maximum broad scan AI summaries per day
Model for broad scans
Model for deep research
Automatic pause when budget reaches threshold
```

Dashboard should show:

```text
AI spend today
AI spend this month
Budget remaining
Estimated monthly run rate
Current AI mode: Lean / Balanced / Aggressive
```

Recommended initial behaviour:

- Use code and structured data for broad filtering.
- Use cheaper/faster models for shortlist summaries.
- Use stronger models only for selected deep research.
- Do not run hourly deep research across hundreds of companies.
- Use hourly checks only for price, news headlines, volume spikes, and trigger detection.

## Suggested Monitoring Schedule

MVP:

```text
Broad market scan: daily
Selected company news check: every 2-4 hours during market days
Price update: hourly or daily depending on data provider and settings
Deep AI research: daily for priority selected companies
Technical/candlestick scan: daily after market close
Full research refresh: weekly
```

The scheduler must respect paused mode and budget limits.

## Opportunity Scoring

### Long Candidate Score

| Factor | Weight |
|---|---:|
| Financial strength | 15 |
| Valuation upside | 15 |
| Catalyst strength | 15 |
| Technical setup | 15 |
| Sector momentum | 10 |
| Liquidity/spread quality | 10 |
| News sentiment | 10 |
| Portfolio fit | 5 |
| Human preference | 5 |

### Short Candidate Score

| Factor | Weight |
|---|---:|
| Weak financials | 15 |
| Overvaluation | 15 |
| Negative catalyst | 15 |
| Technical breakdown | 15 |
| Sector weakness | 10 |
| Liquidity | 10 |
| Squeeze risk | -15 penalty |
| Borrow/financing risk | -10 penalty |
| Human override | 5 |

Show score breakdowns in the dashboard.

## Risk and Investment Assumptions

The app is intended for aggressive research, but not reckless automation.

Working assumptions:

```text
Starting investment capital: GBP 10,000
Primary target: aggressive capital growth
Quarterly campaign target: approximately 7-12%, not guaranteed
Annual stretch target: 20-50%, not guaranteed
Minimum candidate upside: 15%+
Preferred candidate upside: 25%+
Maximum planned loss per position: 5-8%
Maximum total drawdown before pause/review: 15%
Maximum single position: configurable, initially GBP 1,000-1,500
Minimum cash reserve: configurable, initially GBP 1,000
```

These are planning assumptions only. The app should show warnings and should not imply guaranteed returns.

## Portfolio, Cash, and Costs

The app should eventually track:

- Holdings.
- Transactions.
- Cash deposits.
- Cash withdrawals.
- Transfer fees.
- FX fees.
- Dealing fees.
- Stamp duty.
- Spreads.
- Platform fees.
- Dividend income.
- Realised and unrealised P/L.

Cost-aware analysis should answer:

> Is the expected upside large enough after estimated transaction costs and spread?

## Data Model - Initial Tables

Start with these tables or equivalent entities:

```text
Settings
Companies
Watchlists
CompanyStatuses
HumanNotes
MarketPrices
PriceCandles
MarketDataSnapshots
ResearchQueue
AIAnalysisRuns
AICompanyAnalysis
OpportunityScores
Alerts
Sectors
Indexes
ApiUsageLog
JobRuns
```

Later add:

```text
PortfolioAccounts
CashMovements
Transactions
TradeCosts
BrokerFeeProfiles
StockThresholds
FundamentalThresholds
TechnicalSignals
ShortCandidates
RiskRules
```

## Build Phases

### Phase 1 - Core App Shell

Build:

- Windows installable app.
- System tray icon.
- Tray menu.
- Localhost dashboard.
- Basic settings.
- SQLite database.
- Pause/resume state.

Success test:

```text
Can the app install, run from the tray, open the dashboard, pause/resume activity, and exit cleanly?
```

### Phase 2 - Watchlist and Company Universe

Build:

- Company list.
- Watchlist.
- Manual add/edit/remove.
- Market/index/sector fields.
- GBP 1-10 price filter field.
- Company statuses.
- Human notes.

Success test:

```text
Can the user add companies, categorise them, filter them, and create a research list?
```

### Phase 3 - Market Data and Dashboard

Build:

- Market data provider abstraction.
- Latest price.
- Daily change.
- Volume.
- Market cap.
- Sector/index data.
- Basic dashboard tables/cards.
- Low-price candidates table.

Success test:

```text
Can the app update market data and show useful company/index/sector context?
```

### Phase 4 - AI Research MVP

Build:

- AI provider abstraction.
- Run AI analysis on selected companies.
- Research dossier.
- Bull case.
- Bear case.
- Risk flags.
- Catalysts.
- AI score.
- Cost logging.

Success test:

```text
Can the user select 5-10 companies and get useful AI research reports without overspending?
```

### Phase 5 - Broad Scan and Ranking

Build:

- Broad scan rules.
- GBP 1-10 filter.
- Liquidity and spread warnings.
- Volume/market cap filters.
- Basic valuation/momentum filters.
- AI shortlist generation.
- Ranking table.
- Human review queue.

Success test:

```text
Can the app scan a broader universe, reduce it to a shortlist, and present candidates worth reviewing?
```

### Phase 6 - Monitoring and Alerts

Build:

- Scheduled jobs.
- Price threshold alerts.
- News/catalyst alerts.
- Volume spike alerts.
- AI refresh schedule.
- Tray notifications.
- Job logs.
- Error handling.
- Budget limits.

Success test:

```text
Can the app monitor selected companies in the background and alert only when meaningful changes occur?
```

### Phase 6a - Updates and Multi-Computer Sync

Build:

- GitHub release update checker.
- Installed version tracking.
- User-visible update notification.
- Manual update download/open action.
- Backup/export workflow.
- Storage abstraction review for future cloud database support.
- Optional cloud database proof of concept if multi-computer use becomes a current requirement.

Success test:

```text
Can the app tell the user when a newer GitHub release is available, and is the data layer ready for a future shared database without a major rewrite?
```

### Phase 7 - Advanced Investment Logic

Build:

- Portfolio tracker.
- Cash tracking.
- Deposits/withdrawals.
- Transaction costs.
- Position sizing.
- Risk/reward calculator.
- Staggered buy/sell thresholds.
- Candlestick and technical indicators.
- Short-watch module.
- Performance reports.
- Backup/export.

Success test:

```text
Can the app help the user evaluate opportunities, risks, sizing, timing, and review triggers?
```

## Coding Standards

- Keep modules cleanly separated: tray, web server, jobs, data, AI, market data, dashboard.
- Use provider abstractions for AI and market data.
- Avoid hard-coding API providers.
- All background jobs should be cancellable and respect paused mode.
- Use async I/O for API calls.
- Store timestamps in UTC; display in local time.
- Use clear error handling and retry policies.
- Never commit real API keys.
- Add configuration examples using placeholder values.
- Prefer deterministic rule calculations before invoking AI.
- Write tests for scoring, filters, budget limits, pause/resume behaviour, and cost calculations.

## Non-Goals for Early Versions

Do not build these in the MVP:

- Live trading.
- Broker order placement.
- Automated buying/selling.
- Fully autonomous investment decisions.
- Public multi-user accounts.
- Mobile app.
- Tax filing/reporting.
- Hourly deep research across hundreds of companies.

## Definition of Done for MVP

The MVP is complete when:

- The Windows app installs and runs from the system tray.
- `Open Dashboard` opens the local dashboard.
- The user can add companies and statuses.
- The user can filter lower-priced shares.
- The app can fetch or accept basic market data.
- The app can run AI research on selected companies.
- The dashboard shows ranked suggestions and warnings.
- The user can pause/resume AI activity.
- API usage/cost is logged and visible.
- No automated trading occurs.

## Notes for Codex

Codex should treat this file as the source of project direction. When implementing, prefer small vertical slices over large speculative rewrites. Before adding advanced investment logic, first ensure the tray app, local server, dashboard, database, and pause/resume controls are reliable.

When unsure, implement the simpler private-MVP version first and leave extension points for later phases.
