import React, { useState, useMemo } from "react";
import {
  PieChart,
  Pie,
  Tooltip,
} from "recharts";
import {
  Search,
  Plus,
  Download,
  ChevronDown,
  ChevronRight,
  Eye,
  Pencil,
  MoreHorizontal,
  ArrowUpRight,
  ArrowDownRight,
  Wallet,
  TrendingUp,
  TrendingDown,
  Building2,
  Filter,
  RefreshCw,
  BookOpen,
} from "lucide-react";

// ─── Types ───────────────────────────────────────────────────────────────────

type AccountType = "Assets" | "Liabilities" | "Equity" | "Revenue" | "Expenses";
type DrCr = "Dr" | "Cr";
type AccountStatus = "active" | "inactive";

interface Account {
  code: string;
  name: string;
  type: AccountType;
  category: string;
  balance: number;
  normalBalance: DrCr;
  status: AccountStatus;
  lastActivity: string;
  level: number; // 0 = group, 1 = sub-group, 2 = leaf
}

// ─── Data ────────────────────────────────────────────────────────────────────

const accounts: Account[] = [
  // ASSETS — Current
  { code: "1001", name: "Cash on Hand",              type: "Assets",      category: "Current Assets",       balance: 28450,    normalBalance: "Dr", status: "active",   lastActivity: "Feb 26, 2026", level: 2 },
  { code: "1002", name: "Petty Cash",                type: "Assets",      category: "Current Assets",       balance: 1500,     normalBalance: "Dr", status: "active",   lastActivity: "Feb 20, 2026", level: 2 },
  { code: "1100", name: "Bank Account — Main",       type: "Assets",      category: "Current Assets",       balance: 94280,    normalBalance: "Dr", status: "active",   lastActivity: "Feb 26, 2026", level: 2 },
  { code: "1101", name: "Bank Account — Savings",    type: "Assets",      category: "Current Assets",       balance: 32510,    normalBalance: "Dr", status: "active",   lastActivity: "Feb 25, 2026", level: 2 },
  { code: "1200", name: "Accounts Receivable",       type: "Assets",      category: "Current Assets",       balance: 87340,    normalBalance: "Dr", status: "active",   lastActivity: "Feb 26, 2026", level: 2 },
  { code: "1210", name: "Allowance for Bad Debts",   type: "Assets",      category: "Current Assets",       balance: -4200,    normalBalance: "Cr", status: "active",   lastActivity: "Feb 10, 2026", level: 2 },
  { code: "1300", name: "Inventory",                 type: "Assets",      category: "Current Assets",       balance: 62840,    normalBalance: "Dr", status: "active",   lastActivity: "Feb 24, 2026", level: 2 },
  { code: "1400", name: "Prepaid Expenses",          type: "Assets",      category: "Current Assets",       balance: 8600,     normalBalance: "Dr", status: "active",   lastActivity: "Feb 01, 2026", level: 2 },
  // ASSETS — Fixed
  { code: "1500", name: "Equipment",                 type: "Assets",      category: "Fixed Assets",         balance: 145000,   normalBalance: "Dr", status: "active",   lastActivity: "Jan 15, 2026", level: 2 },
  { code: "1501", name: "Accumulated Dep. — Equip.", type: "Assets",      category: "Fixed Assets",         balance: -65780,   normalBalance: "Cr", status: "active",   lastActivity: "Feb 28, 2026", level: 2 },
  { code: "1600", name: "Office Furniture",          type: "Assets",      category: "Fixed Assets",         balance: 28500,    normalBalance: "Dr", status: "active",   lastActivity: "Dec 01, 2025", level: 2 },
  { code: "1601", name: "Accumulated Dep. — Furn.",  type: "Assets",      category: "Fixed Assets",         balance: -8640,    normalBalance: "Cr", status: "active",   lastActivity: "Feb 28, 2026", level: 2 },
  { code: "1700", name: "Intangible Assets",         type: "Assets",      category: "Fixed Assets",         balance: 18000,    normalBalance: "Dr", status: "inactive", lastActivity: "Oct 01, 2025", level: 2 },
  // LIABILITIES — Current
  { code: "2001", name: "Accounts Payable",          type: "Liabilities", category: "Current Liabilities",  balance: 31920,    normalBalance: "Cr", status: "active",   lastActivity: "Feb 25, 2026", level: 2 },
  { code: "2100", name: "Accrued Expenses",          type: "Liabilities", category: "Current Liabilities",  balance: 8450,     normalBalance: "Cr", status: "active",   lastActivity: "Feb 20, 2026", level: 2 },
  { code: "2200", name: "Sales Tax Payable",         type: "Liabilities", category: "Current Liabilities",  balance: 4620,     normalBalance: "Cr", status: "active",   lastActivity: "Feb 26, 2026", level: 2 },
  { code: "2300", name: "Short-term Loan",           type: "Liabilities", category: "Current Liabilities",  balance: 15000,    normalBalance: "Cr", status: "active",   lastActivity: "Feb 01, 2026", level: 2 },
  // LIABILITIES — Long-term
  { code: "2500", name: "Bank Loan — Long Term",     type: "Liabilities", category: "Long-term Liabilities",balance: 34290,    normalBalance: "Cr", status: "active",   lastActivity: "Jan 31, 2026", level: 2 },
  { code: "2600", name: "Deferred Revenue",          type: "Liabilities", category: "Long-term Liabilities",balance: 6200,     normalBalance: "Cr", status: "inactive", lastActivity: "Nov 15, 2025", level: 2 },
  // EQUITY
  { code: "3001", name: "Owner's Capital",           type: "Equity",      category: "Owner's Equity",       balance: 180000,   normalBalance: "Cr", status: "active",   lastActivity: "Jan 01, 2026", level: 2 },
  { code: "3100", name: "Retained Earnings",         type: "Equity",      category: "Owner's Equity",       balance: 74580,    normalBalance: "Cr", status: "active",   lastActivity: "Dec 31, 2025", level: 2 },
  { code: "3200", name: "Current Year Earnings",     type: "Equity",      category: "Owner's Equity",       balance: 36560,    normalBalance: "Cr", status: "active",   lastActivity: "Feb 26, 2026", level: 2 },
  { code: "3300", name: "Owner's Drawings",          type: "Equity",      category: "Owner's Equity",       balance: -18000,   normalBalance: "Dr", status: "active",   lastActivity: "Feb 15, 2026", level: 2 },
  // REVENUE
  { code: "4001", name: "Sales Revenue",             type: "Revenue",     category: "Operating Revenue",    balance: 186420,   normalBalance: "Cr", status: "active",   lastActivity: "Feb 26, 2026", level: 2 },
  { code: "4002", name: "Service Revenue",           type: "Revenue",     category: "Operating Revenue",    balance: 52800,    normalBalance: "Cr", status: "active",   lastActivity: "Feb 25, 2026", level: 2 },
  { code: "4003", name: "Consulting Revenue",        type: "Revenue",     category: "Operating Revenue",    balance: 24600,    normalBalance: "Cr", status: "active",   lastActivity: "Feb 22, 2026", level: 2 },
  { code: "4100", name: "Interest Income",           type: "Revenue",     category: "Other Income",         balance: 1240,     normalBalance: "Cr", status: "active",   lastActivity: "Feb 28, 2026", level: 2 },
  { code: "4200", name: "Other Income",              type: "Revenue",     category: "Other Income",         balance: 3840,     normalBalance: "Cr", status: "active",   lastActivity: "Feb 10, 2026", level: 2 },
  // EXPENSES
  { code: "5001", name: "Cost of Goods Sold",        type: "Expenses",    category: "Cost of Sales",        balance: 89240,    normalBalance: "Dr", status: "active",   lastActivity: "Feb 26, 2026", level: 2 },
  { code: "5100", name: "Salaries Expense",          type: "Expenses",    category: "Operating Expenses",   balance: 48000,    normalBalance: "Dr", status: "active",   lastActivity: "Feb 28, 2026", level: 2 },
  { code: "5200", name: "Rent Expense",              type: "Expenses",    category: "Operating Expenses",   balance: 12000,    normalBalance: "Dr", status: "active",   lastActivity: "Feb 01, 2026", level: 2 },
  { code: "5300", name: "Utilities",                 type: "Expenses",    category: "Operating Expenses",   balance: 3800,     normalBalance: "Dr", status: "active",   lastActivity: "Feb 20, 2026", level: 2 },
  { code: "5400", name: "Marketing & Advertising",   type: "Expenses",    category: "Operating Expenses",   balance: 8200,     normalBalance: "Dr", status: "active",   lastActivity: "Feb 18, 2026", level: 2 },
  { code: "5500", name: "Depreciation Expense",      type: "Expenses",    category: "Operating Expenses",   balance: 9840,     normalBalance: "Dr", status: "active",   lastActivity: "Feb 28, 2026", level: 2 },
  { code: "5600", name: "Office Supplies",           type: "Expenses",    category: "Operating Expenses",   balance: 2480,     normalBalance: "Dr", status: "active",   lastActivity: "Feb 15, 2026", level: 2 },
  { code: "5700", name: "Bank Charges",              type: "Expenses",    category: "Other Expenses",       balance: 840,      normalBalance: "Dr", status: "active",   lastActivity: "Feb 25, 2026", level: 2 },
  { code: "5800", name: "Miscellaneous Expense",     type: "Expenses",    category: "Other Expenses",       balance: 1360,     normalBalance: "Dr", status: "inactive", lastActivity: "Jan 30, 2026", level: 2 },
];

