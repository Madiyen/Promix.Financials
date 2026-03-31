# Al-Ameen Accounting Map

## Purpose
This note captures the accounting model and UX patterns observed from the installed `Al-Ameen` system so we can use them deliberately while evolving `Promix`.

## Evidence Sources
- Installed program binaries and SQL scripts under `C:\Program Files (x86)\alameensoft\alameen ERP\Bin`
- Live company database `AmnDb002` on `MDIYEN-LAPTOP\MSSQLSERVER2025`
- Backup file:
  - `C:\Users\Public\Documents\Al-Ameen\10.0\Backup\AmnDb002\MDIYEN-LAPTOP_MSSQLSERVER2025_AmnDb002_260328_1428390936__10_0_879_5055.dat`
- UI screenshots from:
  - company creation wizard
  - SQL Server connection dialog
  - main shell and invoices area

## Core Accounting Entities In Al-Ameen
- `ac000`: chart of accounts / posting accounts
- `ce000`: journal entry headers
- `en000`: journal entry lines
- `py000`: vouchers
- `et000`: voucher and entry type definitions
- `my000`: currencies
- `br000`: branches
- `MX000`: cost centers / analysis centers
- `cu000`: customers
- `co000`: vendors / suppliers
- `ch000`: cheques and cheque lifecycle
- `us000`: users and administrative flags

## Observed Accounting Structure
- The system uses a clear separation between source document, voucher, and journal entry.
- `vwER` links accounting entries to their origin document.
- `vwEntryItems` shows journal lines carrying:
  - account
  - debit / credit
  - currency
  - cost center
  - contra account
  - customer
  - line notes
- `VoucherPrinting.sql` shows vouchers are treated as printable documents with explicit type, number, date, currency, and print state.

## Observed Voucher Types
From `et000` in the new company:
- Opening Entry
- Receipt Voucher
- Payment Voucher
- Journal Voucher

Observed type-level capabilities:
- auto-post flag
- detailed vs. non-detailed entry behavior
- fixed account / fixed currency options
- forced cost center options
- tax-related account slots
- customer-name field slots
- cheque-type controls

This means Al-Ameen does not treat voucher types as just labels. Each type carries behavior metadata.

## Observed Posting Patterns
From the live company after sample transactions:
- receipt voucher posts cash against customer or counterparty
- payment voucher posts expense or liability against cash / bank
- invoice activity can generate entries without being manually entered as a voucher
- notes receivable (`أوراق القبض`) appear as first-class accounting effects
- purchases and sales move through dedicated operational accounts, not only free-form descriptions

## UX Patterns Learned From Al-Ameen
- Startup is wizard-driven, not a single flat dialog
- company creation exposes:
  - template / preset
  - accounting start date
  - base currency
  - storage / SQL server notes
  - summary before creation
- SQL connection is explicit and recoverable
- shell is document-centric and operational, with modules and strong grouping
- backups are first-class and discoverable
- logging / audit options are visible during file creation

## Gap Analysis Against Promix
### Already Present In Promix
- chart of accounts
- base currency model
- receipt / payment / transfer / journal / opening / cash-closing dialogs
- draft vs. posted status
- trial balance
- account statement
- logical delete and admin-only edit / delete for vouchers
- period lock

### Missing Or Still Weaker Than Al-Ameen
- company setup wizard depth
- persisted accounting start date
- branch dimension
- cost center dimension
- cheque lifecycle
- separate customer / vendor masters as first-class accounting entities
- configurable voucher-type behavior
- source-document to journal linkage
- print / archive / audit state surfaced in the UI
- richer operational document flows around invoices and settlement

## Improvements Applied In Promix During This Round
- Added `AccountingStartDate` to `Company`
- Extended company creation flow to collect and persist accounting start date
- Upgraded the new-company dialog so it feels more like a setup step rather than a plain form
- Surfaced richer voucher metadata in the journal center:
  - currency and entered amount
  - exchange rate
  - posted time
  - modified time
  - audit-oriented summary text
- Surfaced accounting start date in company selection cards

## Recommended Next Phases
1. Add branch support to journal entries and core master data.
2. Add cost-center support to journal lines and reports.
3. Introduce customer / vendor masters distinct from raw posting accounts.
4. Add cheque receipt / payment flows.
5. Promote voucher types from enum-only behavior to configurable templates.
6. Link future invoices and settlements to their generated journal entries.
7. Add backup / restore visibility and an audit timeline in the UI.

## Current Direction For Promix
Promix should remain lighter than Al-Ameen, but it should absorb the accounting ideas that materially improve control and clarity:
- setup clarity
- explicit accounting dates
- richer voucher metadata
- stronger source-to-entry traceability
- operational dimensions only when they affect accounting correctness
