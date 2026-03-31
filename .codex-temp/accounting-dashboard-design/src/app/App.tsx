import { useState } from "react";
import { Sidebar } from "./components/Sidebar";
import { Header } from "./components/Header";
import { KPICards } from "./components/KPICards";
import { QuickActions } from "./components/QuickActions";
import { Charts } from "./components/Charts";
import { TransactionsTable } from "./components/TransactionsTable";
import { AccountsTab } from "./components/AccountsTab";
import { InventoryTab } from "./components/InventoryTab";
import { InvoicesTab } from "./components/InvoicesTab";
import { VendorsTab } from "./components/VendorsTab";
import { VouchersTab } from "./components/VouchersTab";
import { JournalTab } from "./components/JournalTab";

const pageConfig: Record<string, { title: string; subtitle: string }> = {
  Dashboard: { title: "Dashboard",          subtitle: "Welcome back, Ahmed" },
  Invoices:  { title: "Invoices",            subtitle: "Manage your sales & purchase invoices" },
  Customers: { title: "Customers",           subtitle: "View and manage your customer accounts" },
  Vendors:   { title: "Vendors",             subtitle: "Manage supplier relationships" },
  Inventory: { title: "Inventory",           subtitle: "Track stock levels and movements" },
  Accounts:  { title: "Chart of Accounts",   subtitle: "Manage your general ledger structure" },
  Vouchers:  { title: "Vouchers — السندات",  subtitle: "Receipt & payment vouchers with accounting entry preview" },
  Journal:   { title: "Journal Entries — القيود", subtitle: "General ledger journal entries with debit/credit lines & balance validation" },
  Reports:   { title: "Reports",             subtitle: "Financial reports and analytics" },
  Settings:  { title: "Settings",            subtitle: "Configure your accounting suite" },
};

function PlaceholderPage({ name }: { name: string }) {
  return (
    <div
      style={{
        flex: 1,
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        gap: "12px",
        color: "#94A3B8",
        fontFamily: "'Inter', sans-serif",
      }}
    >
      <div
        style={{
          width: 56,
          height: 56,
          borderRadius: 16,
          backgroundColor: "#EFF6FF",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          fontSize: "24px",
        }}
      >
        🚧
      </div>
      <div style={{ color: "#1E3A5F", fontSize: "15px", fontWeight: 700 }}>{name}</div>
      <div style={{ fontSize: "13px" }}>This section is under construction</div>
    </div>
  );
}

export default function App() {
  const [activePage, setActivePage] = useState<string>("Dashboard");
  const cfg = pageConfig[activePage] ?? pageConfig.Dashboard;

  return (
    <div
      style={{
        width: "1440px",
        height: "900px",
        minWidth: "1440px",
        minHeight: "900px",
        display: "flex",
        flexDirection: "row",
        overflow: "hidden",
        fontFamily: "'Inter', sans-serif",
        backgroundColor: "#F5F7FA",
      }}
    >
      {/* Main Content Area (left side) */}
      <div style={{ flex: 1, display: "flex", flexDirection: "column", overflow: "hidden", minWidth: 0 }}>
        {/* Header */}
        <Header title={cfg.title} subtitle={cfg.subtitle} />

        {/* Scrollable Content */}
        <div
          style={{
            flex: 1,
            overflowY: "auto",
            padding: "20px 24px",
            display: "flex",
            flexDirection: "column",
            gap: "16px",
            backgroundColor: "#F5F7FA",
          }}
        >
          {activePage === "Dashboard" && (
            <>
              <section><KPICards /></section>
              <section><QuickActions /></section>
              <section><Charts /></section>
              <section style={{ paddingBottom: "8px" }}><TransactionsTable /></section>
            </>
          )}

          {activePage === "Accounts" && (
            <section style={{ paddingBottom: "8px" }}>
              <AccountsTab />
            </section>
          )}

          {activePage === "Inventory" && (
            <section style={{ paddingBottom: "8px" }}>
              <InventoryTab />
            </section>
          )}

          {activePage === "Invoices" && (
            <section style={{ paddingBottom: "8px" }}>
              <InvoicesTab />
            </section>
          )}

          {activePage === "Vendors" && (
            <section style={{ paddingBottom: "8px" }}>
              <VendorsTab />
            </section>
          )}

          {activePage === "Vouchers" && (
            <section style={{ paddingBottom: "8px" }}>
              <VouchersTab />
            </section>
          )}

          {activePage === "Journal" && (
            <section style={{ paddingBottom: "8px" }}>
              <JournalTab />
            </section>
          )}

          {!["Dashboard", "Accounts", "Inventory", "Invoices", "Vendors", "Vouchers", "Journal"].includes(activePage) && (
            <PlaceholderPage name={activePage} />
          )}
        </div>
      </div>

      {/* Right Sidebar */}
      <Sidebar activePage={activePage} onNavigate={setActivePage} />
    </div>
  );
}