// ─── Config ───────────────────────────────────────────────────────────────────

const typeConfig: Record<AccountType, { color: string; bg: string; light: string; icon: React.ReactNode; label: string }> = {
  Assets:      { color: "#3B82F6", bg: "#EFF6FF", light: "#DBEAFE", icon: <Wallet size={16} />,    label: "Assets" },
  Liabilities: { color: "#EF4444", bg: "#FEF2F2", light: "#FECACA", icon: <TrendingDown size={16} />, label: "Liabilities" },
  Equity:      { color: "#8B5CF6", bg: "#F5F3FF", light: "#DDD6FE", icon: <Building2 size={16} />, label: "Equity" },
  Revenue:     { color: "#10B981", bg: "#ECFDF5", light: "#A7F3D0", icon: <TrendingUp size={16} />, label: "Revenue" },
  Expenses:    { color: "#F59E0B", bg: "#FFFBEB", light: "#FDE68A", icon: <BookOpen size={16} />,   label: "Expenses" },
};

const filterTabs: { key: string; label: string }[] = [
  { key: "All",         label: "All Accounts" },
  { key: "Assets",      label: "Assets" },
  { key: "Liabilities", label: "Liabilities" },
  { key: "Equity",      label: "Equity" },
  { key: "Revenue",     label: "Revenue" },
  { key: "Expenses",    label: "Expenses" },
];

// ─── Sub-components ─────────────────────────────────────────────────────────��─

const StatusBadge = ({ status }: { status: AccountStatus }) =>
  status === "active" ? (
    <span style={{ display: "inline-flex", alignItems: "center", gap: "4px", backgroundColor: "#ECFDF5", color: "#065F46", fontSize: "10.5px", fontWeight: 600, padding: "2px 8px", borderRadius: "20px" }}>
      <span style={{ width: 5, height: 5, borderRadius: "50%", backgroundColor: "#10B981", display: "inline-block" }} />
      Active
    </span>
  ) : (
    <span style={{ display: "inline-flex", alignItems: "center", gap: "4px", backgroundColor: "#F1F5F9", color: "#94A3B8", fontSize: "10.5px", fontWeight: 600, padding: "2px 8px", borderRadius: "20px" }}>
      <span style={{ width: 5, height: 5, borderRadius: "50%", backgroundColor: "#CBD5E1", display: "inline-block" }} />
      Inactive
    </span>
  );

const DrCrBadge = ({ type }: { type: DrCr }) => (
  <span style={{
    display: "inline-flex",
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: type === "Dr" ? "#EFF6FF" : "#F5F3FF",
    color: type === "Dr" ? "#1D4ED8" : "#6D28D9",
    fontSize: "10.5px",
    fontWeight: 700,
    width: "28px",
    height: "20px",
    borderRadius: "4px",
  }}>{type}</span>
);

function formatBalance(balance: number) {
  const abs = Math.abs(balance);
  const str = abs.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  return balance < 0 ? `(${str})` : str;
}

const CustomPieTooltip = ({ active, payload }: any) => {
  if (active && payload && payload.length) {
    return (
      <div style={{ backgroundColor: "#1E3A5F", border: "none", borderRadius: "8px", padding: "7px 11px", fontFamily: "'Inter', sans-serif" }}>
        <span style={{ color: "#FFFFFF", fontSize: "12px", fontWeight: 600 }}>
          {payload[0].name}: ${(payload[0].value / 1000).toFixed(0)}k
        </span>
      </div>
    );
  }
  return null;
};

// ─── Group-row component ──────────────────────────────────────────────────────

function GroupHeader({
  label,
  type,
  total,
  count,
  expanded,
  onToggle,
}: {
  label: string;
  type: AccountType;
  total: number;
  count: number;
  expanded: boolean;
  onToggle: () => void;
}) {
  const cfg = typeConfig[type];
  return (
    <tr
      onClick={onToggle}
      style={{ cursor: "pointer", backgroundColor: cfg.bg }}
    >
      <td colSpan={7} style={{ padding: "9px 16px" }}>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
          <div style={{ display: "flex", alignItems: "center", gap: "10px" }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "center", width: 28, height: 28, borderRadius: 8, backgroundColor: cfg.light }}>
              <span style={{ color: cfg.color }}>{cfg.icon}</span>
            </div>
            <span style={{ color: cfg.color, fontSize: "12.5px", fontWeight: 700, letterSpacing: "0.2px" }}>
              {label}
            </span>
            <span style={{ backgroundColor: cfg.light, color: cfg.color, fontSize: "10px", fontWeight: 700, padding: "1px 7px", borderRadius: "10px" }}>
              {count} accounts
            </span>
          </div>
          <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
            <span style={{ color: total < 0 ? "#EF4444" : cfg.color, fontSize: "13px", fontWeight: 800 }}>
              ${formatBalance(Math.abs(total))}
            </span>
            {expanded
              ? <ChevronDown size={14} style={{ color: cfg.color }} />
              : <ChevronRight size={14} style={{ color: cfg.color }} />
            }
          </div>
        </div>
      </td>
    </tr>
  );
}

// ─── Main Component ───────────────────────────────────────────────────────────

export function AccountsTab() {
  const [activeFilter, setActiveFilter] = useState<string>("All");
  const [searchQuery, setSearchQuery]   = useState<string>("");
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(
    new Set(["Assets", "Liabilities", "Equity", "Revenue", "Expenses"])
  );

  const toggleGroup = (type: string) => {
    setExpandedGroups((prev) => {
      const next = new Set(prev);
      if (next.has(type)) next.delete(type);
      else next.add(type);
      return next;
    });
  };

  // ── Filtered data ──
  const filtered = useMemo(() => {
    return accounts.filter((acc) => {
      const matchType   = activeFilter === "All" || acc.type === activeFilter;
      const q           = searchQuery.toLowerCase();
      const matchSearch = !q || acc.name.toLowerCase().includes(q) || acc.code.includes(q) || acc.category.toLowerCase().includes(q);
      return matchType && matchSearch;
    });
  }, [activeFilter, searchQuery]);

  // ── KPI summaries ──
  const totalAssets      = accounts.filter(a => a.type === "Assets").reduce((s, a) => s + a.balance, 0);
  const totalLiabilities = accounts.filter(a => a.type === "Liabilities").reduce((s, a) => s + a.balance, 0);
  const totalEquity      = accounts.filter(a => a.type === "Equity").reduce((s, a) => s + a.balance, 0);
  const totalRevenue     = accounts.filter(a => a.type === "Revenue").reduce((s, a) => s + a.balance, 0);
  const totalExpenses    = accounts.filter(a => a.type === "Expenses").reduce((s, a) => s + a.balance, 0);
  const activeCount      = accounts.filter(a => a.status === "active").length;

  // ── Groups for rendering ──
  const typeOrder: AccountType[] = ["Assets", "Liabilities", "Equity", "Revenue", "Expenses"];
  const visibleTypes = activeFilter === "All"
    ? typeOrder
    : typeOrder.filter(t => t === activeFilter);

  // ── Donut chart data ──
  const donutData = [
    { name: "Assets",      value: Math.abs(totalAssets),      color: "#3B82F6" },
    { name: "Liabilities", value: Math.abs(totalLiabilities), color: "#EF4444" },
    { name: "Equity",      value: Math.abs(totalEquity),      color: "#8B5CF6" },
    { name: "Revenue",     value: Math.abs(totalRevenue),     color: "#10B981" },
    { name: "Expenses",    value: Math.abs(totalExpenses),    color: "#F59E0B" },
  ];

  const kpiCards = [
    {
      title: "Total Assets",
      value: `$${(totalAssets / 1000).toFixed(1)}k`,
      change: "+8.7%",
      positive: true,
      color: "#3B82F6",
      bg: "#EFF6FF",
      icon: <Wallet size={20} />,
    },
    {
      title: "Total Liabilities",
      value: `$${(totalLiabilities / 1000).toFixed(1)}k`,
      change: "-3.4%",
      positive: false,
      color: "#EF4444",
      bg: "#FEF2F2",
      icon: <TrendingDown size={20} />,
    },
    {
      title: "Net Equity",
      value: `$${(totalEquity / 1000).toFixed(1)}k`,
      change: "+5.2%",
      positive: true,
      color: "#8B5CF6",
      bg: "#F5F3FF",
      icon: <Building2 size={20} />,
    },
    {
      title: "Active Accounts",
      value: `${activeCount}`,
      change: "+2",
      positive: true,
      color: "#10B981",
      bg: "#ECFDF5",
      icon: <BookOpen size={20} />,
    },
  ];

  const totalDonut = donutData.reduce((s, d) => s + d.value, 0);

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "16px", fontFamily: "'Inter', sans-serif" }}>

      {/* ── Page title row ── */}
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h2 style={{ margin: 0, color: "#0F172A", fontSize: "16px", fontWeight: 800 }}>Chart of Accounts</h2>
          <p style={{ margin: "2px 0 0", color: "#94A3B8", fontSize: "12px" }}>
            Manage your full general ledger structure · FY 2025–26
          </p>
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
          <button style={{ display: "flex", alignItems: "center", gap: "6px", padding: "8px 14px", borderRadius: "9px", border: "1px solid #E2E8F0", backgroundColor: "#FFFFFF", color: "#64748B", fontSize: "12px", cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <RefreshCw size={13} />
            Refresh
          </button>
          <button style={{ display: "flex", alignItems: "center", gap: "6px", padding: "8px 14px", borderRadius: "9px", border: "1px solid #E2E8F0", backgroundColor: "#FFFFFF", color: "#64748B", fontSize: "12px", cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <Download size={13} />
            Export
          </button>
          <button style={{ display: "flex", alignItems: "center", gap: "6px", padding: "8px 16px", borderRadius: "9px", border: "none", backgroundColor: "#1E3A5F", color: "#FFFFFF", fontSize: "12px", fontWeight: 600, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <Plus size={14} />
            New Account
          </button>
        </div>
      </div>

      {/* ── KPI Cards ── */}
      <div style={{ display: "flex", gap: "14px" }}>
        {kpiCards.map((card) => (
          <div
            key={card.title}
            style={{
              flex: 1,
              backgroundColor: "#FFFFFF",
              border: "1px solid #F1F5F9",
              borderRadius: "12px",
              padding: "16px 18px",
              boxShadow: "0 1px 4px rgba(0,0,0,0.05)",
              display: "flex",
              alignItems: "center",
              gap: "14px",
              position: "relative",
              overflow: "hidden",
            }}
          >
            <div style={{ position: "absolute", top: 0, left: 0, right: 0, height: "3px", backgroundColor: card.color, borderRadius: "12px 12px 0 0" }} />
            <div style={{ display: "flex", alignItems: "center", justifyContent: "center", width: 42, height: 42, borderRadius: 10, backgroundColor: card.bg, flexShrink: 0 }}>
              <span style={{ color: card.color }}>{card.icon}</span>
            </div>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ color: "#64748B", fontSize: "11px", fontWeight: 500, marginBottom: "3px" }}>{card.title}</div>
              <div style={{ color: "#0F172A", fontSize: "20px", fontWeight: 800, letterSpacing: "-0.4px", lineHeight: 1 }}>{card.value}</div>
            </div>
            <span
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: "3px",
                backgroundColor: card.positive ? "#ECFDF5" : "#FEF2F2",
                color: card.positive ? "#10B981" : "#EF4444",
                fontSize: "10.5px",
                fontWeight: 700,
                padding: "3px 8px",
                borderRadius: "20px",
                flexShrink: 0,
              }}
            >
              {card.positive ? <ArrowUpRight size={11} /> : <ArrowDownRight size={11} />}
              {card.change}
            </span>
          </div>
        ))}
      </div>

      {/* ── Main content: Table + Right Panel ── */}
      <div style={{ display: "flex", gap: "16px", alignItems: "flex-start" }}>

        {/* ── Table Panel ── */}
        <div
          style={{
            flex: 1,
            backgroundColor: "#FFFFFF",
            border: "1px solid #F1F5F9",
            borderRadius: "12px",
            boxShadow: "0 1px 4px rgba(0,0,0,0.05)",
            overflow: "hidden",
            minWidth: 0,
          }}
        >
          {/* Filter bar */}
          <div style={{ padding: "14px 18px", borderBottom: "1px solid #F1F5F9", display: "flex", alignItems: "center", justifyContent: "space-between", gap: "12px" }}>
            {/* Tabs */}
            <div style={{ display: "flex", gap: "4px", backgroundColor: "#F5F7FA", padding: "4px", borderRadius: "10px" }}>
              {filterTabs.map((tab) => {
                const isActive = activeFilter === tab.key;
                const cfg = tab.key !== "All" ? typeConfig[tab.key as AccountType] : null;
                const count = tab.key === "All" ? accounts.length : accounts.filter(a => a.type === tab.key).length;
                return (
                  <button
                    key={tab.key}
                    onClick={() => setActiveFilter(tab.key)}
                    style={{
                      padding: "5px 12px",
                      borderRadius: "7px",
                      border: "none",
                      backgroundColor: isActive ? "#FFFFFF" : "transparent",
                      color: isActive ? (cfg?.color ?? "#1E3A5F") : "#64748B",
                      fontSize: "12px",
                      fontWeight: isActive ? 700 : 400,
                      cursor: "pointer",
                      boxShadow: isActive ? "0 1px 3px rgba(0,0,0,0.1)" : "none",
                      transition: "all 0.15s",
                      display: "flex",
                      alignItems: "center",
                      gap: "5px",
                      fontFamily: "'Inter', sans-serif",
                    }}
                  >
                    {tab.label}
                    <span style={{
                      backgroundColor: isActive ? (cfg?.bg ?? "#EFF6FF") : "#E2E8F0",
                      color: isActive ? (cfg?.color ?? "#1E3A5F") : "#94A3B8",
                      fontSize: "9.5px",
                      fontWeight: 700,
                      padding: "1px 6px",
                      borderRadius: "10px",
                    }}>
                      {count}
                    </span>
                  </button>
                );
              })}
            </div>
            {/* Search + filter */}
            <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
              <div style={{ display: "flex", alignItems: "center", gap: "8px", padding: "6px 12px", borderRadius: "8px", border: "1px solid #E2E8F0", backgroundColor: "#F8FAFC", width: "220px" }}>
                <Search size={13} style={{ color: "#94A3B8", flexShrink: 0 }} />
                <input
                  type="text"
                  placeholder="Search code or name..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  style={{ flex: 1, border: "none", outline: "none", background: "transparent", color: "#334155", fontSize: "12.5px", fontFamily: "'Inter', sans-serif" }}
                />
              </div>
              <button style={{ display: "flex", alignItems: "center", gap: "6px", padding: "6px 12px", borderRadius: "8px", border: "1px solid #E2E8F0", backgroundColor: "#F8FAFC", color: "#64748B", fontSize: "12px", cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
                <Filter size={12} />
                Status
                <ChevronDown size={11} />
              </button>
            </div>
          </div>

          {/* Table */}
          <div style={{ overflowX: "auto", maxHeight: "430px", overflowY: "auto" }}>
            <table style={{ width: "100%", borderCollapse: "collapse" }}>
              <thead style={{ position: "sticky", top: 0, zIndex: 2 }}>
                <tr style={{ backgroundColor: "#F8FAFC" }}>
                  {["Code", "Account Name", "Category", "Balance (SAR)", "Dr/Cr", "Status", "Actions"].map((h, i) => (
                    <th
                      key={h}
                      style={{
                        padding: "9px 16px",
                        textAlign: h === "Balance (SAR)" ? "right" : "left",
                        color: "#64748B",
                        fontSize: "10.5px",
                        fontWeight: 700,
                        textTransform: "uppercase",
                        letterSpacing: "0.6px",
                        borderBottom: "1px solid #E2E8F0",
                        whiteSpace: "nowrap",
                        backgroundColor: "#F8FAFC",
                      }}
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              {visibleTypes.map((type) => {
                const typeAccounts = filtered.filter(a => a.type === type);
                if (typeAccounts.length === 0) return null;
                const typeTotal = typeAccounts.reduce((s, a) => s + a.balance, 0);
                const isExpanded = expandedGroups.has(type);

                return (
                  <tbody key={type}>
                    <GroupHeader
                      label={type}
                      type={type}
                      total={typeTotal}
                      count={typeAccounts.length}
                      expanded={isExpanded}
                      onToggle={() => toggleGroup(type)}
                    />
                    {isExpanded && typeAccounts.map((acc, idx) => {
                      const cfg = typeConfig[acc.type];
                      return (
                        <tr
                          key={acc.code}
                          style={{
                            backgroundColor: idx % 2 === 0 ? "#FFFFFF" : "#FAFBFC",
                            borderBottom: "1px solid #F1F5F9",
                            transition: "background-color 0.1s",
                          }}
                          onMouseEnter={(e) => { (e.currentTarget as HTMLTableRowElement).style.backgroundColor = "#F0F7FF"; }}
                          onMouseLeave={(e) => { (e.currentTarget as HTMLTableRowElement).style.backgroundColor = idx % 2 === 0 ? "#FFFFFF" : "#FAFBFC"; }}
                        >
                          {/* Code */}
                          <td style={{ padding: "10px 16px" }}>
                            <span style={{ color: cfg.color, fontSize: "12px", fontWeight: 700, fontFamily: "monospace" }}>
                              {acc.code}
                            </span>
                          </td>
                          {/* Name */}
                          <td style={{ padding: "10px 16px" }}>
                            <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
                              <div style={{ width: 28, height: 28, borderRadius: 7, backgroundColor: cfg.bg, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
                                <span style={{ color: cfg.color, fontSize: "10px", fontWeight: 700 }}>
                                  {acc.name.slice(0, 2).toUpperCase()}
                                </span>
                              </div>
                              <div>
                                <div style={{ color: "#0F172A", fontSize: "12.5px", fontWeight: 600 }}>{acc.name}</div>
                                <div style={{ color: "#94A3B8", fontSize: "10.5px" }}>Last: {acc.lastActivity}</div>
                              </div>
                            </div>
                          </td>
                          {/* Category */}
                          <td style={{ padding: "10px 16px" }}>
                            <span style={{ color: "#64748B", fontSize: "11.5px", backgroundColor: "#F1F5F9", padding: "2px 8px", borderRadius: "5px" }}>
                              {acc.category}
                            </span>
                          </td>
                          {/* Balance */}
                          <td style={{ padding: "10px 16px", textAlign: "right" }}>
                            <span style={{
                              color: acc.balance < 0 ? "#EF4444" : "#0F172A",
                              fontSize: "13px",
                              fontWeight: 700,
                              fontVariantNumeric: "tabular-nums",
                            }}>
                              {acc.balance < 0 ? `($${Math.abs(acc.balance).toLocaleString("en-US", { minimumFractionDigits: 2 })})` : `$${acc.balance.toLocaleString("en-US", { minimumFractionDigits: 2 })}`}
                            </span>
                          </td>
                          {/* Dr/Cr */}
                          <td style={{ padding: "10px 16px" }}>
                            <DrCrBadge type={acc.normalBalance} />
                          </td>
                          {/* Status */}
                          <td style={{ padding: "10px 16px" }}>
                            <StatusBadge status={acc.status} />
                          </td>
                          {/* Actions */}
                          <td style={{ padding: "10px 16px" }}>
                            <div style={{ display: "flex", alignItems: "center", gap: "4px" }}>
                              <button title="View" style={{ width: 27, height: 27, borderRadius: 7, border: "none", backgroundColor: "#EFF6FF", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center" }}>
                                <Eye size={12} style={{ color: "#3B82F6" }} />
                              </button>
                              <button title="Edit" style={{ width: 27, height: 27, borderRadius: 7, border: "none", backgroundColor: "#F5F3FF", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center" }}>
                                <Pencil size={12} style={{ color: "#8B5CF6" }} />
                              </button>
                              <button title="More" style={{ width: 27, height: 27, borderRadius: 7, border: "none", backgroundColor: "#F8FAFC", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center" }}>
                                <MoreHorizontal size={12} style={{ color: "#94A3B8" }} />
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                );
              })}
            </table>

            {/* Empty state */}
            {filtered.length === 0 && (
              <div style={{ padding: "48px", textAlign: "center" }}>
                <Search size={32} style={{ color: "#CBD5E1", margin: "0 auto 12px" }} />
                <p style={{ color: "#94A3B8", fontSize: "13px", margin: 0 }}>No accounts match your search.</p>
              </div>
            )}
          </div>

          {/* Footer */}
          <div style={{ padding: "11px 18px", borderTop: "1px solid #F1F5F9", display: "flex", alignItems: "center", justifyContent: "space-between" }}>
            <span style={{ color: "#94A3B8", fontSize: "12px" }}>
              Showing {filtered.length} of {accounts.length} accounts
            </span>
            <div style={{ display: "flex", alignItems: "center", gap: "4px" }}>
              {["‹ Prev", "1", "2", "3", "Next ›"].map((p, i) => (
                <button key={i} style={{ minWidth: "28px", height: "28px", borderRadius: "7px", border: p === "1" ? "none" : "1px solid #E2E8F0", backgroundColor: p === "1" ? "#1E3A5F" : "transparent", color: p === "1" ? "#FFFFFF" : "#64748B", fontSize: "12px", fontWeight: p === "1" ? 700 : 400, cursor: "pointer", padding: "0 8px", fontFamily: "'Inter', sans-serif" }}>
                  {p}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* ── Right Panel ── */}
        <div style={{ width: "292px", minWidth: "292px", display: "flex", flexDirection: "column", gap: "14px" }}>

          {/* Distribution Donut */}
          <div style={{ backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: "12px", padding: "18px", boxShadow: "0 1px 4px rgba(0,0,0,0.05)" }}>
            <h4 style={{ margin: "0 0 14px", color: "#0F172A", fontSize: "13px", fontWeight: 700 }}>Balance Distribution</h4>
            <div style={{ display: "flex", flexDirection: "column", alignItems: "center" }}>
              <div style={{ position: "relative" }}>
                <PieChart width={160} height={160}>
                  <Pie
                    data={donutData.map(d => ({ ...d, fill: d.color }))}
                    cx="50%"
                    cy="50%"
                    innerRadius={52}
                    outerRadius={72}
                    paddingAngle={3}
                    dataKey="value"
                    stroke="none"
                    isAnimationActive={false}
                  />
                  <Tooltip content={<CustomPieTooltip />} />
                </PieChart>
                <div style={{ position: "absolute", top: "50%", left: "50%", transform: "translate(-50%, -50%)", textAlign: "center", pointerEvents: "none" }}>
                  <div style={{ color: "#0F172A", fontSize: "15px", fontWeight: 800, lineHeight: 1 }}>
                    {accounts.length}
                  </div>
                  <div style={{ color: "#94A3B8", fontSize: "9px", fontWeight: 500, marginTop: "2px" }}>Accounts</div>
                </div>
              </div>
              {/* Legend */}
              <div style={{ width: "100%", display: "flex", flexDirection: "column", gap: "8px", marginTop: "8px" }}>
                {donutData.map((item) => {
                  const pct = ((item.value / totalDonut) * 100).toFixed(1);
                  return (
                    <div key={item.name} style={{ display: "flex", alignItems: "center", gap: "8px" }}>
                      <div style={{ width: 10, height: 10, borderRadius: 3, backgroundColor: item.color, flexShrink: 0 }} />
                      <span style={{ flex: 1, color: "#64748B", fontSize: "11.5px" }}>{item.name}</span>
                      <div style={{ height: 4, width: 48, borderRadius: 2, backgroundColor: "#F1F5F9", overflow: "hidden" }}>
                        <div style={{ height: "100%", width: `${pct}%`, backgroundColor: item.color, borderRadius: 2 }} />
                      </div>
                      <span style={{ color: "#0F172A", fontSize: "11.5px", fontWeight: 700, minWidth: "34px", textAlign: "right" }}>
                        {pct}%
                      </span>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>

          {/* Balance Sheet Summary */}
          <div style={{ backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: "12px", padding: "18px", boxShadow: "0 1px 4px rgba(0,0,0,0.05)" }}>
            <h4 style={{ margin: "0 0 14px", color: "#0F172A", fontSize: "13px", fontWeight: 700 }}>Balance Sheet Summary</h4>
            <div style={{ display: "flex", flexDirection: "column", gap: "10px" }}>
              {[
                { label: "Total Assets",      value: totalAssets,      color: "#3B82F6" },
                { label: "Total Liabilities", value: totalLiabilities, color: "#EF4444" },
                { label: "Total Equity",      value: totalEquity,      color: "#8B5CF6" },
              ].map((item) => (
                <div key={item.label} style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <div style={{ display: "flex", alignItems: "center", gap: "7px" }}>
                    <div style={{ width: 3, height: 28, borderRadius: 3, backgroundColor: item.color }} />
                    <span style={{ color: "#64748B", fontSize: "12px" }}>{item.label}</span>
                  </div>
                  <span style={{ color: "#0F172A", fontSize: "12.5px", fontWeight: 700 }}>
                    ${Math.abs(item.value).toLocaleString("en-US", { minimumFractionDigits: 0 })}
                  </span>
                </div>
              ))}
              <div style={{ height: 1, backgroundColor: "#F1F5F9", margin: "4px 0" }} />
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <span style={{ color: "#1E3A5F", fontSize: "12.5px", fontWeight: 700 }}>Liabilities + Equity</span>
                <span style={{ color: "#10B981", fontSize: "13px", fontWeight: 800 }}>
                  ${(totalLiabilities + totalEquity).toLocaleString("en-US", { minimumFractionDigits: 0 })}
                </span>
              </div>
              {/* Balance check */}
              <div style={{ backgroundColor: "#ECFDF5", border: "1px solid #A7F3D0", borderRadius: "8px", padding: "8px 10px", display: "flex", alignItems: "center", gap: "7px" }}>
                <div style={{ width: 6, height: 6, borderRadius: "50%", backgroundColor: "#10B981", flexShrink: 0 }} />
                <span style={{ color: "#065F46", fontSize: "11px", fontWeight: 500 }}>
                  Accounting equation <strong>balanced ✓</strong>
                </span>
              </div>
            </div>
          </div>

          {/* P&L Snapshot */}
          <div style={{ backgroundColor: "#1E3A5F", border: "none", borderRadius: "12px", padding: "18px", boxShadow: "0 4px 16px rgba(30,58,95,0.25)" }}>
            <h4 style={{ margin: "0 0 14px", color: "#FFFFFF", fontSize: "13px", fontWeight: 700 }}>P&L Snapshot</h4>
            <div style={{ display: "flex", flexDirection: "column", gap: "9px" }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <span style={{ color: "rgba(255,255,255,0.6)", fontSize: "12px" }}>Total Revenue</span>
                <span style={{ color: "#10B981", fontSize: "12.5px", fontWeight: 700 }}>${totalRevenue.toLocaleString("en-US")}</span>
              </div>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <span style={{ color: "rgba(255,255,255,0.6)", fontSize: "12px" }}>Total Expenses</span>
                <span style={{ color: "#F87171", fontSize: "12.5px", fontWeight: 700 }}>${totalExpenses.toLocaleString("en-US")}</span>
              </div>
              <div style={{ height: 1, backgroundColor: "rgba(255,255,255,0.12)", margin: "2px 0" }} />
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <span style={{ color: "#FFFFFF", fontSize: "12.5px", fontWeight: 700 }}>Net Income</span>
                <span style={{ color: "#FFFFFF", fontSize: "14px", fontWeight: 800 }}>
                  ${(totalRevenue - totalExpenses).toLocaleString("en-US")}
                </span>
              </div>
              {/* Profit margin bar */}
              <div style={{ marginTop: "6px" }}>
                <div style={{ display: "flex", justifyContent: "space-between", marginBottom: "5px" }}>
                  <span style={{ color: "rgba(255,255,255,0.5)", fontSize: "10px" }}>Profit Margin</span>
                  <span style={{ color: "rgba(255,255,255,0.8)", fontSize: "10px", fontWeight: 600 }}>
                    {((totalRevenue - totalExpenses) / totalRevenue * 100).toFixed(1)}%
                  </span>
                </div>
                <div style={{ height: 6, backgroundColor: "rgba(255,255,255,0.1)", borderRadius: 3 }}>
                  <div style={{ height: "100%", width: `${(totalRevenue - totalExpenses) / totalRevenue * 100}%`, backgroundColor: "#3B82F6", borderRadius: 3 }} />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}